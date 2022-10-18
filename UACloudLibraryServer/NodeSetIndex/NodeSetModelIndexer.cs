/* ========================================================================
 * Copyright (c) 2005-2022 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CESMII.OpcUa.NodeSetImporter;
using CESMII.OpcUa.NodeSetModel;
using CESMII.OpcUa.NodeSetModel.Factory.Opc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Interfaces;
using Opc.Ua.Export;

namespace Opc.Ua.Cloud.Library
{
    public class NodeSetModelIndexerFactory
    {
        public NodeSetModelIndexerFactory(IServiceScopeFactory serviceScopeFactory, ILogger<NodeSetModelIndexer> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<NodeSetModelIndexer> _logger;

        public NodeSetModelIndexer Create()
        {
            var scope = _serviceScopeFactory.CreateScope();
            var appDbContext = scope.ServiceProvider.GetService<AppDbContext>();
            var fileStore = scope.ServiceProvider.GetService<IFileStorage>();
            var database = scope.ServiceProvider.GetService<IDatabase>();
            var logger = scope.ServiceProvider.GetService<ILogger<NodeSetModelIndexer>>();
            return new NodeSetModelIndexer(appDbContext, logger, fileStore, database, scope);
        }
    }

    public class NodeSetModelIndexer : IDisposable
    {
        public NodeSetModelIndexer(AppDbContext dbContext, ILogger<NodeSetModelIndexer> logger, IFileStorage storage, IDatabase database)
            : this(dbContext, logger, storage, database, null)
        {
        }
        public NodeSetModelIndexer(AppDbContext dbContext, ILogger logger, IFileStorage storage, IDatabase database, IServiceScope scope = null)
        {
            _dbContext = dbContext;
            _logger = logger;
            _storage = storage;
            _database = database;
            _scope = scope;
        }
        private AppDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly IFileStorage _storage;
        private readonly IDatabase _database;
        private readonly IServiceScope _scope;

        public async Task<(ValidationStatus Status, string Info)> IndexNodeSetModelAsync(string modelUri, string nodeSetXML, string identifier)
        {
            try
            {
                ValidationStatus validationStatus = ValidationStatus.Error;
                string validationStatusInfo = "Internal error";

                var sw = Stopwatch.StartNew();

                var loadedNodesetModels = await ImportNodeSetModelAsync(nodeSetXML, identifier).ConfigureAwait(false);

                foreach (var nodeSetModel in loadedNodesetModels)
                {
                    nodeSetModel.Identifier = identifier;
                    var clNodeSet = nodeSetModel as CloudLibNodeSetModel;
                    if (clNodeSet == null)
                    {
                        throw new Exception($"Internal error: unexpected nodeset model type {nodeSetModel.GetType()}");
                    }
                    clNodeSet.ValidationStatus = validationStatus = ValidationStatus.Indexed;
                    clNodeSet.ValidationStatusInfo = validationStatusInfo = null;
                    if (!_dbContext.Set<NodeSetModel>().Local.Any(nsm => nsm.ModelUri == clNodeSet.ModelUri && nsm.PublicationDate == clNodeSet.PublicationDate))
                    {
                        _dbContext.nodeSets.Add(clNodeSet);
                    }
                }
                var validatedTime = sw.ElapsedMilliseconds;
                _logger.LogInformation($"Finished validating nodeset {modelUri} {identifier}: {validatedTime} ms.");

                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                var savedTime = sw.ElapsedMilliseconds;
                _logger.LogInformation($"Finished indexing nodeset {modelUri} {identifier}: {savedTime - validatedTime} ms. Total: {savedTime} ms.");
#if DEBUG
                var nodeSet = loadedNodesetModels.FirstOrDefault(nsm => nsm.ModelUri == modelUri);
                if (nodeSet != null)
                {
                    var clNodeSet = nodeSet as CloudLibNodeSetModel;
                    clNodeSet.ValidationElapsedTime = TimeSpan.FromMilliseconds(savedTime);
                    clNodeSet.ValidationFinishedTime = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
#endif
                return (validationStatus, validationStatusInfo);
            }
            catch (NodeSetResolverException ex)
            {
                return (ValidationStatus.Error, ex.Message);
            }
        }
        public Task<List<NodeSetModel>> ImportNodeSetModelAsync(string nodeSetXML, string identifier)
        {
            var myNodeSetCache = new UANodeSetIFileStorage(_storage, _dbContext);
            var opcContext = new CloudLibDbOpcUaContext(_dbContext, _logger,
                (model) => CloudLibNodeSetModel.FromModelAsync(model, _dbContext).Result
                );
            var nodesetImporter = new UANodeSetModelImporter(opcContext, myNodeSetCache);
            return nodesetImporter.ImportNodeSetModelAsync(nodeSetXML, identifier);

        }

        public async Task<CloudLibNodeSetModel> CreateNodeSetModelFromNodeSetAsync(UANodeSet nodeSet, string identifier)
        {
            var nodeSetModel = await CloudLibNodeSetModel.FromModelAsync(nodeSet.Models[0], _dbContext).ConfigureAwait(false);
            nodeSetModel.Identifier = identifier;
            nodeSetModel.ValidationStatus = ValidationStatus.Parsed;
            nodeSetModel.ValidationStatusInfo = null;
            await _dbContext.nodeSets.AddAsync(nodeSetModel);
            await _dbContext.SaveChangesAsync();
            return nodeSetModel;
        }

        public async Task DeleteNodeSetIndex(string identifier)
        {
            var existingNodeSet = _dbContext.nodeSets.AsQueryable().Where(n => n.Identifier == identifier).FirstOrDefault();
            if (existingNodeSet != null)
            {
                var nodes = _dbContext.nodeModels.Where(nm => nm.NodeSet == existingNodeSet).ToList();
                foreach (var node in nodes)
                {
                    try
                    {
                        var referencesToNode = await _dbContext.nodeModels.Where(nm => nm.OtherReferencedNodes.Any(reference => reference.Node == node)).ToListAsync().ConfigureAwait(false);
                        foreach (var referencingNode in referencesToNode)
                        {
                            referencingNode.OtherReferencedNodes.RemoveAll(reference => reference.Node == node);
                            _dbContext.Update(referencingNode);
                        }
                    }
                    catch
                    {
                        // ignore: any critical omissions will be caught by SaveChangedAsync
                    }
                    _dbContext.nodeModels.Remove(node);
                }
                _dbContext.nodeSets.Remove(existingNodeSet);
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }



        static bool bIndexing = false;
        static object _indexingLock = new object();
        public static async Task IndexNodeSetsAsync(NodeSetModelIndexerFactory factory)
        {
            lock (_indexingLock)
            {
                if (bIndexing) return;
                bIndexing = true;
            }
            try
            {
                using (var indexer = factory.Create())
                {
                    var logger = indexer._logger;
                    int changedCount = 0;
                    int previousCount;
                    int rerunCount = 0;
                    logger.LogInformation($"Starting background indexing. Nodeset count: {indexer._dbContext.nodeSets.Count()}. Not indexed: {indexer._dbContext.nodeSets.Count(n => n.ValidationStatus != ValidationStatus.Indexed)}");
                    do
                    {
                        previousCount = changedCount;
                        changedCount = await IndexNodeSetsInternalAsync(factory);
                        rerunCount++;
                        if (rerunCount >= 50)
                        {
                            if (changedCount == previousCount)
                            {
                                // stop the loop when there are no more changes after many attempts (defense in depth against faulty indexing)
                                logger.LogError($"Excessive indexing re-runs: {rerunCount}. Stopping indexing loop.");
                                break;
                            }
                            else
                            {
                                logger.LogWarning($"Excessive indexing re-runs: {rerunCount}. {changedCount} {previousCount}");
                            }
                        }
                    } while (changedCount > 0);
                    logger.LogInformation($"Finished background indexing. Nodeset count: {indexer._dbContext.nodeSets.Count()}. Not indexed: {indexer._dbContext.nodeSets.Count(n => n.ValidationStatus != ValidationStatus.Indexed)}");
                }
            }
            finally
            {
                bIndexing = false;
            }
        }

        private static async Task<int> IndexNodeSetsInternalAsync(NodeSetModelIndexerFactory factory)
        {
            var nodeSetIndexer = factory.Create();
            try
            {
                await nodeSetIndexer.IndexMissingNodeSets();

                var unvalidatedNodeSets = nodeSetIndexer._dbContext.nodeSets.Where(n => n.ValidationStatus != ValidationStatus.Indexed).ToList();

                int changedCount = 0;
                foreach (var nodeSetModel in unvalidatedNodeSets)
                {
                    try
                    {
                        if (await nodeSetIndexer.IndexNodeSetModelAsync(nodeSetModel.Identifier, nodeSetModel.ModelUri, nodeSetModel.PublicationDate).ConfigureAwait(false))
                        {
                            changedCount++;
                        }
                    }
                    finally
                    {
                        nodeSetIndexer?.Dispose();
                        nodeSetIndexer = null;
                    }
                    nodeSetIndexer = factory.Create();
                }
                return changedCount;
            }
            finally
            {
                nodeSetIndexer?.Dispose();
                nodeSetIndexer = null;
            }
        }

        private async Task<bool> IndexNodeSetModelAsync(string identifier, string modelUri, DateTime? publicationDate)
        {
            bool bChanged = false;
            var nodeSetXml = await _storage.DownloadFileAsync(identifier).ConfigureAwait(false);
            if (nodeSetXml != null)
            {
                var nodeSetModel = await _dbContext.nodeSets.FindAsync(modelUri, publicationDate).ConfigureAwait(false);
                (ValidationStatus Status, string Info) previousValidationStatus = (nodeSetModel.ValidationStatus, nodeSetModel.ValidationStatusInfo);
                try
                {
                    var newValidationStatus = await this.IndexNodeSetModelAsync(nodeSetModel.ModelUri, nodeSetXml, identifier).ConfigureAwait(false);
                    nodeSetModel.ValidationStatus = newValidationStatus.Status;
                    nodeSetModel.ValidationStatusInfo = newValidationStatus.Info;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error indexing {nodeSetModel}");

                    // Abandon any partial indexing so far
                    _dbContext.ChangeTracker.Clear();
                    // Re-acquire the nodeset and update only it's status
                    nodeSetModel = await _dbContext.nodeSets.FindAsync(modelUri, publicationDate).ConfigureAwait(false);
                    nodeSetModel.ValidationStatus = ValidationStatus.Error;
                    nodeSetModel.ValidationStatusInfo = ex.Message;
                }
                if (previousValidationStatus.Status != nodeSetModel.ValidationStatus || previousValidationStatus.Info != nodeSetModel.ValidationStatusInfo)
                {
                    await _dbContext.SaveChangesAsync();
                    _database.UpdateMetaDataForNodeSet(uint.Parse(identifier, CultureInfo.InvariantCulture), "validationstatus", nodeSetModel.ValidationStatus.ToString());
                    bChanged = true;
                }
            }
            return bChanged;
        }

        private async Task IndexMissingNodeSets()
        {
            try
            {
#pragma warning disable CA1305 // Specify IFormatProvider
                //.ToString() runs in the database using a fixed culture/collation
                var missingNodeSetIds = await _dbContext.Metadata
                    .Select(md => md.NodesetId.ToString())
                    .Distinct()
                    .Where(id => !_dbContext.nodeSets.Any(nsm => nsm.Identifier == id))
                    .ToListAsync().ConfigureAwait(false);
#pragma warning restore CA1305 // Specify IFormatProvider
                foreach (var missingNodeSetId in missingNodeSetIds)
                {
                    try
                    {
                        _logger.LogDebug($"Dowloading missing nodeset {missingNodeSetId}");
                        var nodeSetXml = await _storage.DownloadFileAsync(missingNodeSetId).ConfigureAwait(false);

                        _logger.LogDebug($"Parsing missing nodeset {missingNodeSetId}");
                        var uaNodeSet = InfoModelController.ReadUANodeSet(nodeSetXml);

                        _logger.LogDebug($"Indexing missing nodeset {missingNodeSetId}");
                        var nodeSetModel = await this.CreateNodeSetModelFromNodeSetAsync(uaNodeSet, missingNodeSetId).ConfigureAwait(false);

                        _logger.LogWarning($"Database inconsistency: Created index entry for nodeset {missingNodeSetId} {nodeSetModel}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to re-index missing nodeset {missingNodeSetId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to re-index missing nodesets.");
            }
        }

        public void Dispose()
        {
            if (_scope != null)
            {
                try
                {
                    _scope.Dispose();
                }
                catch { }
            }
        }
    }
}
