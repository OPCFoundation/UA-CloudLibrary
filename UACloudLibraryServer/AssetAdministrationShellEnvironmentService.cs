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
using Opc.Ua.Cloud.Library.Controllers;

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

        public List<AssetAdministrationShell> GetAllAssetAdministrationShells(string userId, List<string> assetIds = null, string idShort = null)
        {
            List<AssetAdministrationShell> output = new();

            // Query database for all Asset Administration Shells
            List<NodesetViewerNode> aasList = _cldata.GetAllNodesOfType(userId, "Asset Admin Shells", true);

            if (aasList != null)
            {
                // Loop through all the Asset Administration Shells we found above.
                foreach (NodesetViewerNode node in aasList)
                {
                    AssetAdministrationShell aas = FactoryMakeAssetAdminShell(userId, node);
                    output.Add(aas);
                }
            }

            return (output);
        }

#pragma warning disable 1998
        public async Task<AssetAdministrationShell> GetAssetAdministrationShellById(string nodesetIdentifier, string userId)
        {
            if (string.IsNullOrWhiteSpace(nodesetIdentifier))
            {
                return null;
            }

            string idDatabase = NameMgr.GetDatabaseIDFromUrl(nodesetIdentifier);
            if (string.IsNullOrEmpty(idDatabase))
                return null;

            // Query database for all Asset Administration Shells
            List<NodesetViewerNode> aasList = _cldata.GetAllNodesOfType(userId, "Asset Admin Shells", true);

            if (aasList == null || aasList.Count == 0)
            {
                return null;
            }

            var node = aasList.FirstOrDefault(n => n.Id == idDatabase);
            if (node == null)
            {
                return null;
            }

            var aas = FactoryMakeAssetAdminShell(userId, node);
            return aas;
        }

        private AssetAdministrationShell FactoryMakeAssetAdminShell(string userId, NodesetViewerNode node)
        {
            string strUri = node.Id;
            AssetAdministrationShell aas = new() {
                ModelType = ModelTypes.AssetAdministrationShell,
                IdShort = node.Id,
                Id = node.Text,
                AssetInformation = new() {  AssetKind = AssetKind.Instance, GlobalAssetId = "GlobalAssetId" }
            };

            // Find the Submodels for the current Asset Administration Shell
            List<NodesetViewerNode> submodels = _cldata.GetAllSubModelNodesForAssetAdminShell(userId, aas);
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
            return aas;
        }

        public async Task<List<Submodel>> GetAllSubmodels(string userId, Reference reqSemanticId = null, string idShort = null)
        {
            List<Submodel> output = new();

            // Get All Submodels
            // List<NodesetViewerNode> submodelList = _cldata.GetAllSubModelNodesForAssetAdminShell(userId, "Submodels", "Data");
            List<NodesetViewerNode> nodesetList = _cldata.GetAllNodesOfType(userId, "Submodels");
            if (nodesetList != null)
            {
                foreach (NodesetViewerNode nodeset in nodesetList)
                {
                    // The following line gives me all nodes of type i-85, namely the "Folders". It is not what I want.
                    string str1 = ObjectIds.ObjectsFolder.ToString();
                    List<NodesetViewerNode> nodeFolderList = await _client.GetChildren(nodeset.Id, ObjectIds.ObjectsFolder.ToString(), userId).ConfigureAwait(false);

                    if (nodeFolderList != null )
                    {
                        foreach (NodesetViewerNode nodeFolder in nodeFolderList)
                        {
                            if (nodeFolder.Text == "Submodels")
                            {
                                List<NodesetViewerNode> nodeSubmodels = await _client.GetChildren(nodeset.Id, nodeFolder.Id, userId).ConfigureAwait(false);
                                if (nodeSubmodels != null)
                                {
                                    foreach (NodesetViewerNode node in nodeSubmodels)
                                    {
                                        List<string>listSubitems = new List<string>();
                                        listSubitems.Add(node.Text);
                                        Submodel sub = new() {
                                            ModelType = ModelTypes.Submodel,
                                            Id = NameMgr.MakeNiceUrl("Submodels", node.Id, nodeset.Id, listSubitems),
                                            IdShort = nodeset.Id + ";" + node.Text,
                                            SemanticId = new Reference() { Type = KeyElements.ExternalReference, Keys = new List<Key>() { new Key() { Value = nodeset.Text, Type = KeyElements.GlobalReference } } },
                                            DisplayName = new List<LangString>() { new LangString() { Text = nodeset.Text } },
                                            Description = new List<LangString>() { new LangString() { Text = string.Empty /* TODO */ } }
                                        };

                                        output.Add(sub);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return output;
        }




        private async Task<List<SubmodelElement>> ReadSubmodelElementNodes(string userId, string nodesetIdentifier, NodesetViewerNode subNode, bool browseDeep)
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
                        List<SubmodelElement> children = await ReadSubmodelElementNodes(userId, nodesetIdentifier, smeNode, browseDeep).ConfigureAwait(false);
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
                                Value = await _client.VariableRead( userId, nodesetIdentifier, smeNode.Id).ConfigureAwait(false)
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


        public async Task<Submodel> GetSubmodelById(string nodesetIdentifier, string userId)
        {
            // List<NodesetViewerNode> nodeList = await _client.GetChildren(nodesetIdentifier, ObjectIds.ObjectsFolder.ToString(), userId).ConfigureAwait(false);
            
            List<NodesetViewerNode> nodeList = await _client.GetChildren("3604358527", ObjectIds.ObjectsFolder.ToString(), userId).ConfigureAwait(false);
            if (nodeList != null)
            {
                foreach (NodesetViewerNode node in nodeList)
                {
                    if (node.Text == "Submodels")
                    {
                        List<NodesetViewerNode> submodelList = await _client.GetChildren(nodesetIdentifier, node.Id, userId).ConfigureAwait(false);
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
                                    IdShort = subNode.Id + ";" + subNode.Text,
                                    SemanticId = new Reference() { Type = KeyElements.ExternalReference, Keys = new List<Key>() { new Key() { Value = subNode.Text, Type = KeyElements.GlobalReference } } },
                                    DisplayName = new List<LangString>() { new LangString() { Text = subNode.Text } },
                                    Description = new List<LangString>() { new LangString() { Text = await _client.VariableRead(userId, nodesetIdentifier, subNode.Id).ConfigureAwait(false) } }
                                };

                                // get all submodel elements
                                sub.SubmodelElements.AddRange(await ReadSubmodelElementNodes(userId, nodesetIdentifier, subNode, true).ConfigureAwait(false));

                                return sub;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public List<ConceptDescription> GetAllConceptDescriptions(string userId, string idShort = null, string reqIsCaseOf = null, string reqDataSpecificationRef = null)
        {
            List<ConceptDescription> output = new();

            // Query database for all Concept Descriptions
            List<NodesetViewerNode> conceptDescrNodes = _cldata.GetAllNodesOfType(userId, "Concept Descriptions");

            if (conceptDescrNodes != null)
            {
                foreach (NodesetViewerNode cdNode in conceptDescrNodes)
                {
                    ConceptDescription cd = new() {
                        ModelType = ModelTypes.ConceptDescription,
                        IdShort = cdNode.Id,
                        Id = cdNode.Id
                    };

                    output.Add(cd);
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

        public async Task<ConceptDescription> GetConceptDescriptionById(string userId, string nodesetIdentifier)
        {
            List<NodesetViewerNode> nodeList = await _client.GetChildren(nodesetIdentifier, ObjectIds.ObjectsFolder.ToString(), userId).ConfigureAwait(false);
            if (nodeList != null)
            {
                foreach (NodesetViewerNode node in nodeList)
                {
                    if (node.Text == "Concept Descriptions")
                    {
                        List<NodesetViewerNode> conceptDescrNodes = await _client.GetChildren(nodesetIdentifier, node.Id, userId).ConfigureAwait(false);
                        if (conceptDescrNodes != null)
                        {
                            if (conceptDescrNodes.Count > 1)
                            {
                                throw new NotImplementedException($"Currently only a single Concept Description per OPC UA nodeset file is supported.");
                            }

                            foreach (NodesetViewerNode cdNode in conceptDescrNodes)
                            {
                                if (cdNode.Id.Equals(nodesetIdentifier, StringComparison.OrdinalIgnoreCase))
                                {
                                    ConceptDescription cd = new() {
                                        ModelType = ModelTypes.ConceptDescription,
                                        IdShort = cdNode.Text,
                                        Id = cdNode.Id + ";" + cdNode.Text,
                                    };

                                    return cd;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        public async Task<AssetInformation> GetAssetInformationFromAas(string nodesetIdentifier, string userId)
        {
            var aas = await GetAssetAdministrationShellById(nodesetIdentifier, userId).ConfigureAwait(false);
            if (aas != null)
            {
                return aas.AssetInformation;
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

            return byteArray;
        }

        public async Task<List<Reference>> GetAllSubmodelReferences(string userId, string nodesetIdentifier)
        {
            var aas = await GetAssetAdministrationShellById(nodesetIdentifier, userId).ConfigureAwait(false);

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

        public async Task<List<SubmodelElement>> GetAllSubmodelElementsFromSubmodel(string nodesetIdentifier, string userId)
        {
            var submodel = await GetSubmodelById(nodesetIdentifier, userId).ConfigureAwait(false);
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
            Submodel submodel = await GetSubmodelById(nodesetIdentifier, userId).ConfigureAwait(false);
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

        //// Helpers to make for more readable paths, etc.
        //private static readonly Dictionary<string, string> TypeToPath = new Dictionary<string, string>
        //{
        //    {"Asset Admin Shells","idta/asset" },
        //    {"Submodels","idta/submodels" },
        //    {"Another","Thing" },
        //};


        //private static readonly Dictionary<string, string> ShortNames = new Dictionary<string, string>
        //{
        //    {"urn_samm_io_catenax_battery_battery_pass_6_0_0_BatteryPass/","BatteryPassport" },
        //    {"http://catena-x.org/UA/urn_samm_io_catenax_battery_battery_pass_6_0_0_BatteryPass/","BatteryPassport" },
        //    {"urn_samm_io_catenax_generic_digital_product_passport_5_0_0_DigitalProductPassport","DigitalProductPassport" },
        //    {"http://catena-x.org/UA/urn_samm_io_catenax_generic_digital_product_passport_5_0_0_DigitalProductPassport/","DigitalProductPassport" },
        //    {"urn_samm_io_catenax_pcf_7_0_0_Pcf/","ProductCarbonFootprint" },
        //    {"http://catena-x.org/UA/urn_samm_io_catenax_pcf_7_0_0_Pcf/","ProductCarbonFootprint" },
        //    {"urn_samm_io_catenax_single_level_bom_as_built_3_0_0_SingleLevelBomAsBuilt/","SingleLevelBomAsBuilt" },
        //    {"http://catena-x.org/UA/urn_samm_io_catenax_single_level_bom_as_built_3_0_0_SingleLevelBomAsBuilt/","SingleLevelBomAsBuilt" },
        //};

        //public static string GetFriendlyName(string strName, Dictionary<string, string> d)
        //{
        //    if (d.TryGetValue(strName, out string strShortName))
        //    {
        //        strName = strName.Replace(strName, strShortName, StringComparison.CurrentCulture);
        //    }
        //    return strName;
        //}

        //private static List<AssetAdministrationShell> AssetAdministrationShellFriendlyNames(List<AssetAdministrationShell> input)
        //{
        //    foreach (AssetAdministrationShell aashell in input)
        //    {
        //        //string strAAPathPart = GetFriendlyName("Asset Admin Shells", TypeToPath);
        //        //string strNodeName = GetFriendlyName(aashell.Id, ShortNames);
        //        //aashell.Id = $"http://example.com/{strAAPathPart}/{strNodeName}/";
        //        //string strValue = GetValueFromId(aashell.IdShort);
        //        //aashell.IdShort = $"{aashell.Id};{strValue}";

        //        (aashell.Id, aashell.IdShort) = UriMapper.AssetAdminShell(aashell.Id, "id");
                

        //        ModelFriendlyNames(aashell.Submodels);
        //    }

        //    return input;
        //}

        //private static void ModelFriendlyNames(List<ModelReference> listModels)
        //{
        //    string strModelPathPart = GetFriendlyName("Submodels", TypeToPath);

        //    foreach (ModelReference mref in listModels)
        //    {
        //        foreach (Key k in mref.Keys)
        //        {
        //            //string strModelNode = GetFriendlyName(k.Value, ShortNames);
        //            //k.Value = $"http://example.com/{strModelPathPart}/{strModelNode}/";
        //            (k.Value, string idShort) = UriMapper.SubModel("Submodel", "submodel");
        //        }
        //    }
        //}

        //private static void SubmodelFriendlyNames(List<Submodel> submodelList)
        //{
        //    string strModelPathPart = GetFriendlyName("Submodels", TypeToPath);

        //    foreach (Submodel subNode in submodelList)
        //    {
        //        string strModelNode = GetFriendlyName(subNode.Id, ShortNames);
        //        subNode.Id = $"http://example.com/{strModelPathPart}/{strModelNode}/";
        //    }
        //}



        //private static string GetValueFromId(string strInput)
        //{
        //    int iSemicolon = strInput.IndexOf(';', StringComparison.CurrentCulture);
        //    if (iSemicolon > 0)
        //        strInput = strInput.Substring(iSemicolon + 1);

        //    return strInput;
        //}

    } // class
} // namespace
