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


        public async Task<List<AssetAdministrationShellDescriptor>> GetAllAssetAdministrationShellDescriptors(string userId)
        {
            List<AssetAdministrationShellDescriptor> output = new();

            // Query database for all Asset Administration Shells
            List<NodesetViewerNode> aasList = await _client.GetAllNodesOfTypeAsync(userId, "Asset Admin Shells", bChildren: true).ConfigureAwait(false);

            if (aasList != null)
            {
                // Loop through all the Asset Administration Shells we found above.
                foreach (NodesetViewerNode a in aasList)
                {
                    string strUri = a.Id;

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

                    AssetAdministrationShellDescriptor aasd = new() {
                        AssetKind = AssetKind.Instance,
                        AssetType = "Not Applicable",
                        Endpoints = listEPs,
                        GlobalAssetId = $"https://example.com/ids/{strUri}",
                        IdShort = a.Id,
                        Id = a.Text + ";" + a.Value
                    };

                    aasd.SubmodelDescriptors = new List<SubmodelDescriptor>();
                    List<NodesetViewerNode> submodels = a.Children?.Where(child => child.Text == "Submodels").ToList();
                    if (submodels != null)
                    {
                        foreach (NodesetViewerNode s in submodels)
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
                                IdShort = s.Id,
                                Id = $"https://example.com/ids/sm/{strUri}",
                                SemanticId = reference
                            };

                            aasd.SubmodelDescriptors.Add(smd);
                        }
                    }

                    output.Add(aasd);
                }
            }

            return output;
        }

        public async Task<AssetAdministrationShellDescriptor> GetAssetAdministrationShellDescriptorById(string userId, string idShort)
        {
            AssetAdministrationShellDescriptor aasd = new();

            // Query database for all Asset Administration Shells
            List<NodesetViewerNode> aasList = await _client.GetAllNodesOfTypeAsync(userId, "Asset Admin Shells", strCloudLibId: idShort, bChildren: true).ConfigureAwait(false);

            if (aasList != null)
            {
                // Loop through all the Asset Administration Shells we found above.
                foreach (NodesetViewerNode a in aasList)
                {
                    string strUri = a.Id;

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

                    aasd.AssetKind = AssetKind.Instance;
                    aasd.AssetType = "Not Applicable";
                    aasd.Endpoints = listEPs;
                    aasd.GlobalAssetId = $"https://example.com/ids/{strUri}";
                    aasd.IdShort = a.Id;
                    aasd.Id = a.Text + ";" + a.Value;

                    aasd.SubmodelDescriptors = new List<SubmodelDescriptor>();
                    List<NodesetViewerNode> submodels = a.Children?.Where(child => child.Text == "Submodels").ToList();
                    if (submodels != null)
                    {
                        foreach (NodesetViewerNode s in submodels)
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
                                IdShort = s.Id,
                                Id = $"https://example.com/ids/sm/{strUri}",
                                SemanticId = reference
                            };

                            aasd.SubmodelDescriptors.Add(smd);
                        }
                    }
                }
            }

            return aasd;
        }


        public async Task<List<AssetAdministrationShell>> GetAllAssetAdministrationShells(string userId, List<string> assetIds = null, string idShort = null)
        {
            List<AssetAdministrationShell> output = new();

            // Query database for all Asset Administration Shells
            List<NodesetViewerNode> aasList = await _client.GetAllNodesOfTypeAsync(userId, "Asset Admin Shells", strCloudLibId: idShort, bChildren: true).ConfigureAwait(false);

            if (aasList != null)
            {
                // Loop through all the Asset Administration Shells we found above.
                foreach (NodesetViewerNode a in aasList)
                {
                    string strUri = a.Id;
                    AssetAdministrationShell aas = new() {
                        ModelType = ModelTypes.AssetAdministrationShell,
                        Identification = new Identifier() { Id = a.Id, Value = a.Value },
                        IdShort = a.Id,
                        Id = a.Text + ";" + a.Value
                    };

                    List<NodesetViewerNode> submodels = a.Children?.Where(child => child.Text == "Submodels").ToList();
                    if (submodels != null)
                    {
                        foreach (NodesetViewerNode s in submodels)
                        {
                            ModelReference mr = new ModelReference() {
                                Keys = new List<Key>()
                            {
                                new Key()
                                {
                                    Value = s.Id,
                                    Type = KeyElements.Submodel
                                }
                            }
                            };
                            aas.Submodels.Add(mr);
                        }
                    }

                    output.Add(aas);
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

        public async Task<List<Submodel>> GetAllSubmodels(string userId, string strCloudLibIdentifier, Reference reqSemanticId = null)
        {
            List<Submodel> output = new();

            // Get All Submodels
            List<NodesetViewerNode> submodelList = await _client.GetAllNodesOfTypeAsync(userId, "Submodels", bChildren: false).ConfigureAwait(false);
            if (submodelList != null)
            {
                foreach (NodesetViewerNode subNode in submodelList)
                {
                    Submodel sub = new() {
                        ModelType = ModelTypes.Submodel,
                        Id = $"{subNode.Text};{subNode.Value}",
                        Identification = new Identifier() { Id = subNode.Id, Value = subNode.Text },
                        IdShort = subNode.Id,
                        SemanticId = new Reference() { Type = KeyElements.ExternalReference, Keys = new List<Key>() { new Key() { Value = subNode.Text, Type = KeyElements.GlobalReference } } },
                        DisplayName = new List<LangString>() { new LangString() { Text = subNode.Text } },
                        Description = new List<LangString>() { new LangString() { Text = string.Empty /* TODO */ } }
                    };

                    output.Add(sub);
                }
            }

            // Apply filters
            if (output.Count > 0)
            {
                // Filter based on idShort
                if (!string.IsNullOrEmpty(strCloudLibIdentifier))
                {
                    var submodels = output.Where(s => s.IdShort.Equals(strCloudLibIdentifier, StringComparison.OrdinalIgnoreCase)).ToList();
                    if ((submodels == null) || (submodels?.Count == 0))
                    {
                        Console.WriteLine($"Submodels with IdShort {strCloudLibIdentifier} Not Found.");
                    }

                    output = submodels;
                }

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

        private async Task<List<SubmodelElement>> ReadSubmodelElementNodes(string nodesetIdentifier, NodesetViewerNode subNode, bool browseDeep, string userId)
        {
            List<SubmodelElement> output = new();

            List<NodesetViewerNode> submodelElementNodes = await _client.GetChildren(userId, nodesetIdentifier, subNode.Id).ConfigureAwait(false);
            if (submodelElementNodes != null)
            {
                foreach (NodesetViewerNode smeNode in submodelElementNodes)
                {
                    if (browseDeep)
                    {
                        // check for children - if there are, create a smel instead of an sme
                        List<SubmodelElement> children = await ReadSubmodelElementNodes(nodesetIdentifier, smeNode, browseDeep, userId).ConfigureAwait(false);
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
                                Value = await _client.VariableRead(userId, nodesetIdentifier, smeNode.Id).ConfigureAwait(false)
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
            List<NodesetViewerNode> conceptDescrNodes = await _client.GetAllNodesOfTypeAsync(userId, "Concept Descriptions", bChildren: true, strCloudLibId: idShort).ConfigureAwait(false);

            if (conceptDescrNodes != null)
            {
                foreach (NodesetViewerNode cdNode in conceptDescrNodes)
                {
                    if (idShort == null || idShort == cdNode.Id)
                    {
                        List<NodesetViewerNode> cdr = cdNode.Children?.Where(child => child.Text == "Concept Descriptions").ToList();

                        if (cdr != null && !string.IsNullOrEmpty(cdr[0].Id))
                        {
                            List<NodesetViewerNode> cdrItems = await _client.GetChildren(userId, cdNode.Id, cdr[0].Id).ConfigureAwait(false);
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
        public async Task<List<ConceptDescription>> GetConceptDescriptionById(string userId, string nodesetIdentifier)
        {
            List<ConceptDescription> output = new();

            // Query database for all Concept Descriptions
            List<NodesetViewerNode> conceptDescrNodes = await _client.GetAllNodesOfTypeAsync(userId, "Concept Descriptions", bChildren: true, strCloudLibId: nodesetIdentifier).ConfigureAwait(false);

            if (conceptDescrNodes != null)
            {
                foreach (NodesetViewerNode cdNode in conceptDescrNodes)
                {
                    if (nodesetIdentifier == cdNode.Id)
                    {
                        List<NodesetViewerNode> cdr = cdNode.Children?.Where(child => child.Text == "Concept Descriptions").ToList();

                        if (cdr != null && !string.IsNullOrEmpty(cdr[0].Id))
                        {
                            List<NodesetViewerNode> cdrItems = await _client.GetChildren(userId, cdNode.Id, cdr[0].Id).ConfigureAwait(false);
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


        public async Task<Submodel> GetSubmodelById(string userId, string nodesetIdentifier)
        {
            List<NodesetViewerNode> nodeList = await _client.GetChildren(userId, nodesetIdentifier, ObjectIds.ObjectsFolder.ToString()).ConfigureAwait(false);
            if (nodeList != null)
            {
                foreach (NodesetViewerNode node in nodeList)
                {
                    if (node.Text == "Submodels")
                    {
                        List<NodesetViewerNode> submodelList = await _client.GetChildren(userId, nodesetIdentifier, node.Id).ConfigureAwait(false);
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
                                    Description = new List<LangString>() { new LangString() { Text = await _client.VariableRead(userId, nodesetIdentifier, subNode.Id).ConfigureAwait(false) } }
                                };

                                // get all submodel elements
                                sub.SubmodelElements.AddRange(await ReadSubmodelElementNodes(nodesetIdentifier, subNode, true, userId).ConfigureAwait(false));

                                return sub;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public async Task<AssetInformation> GetAssetInformationFromAas(string userId, string nodesetIdentifier)
        {
            List<AssetAdministrationShell> aasList = await GetAllAssetAdministrationShells(userId, null, nodesetIdentifier).ConfigureAwait(false);
            if (aasList != null && aasList.Count > 0)
            {
                return aasList.FirstOrDefault().AssetInformation;
            }
            else
            {
                return null;
            }
        }

        public async Task<byte[]> GetFileByPath(string nodesetIdentifier, string idShortPath, string userId)
        {
            byte[] byteArray = null;
            string fileName = null;

            SubmodelElement sme = await GetSubmodelElementByPath(nodesetIdentifier, idShortPath, userId).ConfigureAwait(false);
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

        public async Task<List<Reference>> GetAllSubmodelReferences(string userId, string nodesetIdentifier)
        {
            List<AssetAdministrationShell> aasList = await GetAllAssetAdministrationShells(userId, null, nodesetIdentifier).ConfigureAwait(false);

            if (aasList != null)
            {
                AssetAdministrationShell aas = aasList.FirstOrDefault();
                List<Reference> references = null;
                if (aas != null)
                {
                    references = [.. aas.Submodels];
                }

                return references;
            }
            else
            {
                return null;
            }
        }

        public async Task<List<SubmodelElement>> GetAllSubmodelElementsFromSubmodel(string nodesetIdentifier, string userId)
        {
            var submodel = await GetSubmodelById(userId, nodesetIdentifier).ConfigureAwait(false);
            if (submodel == null)
            {
                return null;
            }
            else
            {
                return submodel.SubmodelElements;
            }
        }

        public async Task<SubmodelElement> GetSubmodelElementByPath(string nodesetIdentifier, string idShortPath, string userId)
        {
            Submodel submodel = await GetSubmodelById(userId, nodesetIdentifier).ConfigureAwait(false);
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

        // Helpers to make for more readable paths, etc.
        private static readonly Dictionary<string, string> TypeToPath = new Dictionary<string, string>
        {
            {"Asset Admin Shells","idta/asset" },
            {"Submodels","idta/submodels" },
            {"Another","Thing" },
        };


        private static readonly Dictionary<string, string> ShortNames = new Dictionary<string, string>
        {
            {"urn_samm_io_catenax_battery_battery_pass_6_0_0_BatteryPass/","BatteryPassport" },
            {"http://catena-x.org/UA/urn_samm_io_catenax_battery_battery_pass_6_0_0_BatteryPass/","BatteryPassport" },
            {"urn_samm_io_catenax_generic_digital_product_passport_5_0_0_DigitalProductPassport","DigitalProductPassport" },
            {"http://catena-x.org/UA/urn_samm_io_catenax_generic_digital_product_passport_5_0_0_DigitalProductPassport/","DigitalProductPassport" },
            {"urn_samm_io_catenax_pcf_7_0_0_Pcf/","ProductCarbonFootprint" },
            {"http://catena-x.org/UA/urn_samm_io_catenax_pcf_7_0_0_Pcf/","ProductCarbonFootprint" },
            {"urn_samm_io_catenax_single_level_bom_as_built_3_0_0_SingleLevelBomAsBuilt/","SingleLevelBomAsBuilt" },
            {"http://catena-x.org/UA/urn_samm_io_catenax_single_level_bom_as_built_3_0_0_SingleLevelBomAsBuilt/","SingleLevelBomAsBuilt" },
        };

        public static string GetFriendlyName(string strName, Dictionary<string, string> d)
        {
            if (d.TryGetValue(strName, out string strShortName))
            {
                strName = strName.Replace(strName, strShortName, StringComparison.CurrentCulture);
            }
            return strName;
        }

        private static List<AssetAdministrationShell> ReplaceWithFriendlyNames(List<AssetAdministrationShell> input)
        {
            foreach (AssetAdministrationShell aashell in input)
            {
                string strAAPathPart = GetFriendlyName("Asset Admin Shells", TypeToPath);
                string strNodeName = GetFriendlyName(aashell.Id, ShortNames);
                aashell.Id = $"http://example.com/{strAAPathPart}/{strNodeName}/";
                string strValue = GetValueFromId(aashell.IdShort);
                aashell.IdShort = $"{aashell.Id};{strValue}";


                foreach (ModelReference mref in aashell.Submodels)
                {
                    string strModelPathPart = GetFriendlyName("Submodels", TypeToPath);
                    foreach (Key k in mref.Keys)
                    {
                        string strModelNode = GetFriendlyName(k.Value, ShortNames);
                        k.Value = $"http://example.com/{strModelPathPart}/{strModelNode}/";
                    }
                }
            }

            return input;
        }

        private static string GetValueFromId(string strInput)
        {
            int iSemicolon = strInput.IndexOf(';', StringComparison.CurrentCulture);
            if (iSemicolon > 0)
                strInput = strInput.Substring(iSemicolon + 1);

            return strInput;
        }

    }
}
