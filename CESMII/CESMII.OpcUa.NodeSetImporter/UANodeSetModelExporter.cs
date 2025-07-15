/* Author:      Chris Muench, C-Labs
 * Last Update: 4/8/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2021
 */

using Opc.Ua.Export;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CESMII.OpcUa.NodeSetModel.Export.Opc;
using CESMII.OpcUa.NodeSetModel.Opc.Extensions;
using Opc.Ua;
using System.Xml.Serialization;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace CESMII.OpcUa.NodeSetModel
{
    /// <summary>
    /// Exporter helper class
    /// </summary>
    public class UANodeSetModelExporter
    {
        public static string ExportNodeSetAsXml(NodeSetModel nodesetModel, Dictionary<string, NodeSetModel> nodesetModels, ILogger logger = null, Dictionary<string, string> aliases = null, bool encodeJsonScalarsAsValue = false)
        {
            return ExportNodeSetAsXmlAndNodeSet(nodesetModel, nodesetModels, logger, aliases, encodeJsonScalarsAsValue).NodeSetXml;
        }
        public static (string NodeSetXml, UANodeSet NodeSet) ExportNodeSetAsXmlAndNodeSet(NodeSetModel nodesetModel, Dictionary<string, NodeSetModel> nodesetModels, ILogger logger = null, Dictionary<string, string> aliases = null, bool encodeJsonScalarsAsValue = false)
        {
            var exportedNodeSet = ExportNodeSet(nodesetModel, nodesetModels, logger, aliases, encodeJsonScalarsAsValue: encodeJsonScalarsAsValue);

            string exportedNodeSetXml;
            // .Net6 changed the default to no-identation: https://github.com/dotnet/runtime/issues/64885
            using (var ms = new MemoryStream())
            {
                using (var writer = new StreamWriter(ms, Encoding.UTF8))
                {
                    try
                    {
                        using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true, }))
                        {
                            XmlSerializer serializer = new XmlSerializer(typeof(UANodeSet));
                            serializer.Serialize(xmlWriter, exportedNodeSet);
                        }
                    }
                    finally
                    {
                        writer.Flush();
                    }
                }
                var xmlBytes = ms.ToArray();
                if (string.IsNullOrEmpty(nodesetModel.HeaderComments))
                {
                    exportedNodeSetXml = Encoding.UTF8.GetString(xmlBytes);
                }
                else
                {
                    int secondLineIndex;
                    for (secondLineIndex = 0; secondLineIndex < xmlBytes.Length; secondLineIndex++)
                    {
                        if (xmlBytes[secondLineIndex] == '\r' || xmlBytes[secondLineIndex] == '\n')
                        {
                            secondLineIndex++;
                            if (xmlBytes[secondLineIndex + 1] == '\n')
                            {
                                secondLineIndex++;
                            }
                            break;
                        }
                    }
                    if (secondLineIndex < xmlBytes.Length - 1)
                    {
                        var sb = new StringBuilder();
                        sb.Append(Encoding.UTF8.GetString(xmlBytes, 0, secondLineIndex));
                        if (nodesetModel.HeaderComments.EndsWith("\r\n"))
                        {
                            sb.Append(nodesetModel.HeaderComments);
                        }
                        else
                        {
                            sb.AppendLine(nodesetModel.HeaderComments);
                        }
                        sb.Append(Encoding.UTF8.GetString(xmlBytes, secondLineIndex, xmlBytes.Length - secondLineIndex));
                        exportedNodeSetXml = sb.ToString();
                    }
                    else
                    {
                        exportedNodeSetXml = Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
            return (exportedNodeSetXml, exportedNodeSet);
        }
        public static UANodeSet ExportNodeSet(NodeSetModel nodeSetModel, Dictionary<string, NodeSetModel> nodeSetModels, ILogger logger, Dictionary<string, string> aliases = null, bool encodeJsonScalarsAsValue = false)
        {
            if (aliases == null)
            {
                aliases = new();
            }

            var exportedNodeSet = new UANodeSet();
            exportedNodeSet.LastModified = DateTime.UtcNow;
            exportedNodeSet.LastModifiedSpecified = true;

            var namespaceUris = nodeSetModel.AllNodesByNodeId.Values.Select(v => v.Namespace).Distinct().ToList();

            var requiredModels = new List<ModelTableEntry>();

            var context = new ExportContext(logger, nodeSetModels)
            {
                Aliases = aliases,
                ReencodeExtensionsAsJson = true,
                EncodeJsonScalarsAsValue = encodeJsonScalarsAsValue,
                _nodeIdsUsed = new HashSet<string>(),
                _exportedSoFar = new Dictionary<string, UANode>(),
            };

            foreach (var nsUri in namespaceUris)
            {
                context.NamespaceUris.GetIndexOrAppend(nsUri);
            }
            var items = ExportAllNodes(nodeSetModel, context);

            // remove unused aliases
            var usedAliases = aliases.Where(pk => context._nodeIdsUsed.Contains(pk.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);

            // Add aliases for all nodeids from other namespaces: aliases can only be used on references, not on the definitions
            var currentNodeSetNamespaceIndex = context.NamespaceUris.GetIndex(nodeSetModel.ModelUri);
            bool bAliasesAdded = false;
            foreach (var nodeId in context._nodeIdsUsed)
            {
                var parsedNodeId = NodeId.Parse(nodeId);
                if (parsedNodeId.NamespaceIndex != currentNodeSetNamespaceIndex
                    && !usedAliases.ContainsKey(nodeId))
                {
                    var namespaceUri = context.NamespaceUris.GetString(parsedNodeId.NamespaceIndex);
                    var nodeIdWithUri = new ExpandedNodeId(parsedNodeId, namespaceUri).ToString();
                    var nodeModel = nodeSetModels.Select(nm => nm.Value.AllNodesByNodeId.TryGetValue(nodeIdWithUri, out var model) ? model : null).FirstOrDefault(n => n != null);
                    var displayName = nodeModel?.DisplayName?.FirstOrDefault()?.Text;
                    if (displayName != null && !(nodeModel is InstanceModelBase))
                    {
                        if (!usedAliases.ContainsValue(displayName))
                        {
                            usedAliases.Add(nodeId, displayName);
                            aliases.Add(nodeId, displayName);
                            bAliasesAdded = true;
                        }
                        else
                        {
                            // name collision: number them
                            int i;
                            for (i = 1; i < 10000; i++)
                            {
                                var numberedDisplayName = $"{displayName}_{i}";
                                if (!usedAliases.ContainsValue(numberedDisplayName))
                                {
                                    usedAliases.Add(nodeId, numberedDisplayName);
                                    aliases.Add(nodeId, numberedDisplayName);
                                    bAliasesAdded = true;
                                    break;
                                }
                            }
                            if (i >= 10000)
                            {

                            }
                        }
                    }
                }
            }

            var aliasList = usedAliases
                .Select(alias => new NodeIdAlias { Alias = alias.Value, Value = alias.Key })
                .OrderBy(kv => GetNodeIdForSorting(kv.Value))
                .ToList();
            exportedNodeSet.Aliases = aliasList.ToArray();

            if (bAliasesAdded)
            {
                context._nodeIdsUsed = null; // No need to track anymore
                // Re-export with new aliases
                items = ExportAllNodes(nodeSetModel, context);
            }

            var allNamespaces = context.NamespaceUris.ToArray();
            if (allNamespaces.Length > 1)
            {
                exportedNodeSet.NamespaceUris = allNamespaces.Where(ns => ns != Namespaces.OpcUa).ToArray();
            }
            else
            {
                exportedNodeSet.NamespaceUris = allNamespaces;
            }

            // Export all referenced nodesets to capture any of their dependencies that may not be used in the model being exported
            foreach (var otherModel in nodeSetModels.Values.Where(m => m.ModelUri != Namespaces.OpcUa && !namespaceUris.Contains(m.ModelUri)))
            {
                // Only need to update the namespaces table
                context.Aliases = null;
                context._nodeIdsUsed = null;
                _ = ExportAllNodes(otherModel, context);
            }
            var allNamespacesIncludingDependencies = context.NamespaceUris.ToArray();

            foreach (var uaNamespace in allNamespacesIncludingDependencies.Except(namespaceUris))
            {
                if (!requiredModels.Any(m => m.ModelUri == uaNamespace))
                {
                    if (nodeSetModels.TryGetValue(uaNamespace, out var requiredNodeSetModel))
                    {
                        var requiredModel = new ModelTableEntry
                        {
                            ModelUri = uaNamespace,
                            Version = requiredNodeSetModel.Version,
                            PublicationDate = requiredNodeSetModel.PublicationDate.GetNormalizedPublicationDate(),
                            PublicationDateSpecified = requiredNodeSetModel.PublicationDate != null,
                            RolePermissions = null,
                            AccessRestrictions = 0,
                        };
                        requiredModels.Add(requiredModel);
                    }
                    else
                    {
                        // The model was not loaded. This can happen if the only reference to the model is in an extension object that only gets parsed but not turned into a node model (Example: onboarding nodeset refernces GDS ns=2;i=1)
                        var requiredModel = new ModelTableEntry
                        {
                            ModelUri = uaNamespace,
                        };
                        requiredModels.Add(requiredModel);
                    }
                }
            }

            var model = new ModelTableEntry
            {
                ModelUri = nodeSetModel.ModelUri,
                RequiredModel = requiredModels.ToArray(),
                AccessRestrictions = 0,
                PublicationDate = nodeSetModel.PublicationDate.GetNormalizedPublicationDate(),
                PublicationDateSpecified = nodeSetModel.PublicationDate != null,
                RolePermissions = null,
                Version = nodeSetModel.Version,
                XmlSchemaUri = nodeSetModel.XmlSchemaUri != nodeSetModel.ModelUri ? nodeSetModel.XmlSchemaUri : null
            };
            if (exportedNodeSet.Models != null)
            {
                var models = exportedNodeSet.Models.ToList();
                models.Add(model);
                exportedNodeSet.Models = models.ToArray();
            }
            else
            {
                exportedNodeSet.Models = new ModelTableEntry[] { model };
            }
            if (exportedNodeSet.Items != null)
            {
                var newItems = exportedNodeSet.Items.ToList();
                newItems.AddRange(items);
                exportedNodeSet.Items = newItems.ToArray();
            }
            else
            {
                exportedNodeSet.Items = items.ToArray();
            }
            return exportedNodeSet;
        }

        private static string GetNodeIdForSorting(string nodeId)
        {
            var intIdIndex = nodeId?.IndexOf("i=");
            if (intIdIndex >= 0 && int.TryParse(nodeId.Substring(intIdIndex.Value + "i=".Length), out var intIdValue))
            {
                return $"{nodeId.Substring(0, intIdIndex.Value)}i={intIdValue:D10}";
            }
            return nodeId;
        }

        private static List<UANode> ExportAllNodes(NodeSetModel nodesetModel, ExportContext context)
        {
            context._exportedSoFar = new Dictionary<string, UANode>();
            var itemsOrdered = new List<UANode>();
            var itemsOrderedSet = new HashSet<UANode>();
            foreach (var nodeModel in nodesetModel.AllNodesByNodeId.Values
                .OrderBy(GetNodeModelSortOrder)
                .ThenBy(n => GetNodeIdForSorting(n.NodeId)))
            {
                var result = NodeModelExportOpc.GetUANode(nodeModel, context);
                if (result.ExportedNode != null)
                {
                    if (context._exportedSoFar.TryAdd(result.ExportedNode.NodeId, result.ExportedNode) || !itemsOrderedSet.Contains(result.ExportedNode))
                    {
                        itemsOrdered.Add(result.ExportedNode);
                        itemsOrderedSet.Add(result.ExportedNode);
                    }
                    else
                    {
                        if (context._exportedSoFar[result.ExportedNode.NodeId] != result.ExportedNode)
                        {

                        }
                    }
                }
                if (result.AdditionalNodes != null)
                {
                    result.AdditionalNodes.ForEach(n =>
                    {
                        if (context._exportedSoFar.TryAdd(n.NodeId, n) || !itemsOrderedSet.Contains(n))
                        {
                            itemsOrdered.Add(n);
                            itemsOrderedSet.Add(n);
                        }
                        else
                        {
                            if (context._exportedSoFar[n.NodeId] != n)
                            {

                            }

                        }
                    });
                }
            }
            return itemsOrdered;
        }

        static int GetNodeModelSortOrder(NodeModel nodeModel)
        {
            if (nodeModel is ReferenceTypeModel) return 1;
            if (nodeModel is DataTypeModel) return 2;
            if (nodeModel is ObjectTypeModel) return 3;
            if (nodeModel is VariableTypeModel) return 4;
            return 5;
        }


    }
}
