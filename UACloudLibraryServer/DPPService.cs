using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    public class DPPService
    {
        private readonly ILogger _logger;
        private readonly UAClient _client;
        private readonly CloudLibDataProvider _dataProvider;

        public DPPService(UAClient client, CloudLibDataProvider dataProvider, ILoggerFactory loggerFactory)
        {
            _client = client;
            _dataProvider = dataProvider;
            _logger = loggerFactory.CreateLogger("DPPService");
        }

        public async Task<DigitalProductPassport> GetByDppId(string userId, string dppId)
        {
            DigitalProductPassport dpp = await BrowseDppFromRootAsync(userId, dppId).ConfigureAwait(false);
            if (dpp != null)
            {
                return dpp;
            }

            _logger.LogWarning("DPP not found for id {DppId}", dppId);
            return null;
        }

        public async Task<DigitalProductPassport> GetByProductId(string userId, string productId)
        {
            List<ObjectModel> dppList = _dataProvider.GetNodeModels(nsm => nsm.Objects, userId)
                .Where(nsm => (nsm.DisplayName != null) && (nsm.DisplayName.Count > 0) && (nsm.DisplayName[0].Text == "UniqueProductIdentifier") && (nsm.NodeId == productId))
                .ToList();

            if (dppList != null)
            {
                foreach (ObjectModel dpp in dppList)
                {
                    return await BrowseDppFromRootAsync(userId, dpp.NodeSet.Identifier).ConfigureAwait(false);
                }
            }

            _logger.LogWarning("DPP not found for product id {ProductId}", productId);
            return null;
        }

        public IReadOnlyList<string> GetDppIdsByProductIds(string userId, IReadOnlyList<string> productIds)
        {
            var result = new List<string>();

            List<ObjectModel> dppList = _dataProvider.GetNodeModels(nsm => nsm.Objects, userId)
                .Where(nsm => (nsm.DisplayName != null) && (nsm.DisplayName.Count > 0) && (nsm.DisplayName[0].Text == "UniqueProductIdentifier"))
                .ToList();

            if (dppList != null)
            {
                foreach (ObjectModel dpp in dppList)
                {
                    result.Add(dpp.NodeSet.Identifier);
                }
            }

            return result;
        }

        public async Task<DataElement> GetElement(string userId, string dppId, string elementPath)
        {
            DigitalProductPassport dpp = await GetByDppId(userId, dppId).ConfigureAwait(false);
            if (dpp == null)
            {
                return null;
            }

            // Resolve a dot-separated elementId path (e.g. "batteryInfo.capacity")
            // by recursively walking the DataElement tree
            string[] pathParts = elementPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return ReadElementRecursive(dpp.Elements, pathParts, 0);
        }

        // Browses the OPC UA address space to construct a DPP for the given nodeset identifier.
        private async Task<DigitalProductPassport> BrowseDppFromRootAsync(string userId, string nodesetIdentifier)
        {
            List<NodesetViewerNode> nodeList = await _client.GetChildren(userId, nodesetIdentifier, ObjectIds.ObjectsFolder.ToString()).ConfigureAwait(false);
            if (nodeList == null)
            {
                return null;
            }

            foreach (NodesetViewerNode node in nodeList)
            {
                if (node.Text == "DigitalProductPassport")
                {
                    return await GenerateDPPFromDPPNode(userId, nodesetIdentifier, node).ConfigureAwait(false);
                }
            }

            return null;
        }

        // Reads DPP properties and child elements from an OPC UA node, building the full DPP object graph.
        private async Task<DigitalProductPassport> GenerateDPPFromDPPNode(string userId, string nodesetIdentifier, NodesetViewerNode dppNode)
        {
            List<NodesetViewerNode> properties = await _client.GetChildren(userId, nodesetIdentifier, dppNode.Id).ConfigureAwait(false);
            if (properties == null)
            {
                return null;
            }

            string uniqueProductId = string.Empty;
            string schemaVersion = string.Empty;
            string status = string.Empty;
            string economicOperator = string.Empty;
            string facilityId = null;
            DateTimeOffset lastUpdate = DateTimeOffset.UtcNow;
            var elements = new List<DataElement>();

            foreach (NodesetViewerNode prop in properties)
            {
                switch (prop.Text)
                {
                    case "UniqueProductIdentifier":
                        uniqueProductId = await _client.VariableRead(userId, nodesetIdentifier, prop.Id).ConfigureAwait(false);
                        break;

                    case "DppSchemaVersion":
                        schemaVersion = await _client.VariableRead(userId, nodesetIdentifier, prop.Id).ConfigureAwait(false);
                        break;

                    case "DppStatus":
                        status = await _client.VariableRead(userId, nodesetIdentifier, prop.Id).ConfigureAwait(false);
                        break;

                    case "LastUpdate":
                        string lastUpdateStr = await _client.VariableRead(userId, nodesetIdentifier, prop.Id).ConfigureAwait(false);
                        if (DateTimeOffset.TryParse(lastUpdateStr, out var parsed))
                        {
                            lastUpdate = parsed;
                        }
                        break;

                    case "EconomicOperatorId":
                        economicOperator = await _client.VariableRead(userId, nodesetIdentifier, prop.Id).ConfigureAwait(false);
                        break;

                    case "FacilityId":
                        facilityId = await _client.VariableRead(userId, nodesetIdentifier, prop.Id).ConfigureAwait(false);
                        break;

                    case "Elements":
                        elements.AddRange(await ReadDataElementNodesAsync(userId, nodesetIdentifier, prop).ConfigureAwait(false));
                        break;
                }
            }

            return new DigitalProductPassport {
                DigitalProductPassportId = dppNode.Text,
                UniqueProductIdentifier = uniqueProductId,
                DppSchemaVersion = schemaVersion,
                DppStatus = status,
                LastUpdate = lastUpdate,
                EconomicOperatorId = economicOperator,
                FacilityId = facilityId,
                Elements = elements
            };
        }

        // Recursively reads OPC UA child nodes to build a DataElement tree
        private async Task<List<DataElement>> ReadDataElementNodesAsync(string userId, string nodesetIdentifier, NodesetViewerNode parentNode)
        {
            var output = new List<DataElement>();

            List<NodesetViewerNode> childNodes = await _client.GetChildren(userId, nodesetIdentifier, parentNode.Id).ConfigureAwait(false);
            if (childNodes == null)
            {
                return output;
            }

            foreach (NodesetViewerNode childNode in childNodes)
            {
                // Check for children – if present, create a collection; otherwise, a leaf element.
                List<DataElement> grandChildren = await ReadDataElementNodesAsync(userId, nodesetIdentifier, childNode).ConfigureAwait(false);
                if (grandChildren.Count > 0)
                {
                    output.Add(new DataElementCollection {
                        ElementId = childNode.Text,
                        Elements = grandChildren
                    });
                }
                else
                {
                    string value = await _client.VariableRead(userId, nodesetIdentifier, childNode.Id).ConfigureAwait(false);
                    output.Add(new SingleValuedDataElement {
                        ElementId = childNode.Text,
                        Value = JsonValue.Create(value)
                    });
                }
            }

            return output;
        }

        // Recursively walks the DataElement tree
        private static DataElement ReadElementRecursive(IReadOnlyList<DataElement> elements, string[] pathParts, int depth)
        {
            if (elements == null || depth >= pathParts.Length)
            {
                return null;
            }

            string targetId = pathParts[depth];

            foreach (var element in elements)
            {
                if (!string.Equals(element.ElementId, targetId, StringComparison.Ordinal))
                {
                    continue;
                }

                // Last segment – return the matched element.
                if (depth == pathParts.Length - 1)
                {
                    return element;
                }

                // Intermediate segment – drill into children if this is a collection.
                if (element is DataElementCollection collection)
                {
                    return ReadElementRecursive(collection.Elements, pathParts, depth + 1);
                }

                if (element is MultiValuedDataElement multi)
                {
                    return ReadElementRecursive(multi.Value, pathParts, depth + 1);
                }

                // Reached a leaf before consuming the full path.
                return null;
            }

            return null;
        }
    }
}
