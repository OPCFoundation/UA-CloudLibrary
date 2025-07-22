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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Interfaces;
using Opc.Ua.Cloud.Library.NodeSetIndex;

namespace Opc.Ua.Cloud.Library
{
    public class NodeSetModelIndexer
    {
        private AppDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly IFileStorage _storage;

        public NodeSetModelIndexer(AppDbContext dbContext, ILoggerFactory logger, IFileStorage storage)
        {
            _dbContext = dbContext;
            _logger = logger.CreateLogger("NodeSetModelIndexer");
            _storage = storage;
        }

        public async Task<(ValidationStatus Status, string Info)> IndexNodeSetModelAsync(string modelUri, string nodeSetXML, string identifier)
        {
            try
            {
                ValidationStatus validationStatus = ValidationStatus.Error;
                string validationStatusInfo = "Internal error";

                var sw = Stopwatch.StartNew();

                var myNodeSetCache = new NodeSetImporter(_storage, _dbContext);
                var opcContext = new CloudLibDbOpcUaContext(_dbContext, _logger, model => CloudLibNodeSetModel.FromModelAsync(model, _dbContext).Result);
                var nodesetImporter = new NodeSetModelImporter((IOpcUaContext)opcContext, myNodeSetCache);

                List<NodeSetModel> loadedNodesetModels = await nodesetImporter.ImportNodeSetModelAsync(nodeSetXML, identifier).ConfigureAwait(false);

                foreach (NodeSetModel nodeSetModel in loadedNodesetModels)
                {
                    nodeSetModel.Identifier = identifier;
                    var clNodeSet = nodeSetModel as CloudLibNodeSetModel;
                    if (clNodeSet == null)
                    {
                        throw new ArgumentException($"Internal error: unexpected nodeset model type {nodeSetModel.GetType()}");
                    }

                    clNodeSet.ValidationStatus = validationStatus = ValidationStatus.Indexed;
                    clNodeSet.ValidationStatusInfo = validationStatusInfo = null;

                    if (!_dbContext.Set<NodeSetModel>().Any(nsm => nsm.ModelUri == clNodeSet.ModelUri && nsm.PublicationDate == clNodeSet.PublicationDate))
                    {
                        _dbContext.nodeSetsWithUnapproved.Add(clNodeSet);
                    }
                    else
                    {
                        _dbContext.nodeSetsWithUnapproved.Update(clNodeSet);
                    }
                }

                long validatedTime = sw.ElapsedMilliseconds;
                _logger.LogInformation($"Finished validating nodeset {modelUri} {identifier}: {validatedTime} ms.");

                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                long savedTime = sw.ElapsedMilliseconds;
                _logger.LogInformation($"Finished indexing nodeset {modelUri} {identifier}: {savedTime - validatedTime} ms. Total: {savedTime} ms.");
#if DEBUG
                NodeSetModel nodeSet = loadedNodesetModels.FirstOrDefault(nsm => nsm.ModelUri == modelUri);
                if (nodeSet != null)
                {
                    var clNodeSet = nodeSet as CloudLibNodeSetModel;
                    clNodeSet.ValidationElapsedTime = TimeSpan.FromMilliseconds(savedTime);
                    clNodeSet.ValidationFinishedTime = DateTime.UtcNow;
                    _dbContext.Update(clNodeSet);
                    await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
#endif
                return (validationStatus, validationStatusInfo);
            }
            catch (Exception ex)
            {
                return (ValidationStatus.Error, ex.Message);
            }
        }
    }
}
