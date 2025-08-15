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

namespace AdminShell
{
    public class AssetAdministrationShellEnvironmentService
    {
        private readonly UAClient _client;

        public AssetAdministrationShellEnvironmentService(UAClient client)
        {
            _client = client;
        }

        public List<AssetAdministrationShell> GetAllAssetAdministrationShells(List<string> assetIds = null, string idShort = null)
        {
            List<AssetAdministrationShell> output = new();

            // get all AASes
            List<NodesetViewerNode> nodeList = new(); // TODO
            if (nodeList != null)
            {
                foreach (NodesetViewerNode node in nodeList)
                {
                    if (node.Text == "Asset Admin Shells")
                    {
                        List<NodesetViewerNode> aasList = new(); // TODO
                        if (aasList != null)
                        {
                            foreach (NodesetViewerNode a in aasList)
                            {
                                AssetAdministrationShell aas = new() {
                                    ModelType = ModelTypes.AssetAdministrationShell,
                                    Identification = new Identifier() { Id = a.Id, Value = a.Text },
                                    IdShort = a.Id + ";" + a.Text,
                                    Id = a.Id
                                };

                                // get all asset and submodel refs
                                List<NodesetViewerNode> assetsAndSubmodelRefs = new(); // TODO
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

                                output.Add(aas);
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

        public List<Submodel> GetAllSubmodels(Reference reqSemanticId = null, string idShort = null)
        {
            List<Submodel> output = new();

            // Get All Submodels
            List<NodesetViewerNode> nodeList = new(); // TODO
            if (nodeList != null)
            {
                foreach (NodesetViewerNode node in nodeList)
                {
                    if (node.Text == "Submodels")
                    {
                        List<NodesetViewerNode> submodelList = new(); // TODO
                        if (submodelList != null)
                        {
                            foreach (NodesetViewerNode subNode in submodelList)
                            {
                                Submodel sub = new() {
                                    ModelType = ModelTypes.Submodel,
                                    Id = subNode.Id,
                                    Identification = new Identifier() { Id = subNode.Id, Value = subNode.Text },
                                    IdShort = subNode.Id + ";" + subNode.Text,
                                    SemanticId = new Reference() { Type = KeyElements.ExternalReference, Keys = new List<Key>() { new Key() { Value = subNode.Text, Type = KeyElements.GlobalReference } } },
                                    DisplayName = new List<LangString>() { new LangString() { Text = subNode.Text } },
                                    Description = new List<LangString>() { new LangString() { Text = string.Empty /* TODO */ } }
                                };

                                output.Add(sub);
                            }
                        }
                    }
                }
            }

            // Apply filters
            if (output.Count > 0)
            {
                // Filter based on idShort
                if (!string.IsNullOrEmpty(idShort))
                {
                    var submodels = output.Where(s => s.IdShort.Equals(idShort, StringComparison.OrdinalIgnoreCase)).ToList();
                    if ((submodels == null) || (submodels?.Count == 0))
                    {
                        Console.WriteLine($"Submodels with IdShort {idShort} Not Found.");
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

        private async Task<List<SubmodelElement>> ReadSubmodelElementNodes(string nodesetIdentifier, NodesetViewerNode subNode, bool browseDeep)
        {
            List<SubmodelElement> output = new();

            List<NodesetViewerNode> submodelElementNodes = await _client.GetChildren(nodesetIdentifier, subNode.Id).ConfigureAwait(false);
            if (submodelElementNodes != null)
            {
                foreach (NodesetViewerNode smeNode in submodelElementNodes)
                {
                    if (browseDeep)
                    {
                        // check for children - if there are, create a smel instead of an sme
                        List<SubmodelElement> children = await ReadSubmodelElementNodes(nodesetIdentifier, smeNode, browseDeep).ConfigureAwait(false);
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
                                Value = await _client.VariableRead(nodesetIdentifier, smeNode.Id).ConfigureAwait(false)
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

        public List<ConceptDescription> GetAllConceptDescriptions(string idShort = null, string reqIsCaseOf = null, string reqDataSpecificationRef = null)
        {
            List<ConceptDescription> output = new();

            // get all concept descriptions
            List<NodesetViewerNode> nodeList = new(); // TODO
            if (nodeList != null)
            {
                foreach (NodesetViewerNode node in nodeList)
                {
                    if (node.Text == "Concept Descriptions")
                    {
                        List<NodesetViewerNode> conceptDescrNodes = new(); // TODO
                        if (conceptDescrNodes != null)
                        {
                            foreach (NodesetViewerNode cdNode in conceptDescrNodes)
                            {
                                ConceptDescription cd = new() {
                                    ModelType = ModelTypes.ConceptDescription,
                                    Identification = new Identifier() { Id = cdNode.Id, Value = cdNode.Text },
                                    IdShort = cdNode.Id + ";" + cdNode.Text,
                                    Id = cdNode.Id
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

        public async Task<AssetAdministrationShell> GetAssetAdministrationShellById(string nodesetIdentifier)
        {
            List<NodesetViewerNode> nodeList = await _client.GetChildren(nodesetIdentifier, ObjectIds.ObjectsFolder.ToString()).ConfigureAwait(false);
            if (nodeList != null)
            {
                foreach (NodesetViewerNode node in nodeList)
                {
                    if (node.Text == "Asset Admin Shells")
                    {
                        List<NodesetViewerNode> aasList = await _client.GetChildren(nodesetIdentifier, node.Id).ConfigureAwait(false);
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
                                List<NodesetViewerNode> assetsAndSubmodelRefs = await _client.GetChildren(nodesetIdentifier, a.Id).ConfigureAwait(false);
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

        public async Task<Submodel> GetSubmodelById(string nodesetIdentifier)
        {
            List<NodesetViewerNode> nodeList = await _client.GetChildren(nodesetIdentifier, ObjectIds.ObjectsFolder.ToString()).ConfigureAwait(false);
            if (nodeList != null)
            {
                foreach (NodesetViewerNode node in nodeList)
                {
                    if (node.Text == "Submodels")
                    {
                        List<NodesetViewerNode> submodelList = await _client.GetChildren(nodesetIdentifier, node.Id).ConfigureAwait(false);
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
                                    Description = new List<LangString>() { new LangString() { Text = await _client.VariableRead(nodesetIdentifier, subNode.Id).ConfigureAwait(false) } }
                                };

                                // get all submodel elements
                                sub.SubmodelElements.AddRange(await ReadSubmodelElementNodes(nodesetIdentifier, subNode, true).ConfigureAwait(false));

                                return sub;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public async Task<ConceptDescription> GetConceptDescriptionById(string nodesetIdentifier)
        {
            List<NodesetViewerNode> nodeList = await _client.GetChildren(nodesetIdentifier, ObjectIds.ObjectsFolder.ToString()).ConfigureAwait(false);
            if (nodeList != null)
            {
                foreach (NodesetViewerNode node in nodeList)
                {
                    if (node.Text == "Concept Descriptions")
                    {
                        List<NodesetViewerNode> conceptDescrNodes = await _client.GetChildren(nodesetIdentifier, node.Id).ConfigureAwait(false);
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
                                        Identification = new Identifier() { Id = cdNode.Id, Value = cdNode.Text },
                                        IdShort = cdNode.Id + ";" + cdNode.Text,
                                        Id = cdNode.Id
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

        public async Task<AssetInformation> GetAssetInformationFromAas(string nodesetIdentifier)
        {
            var aas = await GetAssetAdministrationShellById(nodesetIdentifier).ConfigureAwait(false);
            if (aas != null)
            {
                return aas.AssetInformation;
            }
            else
            {
                return null;
            }
        }

        public async Task<byte[]> GetFileByPath(string nodesetIdentifier, string idShortPath)
        {
            byte[] byteArray = null;
            string fileName = null;

            SubmodelElement sme = await GetSubmodelElementByPath(nodesetIdentifier, idShortPath).ConfigureAwait(false);
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

        public async Task<List<Reference>> GetAllSubmodelReferences(string nodesetIdentifier)
        {
            var aas = await GetAssetAdministrationShellById(nodesetIdentifier).ConfigureAwait(false);

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

        public async Task<List<SubmodelElement>> GetAllSubmodelElementsFromSubmodel(string nodesetIdentifier)
        {
            var submodel = await GetSubmodelById(nodesetIdentifier).ConfigureAwait(false);
            if (submodel == null)
            {
                return null;
            }
            else
            {
                return submodel.SubmodelElements;
            }
        }

        public async Task<SubmodelElement> GetSubmodelElementByPath(string nodesetIdentifier, string idShortPath)
        {
            Submodel submodel = await GetSubmodelById(nodesetIdentifier).ConfigureAwait(false);
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
