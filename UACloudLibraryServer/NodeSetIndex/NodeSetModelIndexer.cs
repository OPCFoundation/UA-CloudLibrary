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
            return new NodeSetModelIndexer(appDbContext, _logger, fileStore, database, scope);
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
            var sw = Stopwatch.StartNew();
            _logger.LogInformation($"Starting nodeset validation for {modelUri} {identifier}");

            var myNodeSetCache = new UANodeSetIFileStorage(_storage, _dbContext);
            var resultSet = UANodeSetImporter.ImportNodeSets(myNodeSetCache, null, new List<string> { nodeSetXML }, false, null, null);
            if (!string.IsNullOrEmpty(resultSet.ErrorMessage))
            {
                return (ValidationStatus.Error, $"{resultSet.ErrorMessage}");
            }

            var importedTime = sw.ElapsedMilliseconds;
            _logger.LogInformation($"Finished importing files for {modelUri} {identifier}: {importedTime} ms");

            var operationContext = new SystemContext();
            var namespaceTable = new NamespaceTable();
            const string strOpcNamespaceUri = "http://opcfoundation.org/UA/";
            namespaceTable.GetIndexOrAppend(strOpcNamespaceUri);
            var typeTable = new TypeTable(namespaceTable);
            var systemContext = new SystemContext(operationContext) {
                NamespaceUris = namespaceTable,
                TypeTable = typeTable,
            };
            var importedNodes = new NodeStateCollection();
            var nodesetModels = new Dictionary<string, NodeSetModel>();

            var opcContext = new DbOpcUaContext(_dbContext, systemContext, importedNodes, nodesetModels, _logger);

            try
            {
                ValidationStatus validationStatus = ValidationStatus.Error;
                string validationStatusInfo = "Internal error";
                foreach (var model in resultSet.Models)
                {
                    var existingNodeSetModel = await _dbContext.nodeSets.FindAsync(model.NameVersion.ModelUri, model.NameVersion.PublicationDate);
                    if (existingNodeSetModel != null)
                    {
                        nodesetModels.Add(existingNodeSetModel.ModelUri, existingNodeSetModel);
                    }
                    else
                    {
                        var nodeSetModel = await CloudLibNodeSetModel.FromModelAsync(model.NodeSet.Models.FirstOrDefault(), _dbContext).ConfigureAwait(false);
                        nodesetModels.Add(model.NameVersion.ModelUri, nodeSetModel);
                    }
                    if (model.NewInThisImport || model.NameVersion.ModelUri == modelUri)
                    {
                        var loadedNodesetModels = await NodeModelFactoryOpc.LoadNodeSetAsync(
                            opcContext,
                            model.NodeSet,
                            null, nodesetModels, systemContext, importedNodes, out _, new Dictionary<string, string>(), true);
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
                            if (existingNodeSetModel == null)
                            {
                                _dbContext.nodeSets.Add(clNodeSet);
                            }
                        }
                        var validatedTime = sw.ElapsedMilliseconds;
                        _logger.LogInformation($"Finished validating nodeset {modelUri} {identifier}: {validatedTime - importedTime} ms. Total: {validatedTime} ms.");

                        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                        var savedTime = sw.ElapsedMilliseconds;
                        _logger.LogInformation($"Finished indexing nodeset {modelUri} {identifier}: {savedTime - validatedTime} ms. Total: {savedTime} ms.");
#if DEBUG
                        if (nodesetModels.TryGetValue(modelUri, out var nodeSet))
                        {
                            var clNodeSet = nodeSet as CloudLibNodeSetModel;
                            clNodeSet.ValidationStatusInfo = validationStatusInfo = $"Indexing time: {((double)savedTime)/1000:N3}s";
                            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                        }
#endif
                    }
                    else
                    {
                        if (existingNodeSetModel == null)
                        {
                            throw new Exception($"Required NodeSet {existingNodeSetModel} not in database: Inconsistency between file store and db?");
                        }
                        if (existingNodeSetModel.ValidationStatus != ValidationStatus.Indexed)
                        {
                            return (ValidationStatus.Error, $"Required NodeSet {existingNodeSetModel} not indexed yet.");
                        }
                        model.NodeSet.Import(systemContext, importedNodes);
                    }
                }
                return (validationStatus, validationStatusInfo);
            }
            catch
            {
                myNodeSetCache.DeleteNewlyAddedNodeSetsFromCache(resultSet);
                throw;
            }
        }

        public async Task CreateNodeSetModelFromNodeSet(UANodeSet nodeSet, string identifier)
        {
            var nodeSetModel = await CloudLibNodeSetModel.FromModelAsync(nodeSet.Models[0], _dbContext).ConfigureAwait(false);
            nodeSetModel.Identifier = identifier;
            nodeSetModel.ValidationStatus = ValidationStatus.Parsed;
            nodeSetModel.ValidationStatusInfo = null;
            await _dbContext.nodeSets.AddAsync(nodeSetModel);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteNodeSetIndex(string identifier)
        {
            var existingNodeSet = _dbContext.nodeSets.AsQueryable().Where(n => n.Identifier == identifier).FirstOrDefault();
            if (existingNodeSet != null)
            {
                var nodes = _dbContext.nodeModels.Where(nm => nm.NodeSet == existingNodeSet).ToList();
                foreach (var node in nodes)
                {
                    var referencesToNode = await _dbContext.nodeModels.Where(nm => nm.OtherChilden.Any(c => c.Child == node)).ToListAsync().ConfigureAwait(false);
                    foreach (var referencingNode in referencesToNode)
                    {
                        referencingNode.OtherChilden.RemoveAll(reference => reference.Child == node);
                        _dbContext.Update(referencingNode);
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
                bool bChanged;
                do
                {
                    bChanged = false;
                    var nodeSetIndexer = factory.Create();
                    try
                    {
                        bChanged = await nodeSetIndexer.IndexNodeSetsAsync();
                    }
                    finally
                    {
                        nodeSetIndexer?.Dispose();
                    }
                } while (bChanged);
            }
            finally
            {
                bIndexing = false;
            }
        }

        private async Task<bool> IndexNodeSetsAsync()
        {
            var bChanged = false;
            var unvalidatedNodeSets = _dbContext.nodeSets.Where(n => n.ValidationStatus != ValidationStatus.Indexed).ToList();
            foreach (var nodeSetModel in unvalidatedNodeSets)
            {
                var identifier = nodeSetModel.Identifier;
                var nodeSetXml = await _storage.DownloadFileAsync(identifier).ConfigureAwait(false);
                if (nodeSetXml != null)
                {
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
                        nodeSetModel.ValidationStatus = ValidationStatus.Error;
                        nodeSetModel.ValidationStatusInfo = ex.Message;
                    }
                    if (previousValidationStatus.Status != nodeSetModel.ValidationStatus || previousValidationStatus.Info != nodeSetModel.ValidationStatusInfo)
                    {
                        bChanged = true;
                        await _dbContext.SaveChangesAsync();
                        _database.UpdateMetaDataForNodeSet(uint.Parse(identifier, CultureInfo.InvariantCulture), "validationstatus", nodeSetModel.ValidationStatus.ToString());
                    }
                }
            }
            return bChanged;
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
