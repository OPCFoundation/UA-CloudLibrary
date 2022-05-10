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

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CESMII.OpcUa.NodeSetImporter;
using Extensions;
using Opc.Ua.Export;
using Opc.Ua.Cloud.Library.Interfaces;

namespace Opc.Ua.Cloud.Library
{
    internal class UANodeSetIFileStorage : IUANodeSetCache
    {
        public UANodeSetIFileStorage(IFileStorage storage, AppDbContext dbContext)
        {
            _storage = storage;
            _dbContext = dbContext;
        }

        private readonly IFileStorage _storage;
        private readonly AppDbContext _dbContext;

        public bool AddNodeSet(UANodeSetImportResult results, string nodeSetXml, object TenantID)
        {
            // Assume already added to cloudlib storage before
            var nodeSet = new UANodeSet();
            // workaround for bug https://github.com/dotnet/runtime/issues/67622
            var patchedXML = nodeSetXml.Replace("<Value/>", "<Value xsi:nil='true' />", StringComparison.OrdinalIgnoreCase);
            using (var nodesetBytes = new MemoryStream(Encoding.UTF8.GetBytes(patchedXML)))
            {
                nodeSet = UANodeSet.Read(nodesetBytes);
            }
            results.AddModelAndDependencies(nodeSet, nodeSet.Models?[0], null, false);
            return false;
        }

        public void DeleteNewlyAddedNodeSetsFromCache(UANodeSetImportResult results)
        {
        }

        public UANodeSetImportResult FlushCache()
        {
            throw new System.NotImplementedException();
        }

        public bool GetNodeSet(UANodeSetImportResult results, ModelNameAndVersion nameVersion, object TenantID)
        {
            // Find next higher model if no exact match
            var matchingNodeSet = _dbContext.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == nameVersion.ModelUri && nsm.PublicationDate >= nameVersion.PublicationDate).OrderBy(nsm => nsm.PublicationDate).FirstOrDefault();
            if (matchingNodeSet != null)
            {
                string tFileName = matchingNodeSet.Identifier;
                var nodeSetXml = _storage.DownloadFileAsync(tFileName).Result;
                if (nodeSetXml != null)
                {
                    AddNodeSet(results, nodeSetXml, TenantID);
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
