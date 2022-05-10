/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
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

namespace Opc.Ua.Cloud.Library
{
    using CESMII.OpcUa.NodeSetImporter;
    using CESMII.OpcUa.NodeSetModel;
    using CESMII.OpcUa.NodeSetModel.Factory.Opc;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Opc.Ua.Cloud.Library.Interfaces;


    public class NodeSetModelStoreFactory
    {
        public NodeSetModelStoreFactory(IServiceScopeFactory serviceScopeFactory, ILogger<NodeSetModelStore> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;//Factory.CreateLogger<NodeSetModelStore>();
        }

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<NodeSetModelStore> _logger;

        public NodeSetModelStore Create()
        {
            var scope = _serviceScopeFactory.CreateScope();
            var appDbContext = scope.ServiceProvider.GetService<AppDbContext>();
            var fileStore = scope.ServiceProvider.GetService<IFileStorage>();
            var database = scope.ServiceProvider.GetService<IDatabase>();
            return new NodeSetModelStore(appDbContext, _logger, fileStore, database, scope);
        }
    }

    public class NodeSetModelStore: IDisposable
    {
        public NodeSetModelStore(AppDbContext appDbContext, ILogger<NodeSetModelStore> logger, IFileStorage storage, IDatabase database, IServiceScope scope = null)
        {
            _appDbContext = appDbContext;
            _logger = logger;
            _storage = storage;
            _database = database;
            _scope = scope;
        }
        private AppDbContext _appDbContext;
        private readonly ILogger _logger;
        private readonly IFileStorage _storage;
        private readonly IDatabase _database;
        private readonly IServiceScope _scope;

        public async Task<string> StoreNodeSetModelAsync(string modelUri, string nodeSetXML, string identifier)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation($"Starting nodeset validation for {modelUri} {identifier}");
            var operationContext = new SystemContext();
            var namespaceTable = new NamespaceTable();
            const string strOpcNamespaceUri = "http://opcfoundation.org/UA/";

            namespaceTable.GetIndexOrAppend(strOpcNamespaceUri);
            var typeTable = new TypeTable(namespaceTable);
            var systemContext = new SystemContext(operationContext)
            {
                NamespaceUris = namespaceTable,
                TypeTable = typeTable,
            };

            var importedNodes = new NodeStateCollection();
            var nodesetModels = new Dictionary<string, NodeSetModel>();

            var myNodeSetCache = new UANodeSetIFileStorage(_storage, _appDbContext);

            var resultSet = UANodeSetImporter.ImportNodeSets(myNodeSetCache, null, new List<string> { nodeSetXML }, false, null, null);
            if (!string.IsNullOrEmpty(resultSet.ErrorMessage))
            {
                return $"Error: {resultSet.ErrorMessage}";
            }

            var importedTime = sw.ElapsedMilliseconds;
            _logger.LogInformation($"Finished importing files for {modelUri} {identifier}: {importedTime} ms");
            //_appDbContext = _scope.ServiceProvider.GetService<AppDbContext>();

            var opcContext = new DbOpcUaContext(_appDbContext, systemContext, importedNodes, nodesetModels, _logger);

            try
            {

                foreach (var model in resultSet.Models)
                {
                    if (model.NewInThisImport || model.NameVersion.ModelUri == modelUri)
                    {
                        var nodeSetModel = _appDbContext.nodeSets.Where(m => m.ModelUri == model.NameVersion.ModelUri && m.PublicationDate == model.NameVersion.PublicationDate)?.FirstOrDefault();
                        if (nodeSetModel != null)
                        {
                            continue;
                            //throw new Exception($"NodeSet {model.NameVersion.ModelUri} {model.NameVersion.PublicationDate} already indexed.");
                        }

                        var loadedNodesetModels = await NodeModelFactoryOpc.LoadNodeSetAsync(
                            opcContext,
                            model.NodeSet,
                            null, nodesetModels, systemContext, importedNodes, out _, new Dictionary<string, string>(), false);
                        foreach (var nodesetModel in loadedNodesetModels)
                        {
                            nodesetModel.Identifier = identifier;
                            if (_appDbContext.nodeSets.Where(m => m.ModelUri == model.NameVersion.ModelUri && m.PublicationDate == model.NameVersion.PublicationDate)?.FirstOrDefault() == null)
                            {
                                _appDbContext.nodeSets.Add(nodesetModel);
                            }
                            else
                            {
                                // TODO Why are we hitting this?
                            }
                        }
                        var validatedTime = sw.ElapsedMilliseconds;
                        _logger.LogInformation($"Finished validating nodeset {modelUri} {identifier}: {validatedTime - importedTime} ms. Total: {validatedTime} ms.");

                        await _appDbContext.SaveChangesAsync();
                        var savedTime = sw.ElapsedMilliseconds;
                        _logger.LogInformation($"Finished indexing nodeset {modelUri} {identifier}: {savedTime - validatedTime} ms. Total: {savedTime} ms.");
                    }
                    else
                    {
                        var nodeSetModel = _appDbContext.Set<NodeSetModel>().Where(m => m.ModelUri == model.NameVersion.ModelUri && m.PublicationDate == model.NameVersion.PublicationDate).FirstOrDefault();
                        if (nodeSetModel == null)
                        {
                            throw new Exception("NodeSet not in database: Inconsistency between file store and db?");
                        }
                        model.NodeSet.Import(systemContext, importedNodes);
                        //nodeSetModel.UpdateIndices();
                        nodesetModels.Add(model.NameVersion.ModelUri, nodeSetModel);
                    }
                }
                return "Validated";

            }
            catch
            {
                myNodeSetCache.DeleteNewlyAddedNodeSetsFromCache(resultSet);
                throw;
            }
        }

        static bool bIndexing = false;
        static object _indexingLock = new object();
        public  async Task IndexNodeSetsAsync()
        {
            try
            {
                lock (_indexingLock)
                {
                    if (bIndexing) return;
                    bIndexing = true;
                }
                bool bChanged;
                do
                {
                    var allNodeSets = _database.FindNodesets(new[] { "*" });
                    var unvalidatedNodeSets = allNodeSets.Where(n => n.ValidationStatus != "Validated").ToList();
                    bChanged = false;
                    foreach (var nodeSet in unvalidatedNodeSets)
                    {
                        var identifier = nodeSet.Id.ToString(CultureInfo.InvariantCulture);
                        var nodeSetXml = await _storage.DownloadFileAsync(identifier).ConfigureAwait(false);
                        if (nodeSetXml != null)
                        {
                            string modelValidationStatus;
                            try
                            {
                                modelValidationStatus = await this.StoreNodeSetModelAsync(nodeSet.NameSpaceUri, nodeSetXml, identifier).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                modelValidationStatus = $"Error: {ex.Message}";
                            }
                            if (modelValidationStatus != nodeSet.ValidationStatus)
                            {
                                bChanged = true;
                                _database.UpdateMetaDataForNodeSet(nodeSet.Id, "validationstatus", modelValidationStatus);
                            }
                        }
                    }
                } while (bChanged);
            }
            catch (Exception)
            {
            }
            finally
            {
                bIndexing = false;
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
