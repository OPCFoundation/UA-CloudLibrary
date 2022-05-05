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

namespace UACloudLibrary
{
    using CESMII.OpcUa.NodeSetImporter;
    using CESMII.OpcUa.NodeSetModel;
    using CESMII.OpcUa.NodeSetModel.Factory.Opc;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class NodeSetModelStore
    {
        public NodeSetModelStore(AppDbContext appDbContext, ILogger logger)
        {
            _appDbContext = appDbContext;
            _logger = logger;
        }
        private readonly AppDbContext _appDbContext;
        private readonly ILogger _logger;

        public async Task StoreNodeSetModelAsync(string nodeSetXML, string identifier)
        {
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

            var cachePath = Path.Combine(Path.GetTempPath(), "CloudLib", "ImporterCache");
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }
            var myNodeSetCache = new UANodeSetFileCache(cachePath);

            var resultSet = UANodeSetImporter.ImportNodeSets(myNodeSetCache, null, new List<string> { nodeSetXML }, false, null, null);

            var opcContext = new DbOpcUaContext(_appDbContext, systemContext, importedNodes, nodesetModels, _logger);

            try
            {

                foreach (var model in resultSet.Models)
                {
                    if (model.NewInThisImport)
                    {
                        var loadedNodesetModels = await NodeModelFactoryOpc.LoadNodeSetAsync(
                            opcContext,
                            model.NodeSet,
                            null, nodesetModels, systemContext, importedNodes, out _, new Dictionary<string, string>(), false);
                        foreach (var nodesetModel in loadedNodesetModels)
                        {
                            nodesetModel.Identifier = identifier;
                            _appDbContext.nodeSets.Add(nodesetModel);
                        }
                        await _appDbContext.SaveChangesAsync();
                    }
                    else
                    {
                        var nodeSetModel = _appDbContext.Set<NodeSetModel>().Where(m => m.ModelUri == model.NameVersion.ModelUri && m.PublicationDate == model.NameVersion.PublicationDate)
                            //.Include(m => m.Objects)
                            //.Include(m => m.ObjectTypes)
                            //.Include(m => m.DataTypes)
                            //.Include(m => m.DataVariables)
                            //.Include(m => m.Properties)
                            //.Include(m => m.UnknownNodes)
                            //.Include(m => m.Interfaces)
                            .FirstOrDefault();
                        if (nodeSetModel == null)
                        {
                            throw new Exception("NodeSet not in database: Inconsistency between file store and db?");
                        }
                        model.NodeSet.Import(systemContext, importedNodes);
                        //nodeSetModel.UpdateIndices();
                        nodesetModels.Add(model.NameVersion.ModelUri, nodeSetModel);
                    }
                }
            }
            catch
            {
                myNodeSetCache.DeleteNewlyAddedNodeSetsFromCache(resultSet);
                throw;
            }
        }
    }
}
