/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
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
using System.Linq;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Cloud.Library;

namespace AdminShell
{
    public class AssetAdministrationShellEnvironmentService
    {
        private readonly UAClient _client;
        private readonly CloudLibDataProvider _dataProvider;

        public AssetAdministrationShellEnvironmentService(UAClient client, CloudLibDataProvider dataProvider)
        {
            _client = client;
            _dataProvider = dataProvider;
        }

        public async Task<Submodel> GetSubmodelById(string userId, string idShort)
        {
            List<NodesetViewerNode> nodeList = await _client.GetChildren(userId, idShort, ObjectIds.ObjectsFolder.ToString()).ConfigureAwait(false);
            if (nodeList != null)
            {
                foreach (NodesetViewerNode node in nodeList)
                {
                    if (node.Text == "Submodels")
                    {
                        List<NodesetViewerNode> submodelList = await _client.GetChildren(userId, idShort, node.Id).ConfigureAwait(false);
                        if (submodelList != null)
                        {
                            if (submodelList.Count > 1)
                            {
                                throw new NotImplementedException($"Currently only a single Submodel per OPC UA nodeset file is supported.");
                            }

                            foreach (NodesetViewerNode subNode in submodelList)
                            {
                                Submodel sub = new() {
                                    SemanticId = subNode.Text
                                };

                                // get all submodel elements
                                sub.SubmodelElements.AddRange(await ReadSubmodelElementNodes(userId, idShort, subNode, true).ConfigureAwait(false));

                                return sub;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private async Task<List<SubmodelElement>> ReadSubmodelElementNodes(string userId, string idShort, NodesetViewerNode subNode, bool browseDeep)
        {
            List<SubmodelElement> output = new();

            List<NodesetViewerNode> submodelElementNodes = await _client.GetChildren(userId, idShort, subNode.Id).ConfigureAwait(false);
            if (submodelElementNodes != null)
            {
                foreach (NodesetViewerNode smeNode in submodelElementNodes)
                {
                    if (browseDeep)
                    {
                        // check for children - if there are, create a smel instead of an sme
                        List<SubmodelElement> children = await ReadSubmodelElementNodes(userId, idShort, smeNode, browseDeep).ConfigureAwait(false);
                        if (children.Count > 0)
                        {
                            SubmodelElementList smel = new() {
                                SemanticId = subNode.Text,
                            };

                            smel.Value.AddRange(children);

                            output.Add(smel);
                        }
                        else
                        {
                            AASProperty sme = new() {
                                SemanticId = smeNode.Text,
                                Value = await _client.VariableRead(userId, idShort, smeNode.Id).ConfigureAwait(false)
                            };

                            output.Add(sme);
                        }
                    }
                    else
                    {
                        // add just one property to be spec conform
                        AASProperty sme = new() {
                            SemanticId = smeNode.Text
                        };

                        output.Add(sme);
                    }
                }
            }

            return output;
        }
    }
}
