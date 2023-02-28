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
using System.Linq;
using CESMII.OpcUa.NodeSetImporter;
using CESMII.OpcUa.NodeSetModel.EF;
using Opc.Ua.Cloud.Library.Interfaces;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Make the UANodeSetImporter work over IFileStorage
    /// </summary>
    internal class UANodeSetIFileStorage : IUANodeSetCache
    {
        public UANodeSetIFileStorage(IFileStorage storage, AppDbContext dbContext)
        {
            _storage = storage;
            _dbContext = dbContext;
        }

        private readonly IFileStorage _storage;
        private readonly AppDbContext _dbContext;

        public bool AddNodeSet(UANodeSetImportResult results, string nodeSetXml, object TenantID, bool requested)
        {
            // Assume already added to cloudlib storage before
            var nodeSet = InfoModelController.ReadUANodeSet(nodeSetXml);
            // Assumption: exactly one nodeSetXml was passed into UANodeSetImport.ImportNodeSets and it's the first one being added.
            bool isNew = !results.Models.Any();
            var modelInfo = results.AddModelAndDependencies(nodeSet, nodeSet.Models?[0], null, isNew);
            modelInfo.Model.RequestedForThisImport = requested;
            return modelInfo.Added;
        }

        public void DeleteNewlyAddedNodeSetsFromCache(UANodeSetImportResult results)
        {
            // Not needed as files are stored externally in the cloud library regardless of indexer state
        }

        public UANodeSetImportResult FlushCache()
        {
            throw new System.NotImplementedException();
        }

        public bool GetNodeSet(UANodeSetImportResult results, ModelNameAndVersion nameVersion, object TenantID)
        {
            // Find next higher model if no exact match
            var matchingNodeSet = DbOpcUaContext.GetMatchingOrHigherNodeSetAsync(_dbContext, nameVersion.ModelUri, nameVersion.PublicationDate, nameVersion.ModelVersion).Result as CloudLibNodeSetModel;
            if (matchingNodeSet != null)
            {
                string tFileName = matchingNodeSet.Identifier;
                var nodeSetXml = _storage.DownloadFileAsync(tFileName).Result;
                if (nodeSetXml != null)
                {
                    AddNodeSet(results, nodeSetXml, TenantID, false);
                    return true;
                }
            }
            return false;
        }

        public ModelValue GetNodeSetByID(string id)
        {
            throw new NotImplementedException();
        }

        public string GetRawModelXML(ModelValue model)
        {
            throw new NotImplementedException();
        }
    }
}
