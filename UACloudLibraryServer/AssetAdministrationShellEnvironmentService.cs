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
        private readonly CloudLibDataProvider _cldata;

        public AssetAdministrationShellEnvironmentService(UAClient client, CloudLibDataProvider cldata)
        {
            _client = client;
            _cldata = cldata;
        }

        public List<NodesetViewerNode> GetAllNodesetsOfType(string userId, string strType)
        {
            List<NodesetViewerNode> result = new();

            var allNodesets = _cldata.GetNodeSets(userId);
            foreach (var nodeset in allNodesets)
            {
                foreach (var nodesetObject in nodeset.Objects)
                {
                    NodesetViewerNode nsvNode = new();
                    if (nodesetObject.DisplayName[0].Text == strType)
                    {
                        nsvNode.Id = nodeset.Identifier;
                        nsvNode.Value = nodesetObject.NodeId;
                        nsvNode.Text = nodesetObject.Namespace;
                        nsvNode.DisplayName = nodesetObject.DisplayName?.FirstOrDefault()?.Text ?? string.Empty;
                        nsvNode.Description = nodesetObject.Description?.FirstOrDefault()?.Text ?? string.Empty;

                        result.Add(nsvNode);
                    }
                }
            }

            return result;
        }

        public List<NodesetViewerNode> GetNodesetOfTypeById(string userId, string strType, string idShort)
        {
            List<NodesetViewerNode> nsvnReturn = new();

            // Database functions expect 'null' when value not provided.
            idShort = string.IsNullOrEmpty(idShort) ? null : idShort;

            var allNodesets = _cldata.GetNodeSets(userId, idShort);
            foreach (var nodeset in allNodesets)
            {
                foreach (var nodesetObject in nodeset.Objects)
                {
                    if (nodesetObject.DisplayName[0].Text == strType)
                    {
                        NodesetViewerNode nsvNode = new();
                        nsvNode.Id = nodeset.Identifier;
                        nsvNode.Value = nodesetObject.NodeId;
                        nsvNode.Text = nodesetObject.Namespace;
                        nsvNode.DisplayName = nodesetObject.DisplayName?.FirstOrDefault()?.Text ?? string.Empty;
                        nsvNode.Description = nodesetObject.Description?.FirstOrDefault()?.Text ?? string.Empty;
                        nsvnReturn.Add(nsvNode);
                    }
                }
            }

            return nsvnReturn;
        }

        public List<AssetAdministrationShellDescriptor> GetAllAssetAdministrationShellDescriptors(string userId)
        {
            List<AssetAdministrationShellDescriptor> output = new();

            // Query database for all Asset Administration Shells
            List<NodesetViewerNode> aasList = GetAllNodesetsOfType(userId, "Asset Admin Shells");

            if (aasList != null)
            {
                // Loop through all the Asset Administration Shells we found above.
                foreach (NodesetViewerNode nsvNode in aasList)
                {
                    string strNodesetId = nsvNode.Id;

                    List<Endpoint> listEPs = new();
                    Endpoint ep = new Endpoint() {
                        Interface = "AAS-1.0",
                        ProtocolInformation = new ProtocolInformation() { Href = $"https://v3.2.example.com/shells/{strNodesetId}" },

                    };
                    listEPs.Add(ep);

                    List<SpecificAssetId> listSai = new();
                    SpecificAssetId sai = new SpecificAssetId() {
                        Name = "assetKind",
                        Value = "Instance",
                        ExternalSubjectId = new Reference() { Type = KeyElements.GlobalReference },
                    };

                    AssetAdministrationShellDescriptor aasd = new() {
                        AssetKind = AssetKind.Instance,
                        AssetType = "Not Applicable",
                        Endpoints = listEPs,
                        GlobalAssetId = $"https://example.com/ids/{strNodesetId}",
                        IdShort = nsvNode.Id,
                        Id = nsvNode.Text + ";" + nsvNode.Value
                    };

                    aasd.SubmodelDescriptors = new List<SubmodelDescriptor>();
                    List<NodesetViewerNode> listSubmodel = GetNodesetOfTypeById(userId, "Submodels", strNodesetId);
                    if (listSubmodel != null)
                    {
                        foreach (NodesetViewerNode nsvbsubmodel in listSubmodel)
                        {
                            if (nsvbsubmodel != null)
                            {
                                List<Endpoint> listSmdep = new();
                                Endpoint smdep = new Endpoint() {
                                    Interface = "SUBMODEL-1.0",
                                    ProtocolInformation = new ProtocolInformation() { Href = $"https://v3.2.example.com/shells/{strNodesetId}" },

                                };
                                listSmdep.Add(smdep);
                                Reference reference = new Reference { Keys = new List<Key> { new Key("GlobalReference", $"http://example.com/idta/nameplate/{strNodesetId}") } };

                                SubmodelDescriptor smd = new() {
                                    Endpoints = listSmdep,
                                    IdShort = nsvbsubmodel.Id,
                                    Id = $"https://example.com/ids/sm/{strNodesetId}",
                                    SemanticId = reference
                                };

                                aasd.SubmodelDescriptors.Add(smd);
                            }
                        }
                    }

                    output.Add(aasd);
                }
            }

            return output;
        }

        public List<AssetAdministrationShellDescriptor> GetAssetAdministrationShellDescriptorById(string userId, string idShort)
        {
            List<AssetAdministrationShellDescriptor> aasdReturn = new();

            List<NodesetViewerNode> listAAS = GetNodesetOfTypeById(userId, "Asset Admin Shells", idShort);
            if (listAAS != null)
            {
                foreach (var nodeset in listAAS)
                {
                    AssetAdministrationShellDescriptor aasdescrip = new();

                    string strUri = nodeset.Id;

                    List<Endpoint> listEPs = new();
                    Endpoint ep = new Endpoint() {
                        Interface = "AAS-1.0",
                        ProtocolInformation = new ProtocolInformation() { Href = $"https://v3.2.example.com/shells/{strUri}" },

                    };
                    listEPs.Add(ep);

                    List<SpecificAssetId> listSai = new();
                    SpecificAssetId sai = new SpecificAssetId() {
                        Name = "assetKind",
                        Value = "Instance",
                        ExternalSubjectId = new Reference() { Type = KeyElements.GlobalReference },
                    };

                    aasdescrip.AssetKind = AssetKind.Instance;
                    aasdescrip.AssetType = "Not Applicable";
                    aasdescrip.Endpoints = listEPs;
                    aasdescrip.GlobalAssetId = $"https://example.com/ids/{strUri}";
                    aasdescrip.IdShort = nodeset.Id;
                    aasdescrip.Id = nodeset.Text + ";" + nodeset.Value;

                    aasdescrip.SubmodelDescriptors = new List<SubmodelDescriptor>();
                    List<NodesetViewerNode> listSubmodels = GetNodesetOfTypeById(userId, "Submodels", idShort);
                    if (listSubmodels != null)
                    {
                        foreach (NodesetViewerNode nsvnSubmodels in listSubmodels)
                        {
                            List<Endpoint> listSmdep = new();
                            Endpoint smdep = new Endpoint() {
                                Interface = "SUBMODEL-1.0",
                                ProtocolInformation = new ProtocolInformation() { Href = $"https://v3.2.example.com/shells/{strUri}" },

                            };
                            listSmdep.Add(smdep);
                            Reference reference = new Reference { Keys = new List<Key> { new Key("GlobalReference", $"http://example.com/idta/nameplate/{strUri}") } };

                            SubmodelDescriptor smd = new() {
                                Endpoints = listSmdep,
                                IdShort = nsvnSubmodels.Id,
                                Id = $"https://example.com/ids/sm/{strUri}",
                                SemanticId = reference
                            };

                            aasdescrip.SubmodelDescriptors.Add(smd);
                        }
                    }

                    aasdReturn.Add(aasdescrip);
                }
            }

            return aasdReturn;
        }

        public List<AssetAdministrationShell> GetAllAssetAdministrationShells(string userId, List<string> assetIds = null, string idShort = null)
        {
            List<AssetAdministrationShell> output = new();

            // Query database for all Asset Administration Shells
            List<NodesetViewerNode> aasList = GetAllNodesetsOfType(userId, "Asset Admin Shells");

            if (aasList != null)
            {
                // Loop through all the Asset Administration Shells we found above.
                foreach (NodesetViewerNode a in aasList)
                {
                    string strCloudLibId = a.Id;
                    AssetAdministrationShell aasReturn = new() {
                        ModelType = ModelTypes.AssetAdministrationShell,
                        Identification = new Identifier() { Id = a.Id, Value = a.Value },
                        IdShort = a.Id,
                        Id = a.Text + ";" + a.Value
                    };

                    // get all asset and submodel refs
                    List<NodesetViewerNode> assetsAndSubmodelRefs = new(); // TODO
                    if (assetsAndSubmodelRefs != null)
                    {
                        foreach (NodesetViewerNode s in assetsAndSubmodelRefs)
                        {
                            if (s.Text.ToLower(CultureInfo.InvariantCulture).Contains("https://admin-shell.io/idta/asset/", StringComparison.OrdinalIgnoreCase))
                            {
                                aasReturn.AssetInformation = new AssetInformation() { AssetKind = AssetKind.Instance, SpecificAssetIds = new List<IdentifierKeyValuePair>() { new IdentifierKeyValuePair() { Key = s.Text } } };
                            }

                            if (s.Text.ToLower(CultureInfo.InvariantCulture).Contains("https://admin-shell.io/idta/submodel", StringComparison.OrdinalIgnoreCase))
                            {
                                aasReturn.Submodels.Add(new ModelReference() { Keys = new List<Key>() { new Key() { Value = s.Text, Type = KeyElements.Submodel } } });
                            }
                        }
                    }

                    output.Add(aasReturn);
                }
            }

            if (output.Count > 0)
            {
                // Filter AASs based on IdShort
                if (!string.IsNullOrEmpty(idShort))
                {
                    output = output.Where(a => a.IdShort.Equals(idShort, StringComparison.OrdinalIgnoreCase)).ToList();
                    if ((output == null) || output?.Count == 0)
                    {
                        throw new ArgumentException($"AssetAdministrationShells with IdShort {idShort} Not Found.");
                    }
                }

                // Filter based on AssetId
                if (assetIds != null && assetIds.Count != 0)
                {
                    List<AssetAdministrationShell> filteredAASes = new();
                    foreach (var assetId in assetIds)
                    {
                        filteredAASes.AddRange(output.Where(a => a.AssetInformation.SpecificAssetIds.Contains(new IdentifierKeyValuePair() { Key = assetId })).ToList());
                    }

                    if (filteredAASes.Count > 0)
                    {
                        return filteredAASes;
                    }
                    else
                    {
                        throw new ArgumentException($"AssetAdministrationShells with requested SpecificAssetIds Not Found.");
                    }
                }
            }

            return output;
        }

        public async Task<AssetAdministrationShell> GetAssetAdministrationShellById(string userId, string idShort)
        {
            List<NodesetViewerNode> nodeList = await _client.GetChildren(userId, idShort, ObjectIds.ObjectsFolder.ToString()).ConfigureAwait(false);
            if (nodeList != null)
            {
                foreach (NodesetViewerNode node in nodeList)
                {
                    if (node.Text == "Asset Admin Shells")
                    {
                        List<NodesetViewerNode> aasList = await _client.GetChildren(userId, idShort, node.Id).ConfigureAwait(false);
                        if (aasList != null)
                        {
                            if (aasList.Count > 1)
                            {
                                throw new NotImplementedException($"Currently only a single AAS per OPC UA nodeset file is supported.");
                            }

                            foreach (NodesetViewerNode a in aasList)
                            {
                                AssetAdministrationShell aas = new() {
                                    ModelType = ModelTypes.AssetAdministrationShell,
                                    Identification = new Identifier() { Id = a.Id, Value = a.Text },
                                    IdShort = a.Id + ";" + a.Text,
                                    Id = a.Id
                                };

                                // get all asset and submodel refs
                                List<NodesetViewerNode> assetsAndSubmodelRefs = await _client.GetChildren(userId, idShort, a.Id).ConfigureAwait(false);
                                if (assetsAndSubmodelRefs != null)
                                {
                                    foreach (NodesetViewerNode s in assetsAndSubmodelRefs)
                                    {
                                        if (s.Text.ToLower(CultureInfo.InvariantCulture).Contains("https://admin-shell.io/idta/asset/", StringComparison.OrdinalIgnoreCase))
                                        {
                                            aas.AssetInformation = new AssetInformation() { AssetKind = AssetKind.Instance, SpecificAssetIds = new List<IdentifierKeyValuePair>() { new IdentifierKeyValuePair() { Key = s.Text } } };
                                        }

                                        if (s.Text.ToLower(CultureInfo.InvariantCulture).Contains("https://admin-shell.io/idta/submodel", StringComparison.OrdinalIgnoreCase))
                                        {
                                            aas.Submodels.Add(new ModelReference() { Keys = new List<Key>() { new Key() { Value = s.Text, Type = KeyElements.Submodel } } });
                                        }
                                    }
                                }

                                return aas;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public List<Submodel> GetAllSubmodels(string userId, Reference reqSemanticId = null)
        {
            List<Submodel> output = new();

            // Get All Submodels
            List<NodesetViewerNode> listSubmodel = GetAllNodesetsOfType(userId, "Submodels");
            if (listSubmodel != null)
            {
                foreach (NodesetViewerNode nsvnSubmodel in listSubmodel)
                {
                    Submodel subReturn = new() {
                        ModelType = ModelTypes.Submodel,
                        Id = $"{nsvnSubmodel.Text};{nsvnSubmodel.Value}",
                        Identification = new Identifier() { Id = nsvnSubmodel.Id, Value = nsvnSubmodel.Text },
                        IdShort = nsvnSubmodel.Id,
                        SemanticId = new Reference() { Type = KeyElements.ExternalReference, Keys = new List<Key>() { new Key() { Value = nsvnSubmodel.Text, Type = KeyElements.GlobalReference } } },
                        DisplayName = new List<LangString>() { new LangString() { Text = nsvnSubmodel.DisplayName } },
                        Description = new List<LangString>() { new LangString() { Text = nsvnSubmodel.Description } }
                    };

                    output.Add(subReturn);
                }
            }

            // Apply filters
            if (output.Count > 0)
            {

                // Filter based on SemanticId
                if ((reqSemanticId != null) && (reqSemanticId.Keys[0].Value != null))
                {
                    if (output.Count > 0)
                    {
                        var submodels = output.Where(s => s.SemanticId.Matches(reqSemanticId)).ToList();
                        if ((submodels == null) || submodels?.Count == 0)
                        {
                            Console.WriteLine($"Submodels with requested SemnaticId Not Found.");
                        }

                        output = submodels;
                    }
                }
            }

            return output;
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

        public async Task<List<ConceptDescription>> GetAllConceptDescriptions(string userId, string idShort = null, string reqIsCaseOf = null, string reqDataSpecificationRef = null)
        {
            List<ConceptDescription> output = new();

            // Query database for all Concept Descriptions
            List<NodesetViewerNode> listConceptDescriptions = GetAllNodesetsOfType(userId, "Concept Descriptions");

            if (listConceptDescriptions != null)
            {
                foreach (NodesetViewerNode cdNode in listConceptDescriptions)
                {
                    if (cdNode != null)
                    {
                        List<NodesetViewerNode> cdrItems = await _client.GetChildren(userId, cdNode.Id, cdNode.Value).ConfigureAwait(false);

                        if (cdrItems != null)
                        {
                            foreach (NodesetViewerNode item in cdrItems)
                            {
                                ConceptDescription cd = new() {
                                    ModelType = ModelTypes.ConceptDescription,
                                    IdShort = item.Text,
                                    Id = item.Id,
                                };

                                output.Add(cd);
                            }
                        }
                    }
                }
            }

            if (output.Count > 0)
            {
                // Filter AASs based on IdShort
                if (!string.IsNullOrEmpty(idShort))
                {
                    output = output.Where(a => a.IdShort.Equals(idShort, StringComparison.OrdinalIgnoreCase)).ToList();
                    if ((output == null) || output?.Count == 0)
                    {
                        throw new ArgumentException($"Concept Description with IdShort {idShort} Not Found.");
                    }
                }
            }

            return output;
        }

        public async Task<List<ConceptDescription>> GetConceptDescriptionById(string userId, string idShort)
        {
            List<ConceptDescription> output = new();

            // Query database for Concept Descriptions
            List<NodesetViewerNode> listConceptDescriptions = GetNodesetOfTypeById(userId, "Concept Descriptions", idShort);
            if (listConceptDescriptions != null)
            {
                foreach (NodesetViewerNode cdNode in listConceptDescriptions)
                {
                    if (cdNode != null)
                    {
                        if (idShort == cdNode.Id)
                        {
                            List<NodesetViewerNode> cdrItems = await _client.GetChildren(userId, idShort, cdNode.Value).ConfigureAwait(false);

                            if (cdrItems != null)
                            {
                                foreach (NodesetViewerNode item in cdrItems)
                                {
                                    ConceptDescription cd = new() {
                                        ModelType = ModelTypes.ConceptDescription,
                                        IdShort = item.Text,
                                        Id = item.Id,
                                    };

                                    output.Add(cd);
                                }
                            }
                        }
                    }
                }
            }


            return output;
        }

        public async Task<AssetInformation> GetAssetInformationFromAas(string userId, string idShort)
        {
            var aas = await GetAssetAdministrationShellById(userId, idShort).ConfigureAwait(false);
            if (aas != null)
            {
                return aas.AssetInformation;
            }
            else
            {
                return null;
            }
        }

        public async Task<byte[]> GetFileByPath(string userId, string idShort, string idShortPath)
        {
            byte[] byteArray = null;
            string fileName = null;

            SubmodelElement sme = await GetSubmodelElementByPath(userId, idShort, idShortPath).ConfigureAwait(false);
            if (sme != null)
            {
                if (sme is File file)
                {
                    fileName = file.Value;
                    byteArray = System.IO.File.ReadAllBytes(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", fileName));
                }
                else
                {
                    throw new ArgumentException($"Submodel element {sme.IdShort} is not of Type File.");
                }
            }
            else
            {
                throw new ArgumentException($"Submodel element {idShortPath} not found.");
            }

            return byteArray;
        }

        public async Task<List<Reference>> GetAllSubmodelReferences(string userId, string idShort)
        {
            var aas = await GetAssetAdministrationShellById(userId, idShort).ConfigureAwait(false);

            if (aas != null)
            {
                List<Reference> references = [.. aas.Submodels];

                return references;
            }
            else
            {
                return null;
            }
        }

        public async Task<List<SubmodelElement>> GetAllSubmodelElementsFromSubmodel(string userId, string idShort)
        {
            var submodel = await GetSubmodelById(userId, idShort).ConfigureAwait(false);
            if (submodel == null)
            {
                return null;
            }
            else
            {
                return submodel.SubmodelElements;
            }
        }

        public async Task<SubmodelElement> GetSubmodelElementByPath(string userId, string idShort, string idShortPath)
        {
            Submodel submodel = await GetSubmodelById(userId, idShort).ConfigureAwait(false);
            if (submodel != null)
            {
                return FindSubmodelElementByIdShortPath(submodel, idShortPath);
            }
            else
            {
                return null;
            }
        }

        private SubmodelElement FindSubmodelElementByIdShortPath(object element, string idShortPath)
        {
            string[] path = idShortPath.Split(".");
            if (path.Length == 0)
            {
                return null;
            }

            // we're at the end of the path
            if (path.Length == 1)
            {
                if (element is Submodel submodel)
                {
                    foreach (SubmodelElement sme in submodel.SubmodelElements)
                    {
                        if (sme.IdShort == path[0])
                        {
                            return sme;
                        }
                    }

                    return null;
                }
                else if (element is SubmodelElementList list)
                {
                    foreach (SubmodelElement sme in list.Value)
                    {
                        if (sme.IdShort == path[0])
                        {
                            return sme;
                        }
                    }

                    return null;
                }
                else if (element is Entity entity)
                {
                    if (entity.IdShort == path[0])
                    {
                        return entity;
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (element is DataElement dataElement)
                {
                    if (dataElement.IdShort == path[0])
                    {
                        return dataElement;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                string currentPathElement = path[0];
                string restOfPath = string.Join(".", path.Skip(1).ToArray());
                if (restOfPath != null)
                {
                    if (element is Submodel submodel)
                    {
                        foreach (SubmodelElement sme in submodel.SubmodelElements)
                        {
                            if (sme.IdShort == path[0])
                            {
                                return FindSubmodelElementByIdShortPath(sme, restOfPath);
                            }
                        }

                        return null;
                    }
                    else if (element is SubmodelElementList list)
                    {
                        foreach (SubmodelElement sme in list.Value)
                        {
                            if (sme.IdShort == path[0])
                            {
                                return FindSubmodelElementByIdShortPath(sme, restOfPath);
                            }
                        }

                        return null;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
