using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Opc.Ua;

namespace AdminShell
{
    public class AssetAdministrationShellEnvironmentService
    {
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
                                    Description = new List<LangString>() { new LangString() { Text = "" /*TODO*/ } }
                                };

                                // get all submodel elements
                                sub.SubmodelElements.AddRange(ReadSubmodelElementNodes(subNode, false));

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

        private List<SubmodelElement> ReadSubmodelElementNodes(NodesetViewerNode subNode, bool browseDeep)
        {
            List<SubmodelElement> output = new();

            List<NodesetViewerNode> submodelElementNodes = new(); // TODO
            if (submodelElementNodes != null)
            {
                foreach (NodesetViewerNode smeNode in submodelElementNodes)
                {
                    if (browseDeep)
                    {
                        // check for children - if there are, create a smel instead of an sme
                        List<SubmodelElement> children = ReadSubmodelElementNodes(smeNode, browseDeep);
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
                            Property sme = new() {
                                ModelType = ModelTypes.Property,
                                DisplayName = new List<LangString>() { new LangString() { Text = smeNode.Text } },
                                IdShort = smeNode.Text,
                                SemanticId = new SemanticId() { Type = KeyElements.ExternalReference, Keys = new List<Key>() { new Key() { Value = smeNode.Text, Type = KeyElements.GlobalReference } } },
                                Value = "0" // TODO: Read the actual value from the node
                            };

                            output.Add(sme);
                        }
                    }
                    else
                    {
                        // add just one property to be spec conform
                        Property sme = new() {
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

        public AssetAdministrationShell GetAssetAdministrationShellById(string aasIdentifier)
        {
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
                                if (a.Id.Equals(aasIdentifier, StringComparison.OrdinalIgnoreCase))
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

                                    return aas;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        public Submodel GetSubmodelById(string submodelIdentifier)
        {
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
                                if (subNode.Id.Equals(submodelIdentifier, StringComparison.OrdinalIgnoreCase))
                                {
                                    Submodel sub = new() {
                                        ModelType = ModelTypes.Submodel,
                                        Id = subNode.Id,
                                        Identification = new Identifier() { Id = subNode.Id, Value = subNode.Text },
                                        IdShort = subNode.Id + ";" + subNode.Text,
                                        SemanticId = new Reference() { Type = KeyElements.ExternalReference, Keys = new List<Key>() { new Key() { Value = subNode.Text, Type = KeyElements.GlobalReference } } },
                                        DisplayName = new List<LangString>() { new LangString() { Text = subNode.Text } },
                                        Description = new List<LangString>() { new LangString() { Text = "" /* TODO */ } }
                                    };

                                    // get all submodel elements
                                    sub.SubmodelElements.AddRange(ReadSubmodelElementNodes(subNode, true));

                                    return sub;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        public ConceptDescription GetConceptDescriptionById(string cdIdentifier)
        {
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
                                if (cdNode.Id.Equals(cdIdentifier, StringComparison.OrdinalIgnoreCase))
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

        public AssetInformation GetAssetInformationFromAas(string aasIdentifier)
        {
            var aas = GetAssetAdministrationShellById(aasIdentifier);
            if (aas != null)
            {
                return aas.AssetInformation;
            }
            else
            {
                return null;
            }
        }

        public string GetFileByPath(string submodelIdentifier, string idShortPath, out byte[] byteArray, out long fileSize)
        {
            byteArray = null;
            string fileName = null;
            fileSize = 0;

            SubmodelElement sme = GetSubmodelElementByPath(submodelIdentifier, idShortPath);
            if (sme != null)
            {
                if (sme is File file)
                {
                    fileName = file.Value;
                    byteArray = System.IO.File.ReadAllBytes(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", fileName));
                    fileSize = byteArray.Length;
                }
                else
                {
                    throw new ArgumentException($"Submodel element {sme.IdShort} is not of Type File.");
                }
            }

            return fileName;
        }

        public List<Reference> GetAllSubmodelReferences(string decodedAasId)
        {
            var aas = GetAssetAdministrationShellById(decodedAasId);

            if (aas != null)
            {
                List<Reference> references = new();
                foreach (ModelReference smr in aas.Submodels)
                {
                    references.Add(smr);
                }

                return references;
            }
            else
            {
                return null;
            }
        }

        public List<SubmodelElement> GetAllSubmodelElementsFromSubmodel(string submodelIdentifier)
        {
            var submodel = GetSubmodelById(submodelIdentifier);
            if (submodel == null)
            {
                return null;
            }
            else
            {
                return submodel.SubmodelElements;
            }
        }

        public SubmodelElement GetSubmodelElementByPath(string submodelIdentifier, string idShortPath)
        {
            Submodel submodel = GetSubmodelById(submodelIdentifier);
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
