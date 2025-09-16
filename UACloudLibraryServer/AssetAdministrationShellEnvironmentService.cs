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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Cloud.Library;
using Opc.Ua.Export;

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

        public List<AssetAdministrationShellDescriptor> GetAllAssetAdministrationShellDescriptors(string userId)
        {
            List<AssetAdministrationShellDescriptor> output = new();

            // Query database for all Asset Administration Shells
            List<ObjectModel> aasList = _dataProvider.GetNodeModels(nsm => nsm.Objects, userId).Where(nsm => nsm.DisplayName[0].Text == "Asset Admin Shells").ToList();
            if (aasList != null)
            {
                // Loop through all the Asset Administration Shells we found above.
                foreach (ObjectModel aas in aasList)
                {
                    List<Endpoint> aasEndpoints = new() {
                        new Endpoint() {
                            Interface = "AAS-1.0",
                            ProtocolInformation = new ProtocolInformation() {
                                Href = $"http://example.com/idta/shells/{aas.NodeSet.Identifier}"
                            }
                        }
                    };

                    AssetAdministrationShellDescriptor aasDescriptor = new() {
                        AssetKind = AssetKind.Instance,
                        AssetType = "Not Applicable",
                        Endpoints = aasEndpoints,
                        GlobalAssetId = $"http://example.com/idta/ids/{aas.NodeSet.Identifier}",
                        IdShort = aas.NodeSet.Identifier,
                        Id = aas.NodeId,
                        SpecificAssetIds = new List<SpecificAssetId>(), // TODO: fill this from database
                        SubmodelDescriptors = new List<SubmodelDescriptor>()
                    };

                    if (aas.AllReferencedNodes != null)
                    {
                        foreach (var reference in aas.AllReferencedNodes)
                        {
                            ObjectModel submodel = _dataProvider.GetNodeSets(userId, aas.NodeSet.Identifier)
                                .FirstOrDefault()?.Objects
                                .FirstOrDefault(o => (o.Namespace == reference.Node.Namespace) && (o.NodeId == reference.Node.NodeId));
                            if (submodel != null)
                            {
                                List<Endpoint> submodelEndpoints = new() {
                                    new Endpoint() {
                                        Interface = "SUBMODEL-1.0",
                                        ProtocolInformation = new ProtocolInformation() {
                                            Href = $"http://example.com/idta/shells/{submodel.NodeSet.Identifier}"
                                        }
                                    }
                                };

                                SubmodelDescriptor submodelDescriptor = new() {
                                    Endpoints = submodelEndpoints,
                                    IdShort = submodel.NodeSet.Identifier,
                                    Id = $"http://example.com/idta/ids/{submodel.NodeSet.Identifier}",
                                    SemanticId = new Reference {
                                        Keys = new List<Key> {
                                        new Key("GlobalReference", $"http://example.com/idta/submodels/{submodel.NodeSet.Identifier}")
                                    }
                                    }
                                };

                                aasDescriptor.SubmodelDescriptors.Add(submodelDescriptor);
                            }
                        }
                    }

                    output.Add(aasDescriptor);
                }
            }

            return output;
        }

        public AssetAdministrationShellDescriptor GetAssetAdministrationShellDescriptorById(string userId, string idShort)
        {
            ObjectModel aas = _dataProvider.GetNodeSets(userId, idShort).FirstOrDefault()?.Objects.FirstOrDefault(o => o.DisplayName[0].Text == "Asset Admin Shells");
            if (aas != null)
            {
                List<Endpoint> aasEndpoints = new() {
                    new Endpoint() {
                        Interface = "AAS-1.0",
                        ProtocolInformation = new ProtocolInformation() {
                            Href = $"http://example.com/idta/shells/{aas.NodeSet.Identifier}"
                        }
                    }
                };

                AssetAdministrationShellDescriptor aasDescriptor = new() {
                    AssetKind = AssetKind.Instance,
                    AssetType = "Not Applicable",
                    Endpoints = aasEndpoints,
                    GlobalAssetId = $"http://example.com/idta/ids/{aas.NodeSet.Identifier}",
                    IdShort = aas.NodeSet.Identifier,
                    Id = aas.NodeId,
                    SpecificAssetIds = new List<SpecificAssetId>(), // TODO: fill this from database
                    SubmodelDescriptors = new List<SubmodelDescriptor>()
                };

                if (aas.AllReferencedNodes != null)
                {
                    foreach (var reference in aas.AllReferencedNodes)
                    {
                        ObjectModel submodel = _dataProvider.GetNodeSets(userId, aas.NodeSet.Identifier)
                            .FirstOrDefault()?.Objects
                            .FirstOrDefault(o => (o.Namespace == reference.Node.Namespace) && (o.NodeId == reference.Node.NodeId));
                        if (submodel != null)
                        {
                            List<Endpoint> submodelEndpoints = new() {
                                new Endpoint() {
                                    Interface = "SUBMODEL-1.0",
                                    ProtocolInformation = new ProtocolInformation() {
                                        Href = $"http://example.com/idta/shells/{submodel.NodeSet.Identifier}"
                                    }
                                }
                            };

                            SubmodelDescriptor submodelDescriptor = new() {
                                Endpoints = submodelEndpoints,
                                IdShort = submodel.NodeSet.Identifier,
                                Id = $"http://example.com/idta/ids/{submodel.NodeSet.Identifier}",
                                SemanticId = new Reference {
                                    Keys = new List<Key> {
                                    new Key("GlobalReference", $"http://example.com/idta/submodels/{submodel.NodeSet.Identifier}")
                                }
                                }
                            };

                            aasDescriptor.SubmodelDescriptors.Add(submodelDescriptor);
                        }
                    }
                }

                return aasDescriptor;
            }

            return null;
        }

        public List<SubmodelDescriptor> GetAllSubmodelDescriptors(string userId, string idShort)
        {
            List<SubmodelDescriptor> output = new();

            // Query database for all Submodels
            List<ObjectModel> submodelList = _dataProvider.GetNodeModels(nsm => nsm.Objects, userId).Where(nsm => nsm.DisplayName[0].Text == "Submodels").ToList();
            if (submodelList != null)
            {
                // Loop through all the submodels we found above.
                foreach (ObjectModel submodel in submodelList)
                {
                    List<Endpoint> submodelEndpoints = new() {
                        new Endpoint() {
                            Interface = "SUBMODEL-1.0",
                            ProtocolInformation = new ProtocolInformation() {
                                Href = $"http://example.com/idta/submodels/{submodel.NodeSet.Identifier}"
                            }
                        }
                    };

                    SubmodelDescriptor submodelDescriptor = new() {
                        Endpoints = submodelEndpoints,
                        IdShort = submodel.NodeSet.Identifier,
                        Id = $"http://example.com/idta/ids/{submodel.NodeSet.Identifier}",
                        SemanticId = new Reference {
                            Keys = new List<Key> {
                                new Key("GlobalReference", $"http://example.com/idta/submodels/{submodel.NodeSet.Identifier}")
                            }
                        }
                    };

                    output.Add(submodelDescriptor);
                }
            }

            return output;
        }

        public SubmodelDescriptor GetSubmodelDescriptorById(string userId, string idShort)
        {
            ObjectModel submodel = _dataProvider.GetNodeSets(userId, idShort).FirstOrDefault()?.Objects.FirstOrDefault(o => o.DisplayName[0].Text == "Submodels");
            if (submodel != null)
            {
                List<Endpoint> submodelEndpoints = new() {
                    new Endpoint() {
                        Interface = "SUBMODEL-1.0",
                        ProtocolInformation = new ProtocolInformation() {
                            Href = $"http://example.com/idta/submodels/{submodel.NodeSet.Identifier}"
                        }
                    }
                };

                SubmodelDescriptor submodelDescriptor = new() {
                    Endpoints = submodelEndpoints,
                    IdShort = submodel.NodeSet.Identifier,
                    Id = $"http://example.com/idta/ids/{submodel.NodeSet.Identifier}",
                    SemanticId = new Reference {
                        Keys = new List<Key> {
                            new Key("GlobalReference", $"http://example.com/idta/submodels/{submodel.NodeSet.Identifier}")
                        }
                    }
                };

                return submodelDescriptor;
            }

            return null;
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
                                    ModelType = ModelTypes.Submodel,
                                    Id = subNode.Id,
                                    Identification = new Identifier() { Id = subNode.Id, Value = subNode.Text },
                                    IdShort = subNode.Id + ";" + subNode.Text,
                                    SemanticId = new Reference() { Type = KeyElements.ExternalReference, Keys = new List<Key>() { new Key() { Value = subNode.Text, Type = KeyElements.GlobalReference } } },
                                    DisplayName = new List<LangString>() { new LangString() { Text = subNode.Text } },
                                    Description = new List<LangString>() { new LangString() { Text = await _client.VariableRead(userId, idShort, subNode.Id).ConfigureAwait(false) } }
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
                                ModelType = ModelTypes.SubmodelElementCollection,
                                DisplayName = new List<LangString>() { new LangString() { Text = smeNode.Text } },
                                IdShort = smeNode.Text,
                                SemanticId = new SemanticId() { Type = KeyElements.ExternalReference, Keys = new List<Key>() { new Key() { Value = smeNode.Text, Type = KeyElements.GlobalReference } } }
                            };

                            smel.Value.AddRange(children);

                            output.Add(smel);
                        }
                        else
                        {
                            AASProperty sme = new() {
                                ModelType = ModelTypes.Property,
                                DisplayName = new List<LangString>() { new LangString() { Text = smeNode.Text } },
                                IdShort = smeNode.Text,
                                SemanticId = new SemanticId() { Type = KeyElements.ExternalReference, Keys = new List<Key>() { new Key() { Value = smeNode.Text, Type = KeyElements.GlobalReference } } },
                                Value = await _client.VariableRead(userId, idShort, smeNode.Id).ConfigureAwait(false)
                            };

                            output.Add(sme);
                        }
                    }
                    else
                    {
                        // add just one property to be spec conform
                        AASProperty sme = new() {
                            ModelType = ModelTypes.Property,
                            DisplayName = new List<LangString>() { new LangString() { Text = smeNode.Text } },
                            IdShort = smeNode.Text,
                            SemanticId = new SemanticId() { Type = KeyElements.ExternalReference, Keys = new List<Key>() { new Key() { Value = smeNode.Text, Type = KeyElements.GlobalReference } } }
                        };

                        output.Add(sme);
                    }
                }
            }

            return output;
        }
    }
}
