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
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Cloud.Library.NodeSetIndex;
using Opc.Ua.Export;

namespace Opc.Ua.Cloud.Library
{
    public class CloudLibDataProvider
    {
        private readonly AppDbContext _dbContext = null;
        private readonly DbFileStorage _storage;
        private readonly ILogger _logger;
        private readonly bool _approvalRequired;

        public CloudLibDataProvider(AppDbContext context, ILoggerFactory logger, DbFileStorage storage, IConfiguration configuration)
        {
            _dbContext = context;
            _storage = storage;
            _logger = logger.CreateLogger("CloudLibDataProvider");
            _approvalRequired = configuration.GetSection("CloudLibrary")?.GetValue<bool>("ApprovalRequired") ?? false;
        }

        public IQueryable<NodeSetModel> GetNodeSets(
            string identifier = null,
            string modelUri = null,
            DateTime? publicationDate = null,
            string[] keywords = null)
        {
            IQueryable<NodeSetModel> nodeSets;
            if (!string.IsNullOrEmpty(identifier))
            {
                if (modelUri != null || publicationDate != null || keywords != null)
                {
                    throw new ArgumentException($"Must not specify other parameters when providing identifier.");
                }

                // Return unapproved nodesets only if request by identifier, but not in queries
                nodeSets = _dbContext.NodeSetsWithUnapproved.AsQueryable().Where(nsm => nsm.Identifier == identifier);
            }
            else
            {
                IQueryable<NodeSetModel> nodeSetQuery = SearchNodesets(keywords);
                if (modelUri != null && publicationDate != null)
                {
                    nodeSets = nodeSetQuery.Where(nsm => nsm.ModelUri == modelUri && nsm.PublicationDate == publicationDate);
                }
                else if (modelUri != null)
                {
                    nodeSets = nodeSetQuery.Where(nsm => nsm.ModelUri == modelUri);
                }
                else
                {
                    nodeSets = nodeSetQuery;
                }
            }

            return nodeSets;
        }

        public IQueryable<ObjectTypeModel> GetObjectTypes(string modelUri = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<ObjectTypeModel>(nsm => nsm.ObjectTypes, modelUri, publicationDate, nodeId);
        }

        public IQueryable<VariableTypeModel> GetVariableTypes(string modelUri = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<VariableTypeModel>(nsm => nsm.VariableTypes, modelUri, publicationDate, nodeId);
        }

        public IQueryable<DataTypeModel> GetDataTypes(string modelUri = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<DataTypeModel>(nsm => nsm.DataTypes, modelUri, publicationDate, nodeId);
        }

        public IQueryable<ReferenceTypeModel> GetReferenceTypes(string modelUri = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<ReferenceTypeModel>(nsm => nsm.ReferenceTypes, modelUri, publicationDate, nodeId);
        }

        public IQueryable<NamespaceMetaDataModel> NamespaceMetaData
        {
            get => _approvalRequired
                ? _dbContext.NamespaceMetaDataWithUnapproved.Where(n => n.ApprovalStatus == ApprovalStatus.Approved)
                : _dbContext.NamespaceMetaDataWithUnapproved;
        }

        public IQueryable<NodeSetModel> NodeSets
        {
            get =>
                _approvalRequired
                ? _dbContext.NodeSetsWithUnapproved.Where(n => _dbContext.NamespaceMetaDataWithUnapproved.Any(nmd => nmd.NodesetId == n.Identifier && nmd.ApprovalStatus == ApprovalStatus.Approved))
                : _dbContext.NodeSetsWithUnapproved;
        }

        private IQueryable<T> GetNodeModels<T>(Expression<Func<NodeSetModel, IEnumerable<T>>> selector, string modelUri = null, DateTime? publicationDate = null, string nodeId = null)
            where T : NodeModel
        {
            if (nodeId != null && modelUri == null)
            {
                var expandedNodeId = ExpandedNodeId.Parse(nodeId);
                if (expandedNodeId?.NamespaceUri != null)
                {
                    modelUri = expandedNodeId.NamespaceUri;
                }
            }

            IQueryable<NodeSetModel> nodeSets;
            if (modelUri != null && publicationDate != null)
            {
                nodeSets = NodeSets.AsQueryable().Where(nsm => nsm.ModelUri == modelUri && nsm.PublicationDate == publicationDate);
            }
            else if (modelUri != null)
            {
                nodeSets = NodeSets.AsQueryable().Where(nsm => nsm.ModelUri == modelUri);
            }
            else
            {
                nodeSets = NodeSets.AsQueryable();
            }

            IQueryable<T> nodeModels = nodeSets.SelectMany(selector);
            if (!string.IsNullOrEmpty(nodeId))
            {
                nodeModels = nodeModels.Where(ot => ot.NodeId == nodeId);
            }

            return nodeModels;
        }

        public async Task<string> AddMetaDataAsync(UANameSpace uaNamespace, UANodeSet nodeSet, uint legacyNodesetHashCode, string userId)
        {
            string message = "Internal error: transaction not executed";
            try
            {
                // NodeSetModel deletion relies on database cascading delete. The deletions need to be saved for this to happen before we can re-add the same nodeset
                // To achieve consistency across delete and re-add, the operations need to run under a database transaction

                await _dbContext.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(async () => {

                    message = null;

                    await DeleteAllRecordsForNodesetAsync(uaNamespace.Nodeset.Identifier).ConfigureAwait(false);

                    // delete any matching legacy nodesets
                    if (legacyNodesetHashCode != 0)
                    {
                        await DeleteAllRecordsForNodesetAsync(legacyNodesetHashCode).ConfigureAwait(false);
                    }

                    NodeSetModel existingModel = await _dbContext.NodeSetsWithUnapproved.FindAsync(nodeSet.Models[0].ModelUri, nodeSet.Models[0].GetNormalizedPublicationDate()).ConfigureAwait(false);
                    if (existingModel != null)
                    {
                        message = "Error: nodeset still exists after delete.";
                        return;
                    }

                    NamespaceMetaDataModel nameSpaceModel = MapRESTNamespaceToNamespaceMetaDataModel(uaNamespace, userId);


                    await _dbContext.AddAsync(nameSpaceModel).ConfigureAwait(false);

                    await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                },
                // This will only run on failures during transaction commit, where the EF can not determine if the Tx was committed or not
                () => _dbContext.NodeSetsWithUnapproved.AnyAsync(
                    n => n.ModelUri == nodeSet.Models[0].ModelUri && n.PublicationDate == (nodeSet.Models[0].PublicationDateSpecified ? nodeSet.Models[0].PublicationDate : default))
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                message = "Error: Could not save nodeset metadata";
            }

            return message;
        }

        public async Task IndexNodeSetModelAsync(UANodeSet nodeset, UANameSpace uaNamespace)
        {
            if (nodeset.Models.Length == 0)
            {
                throw new ArgumentException($"Invalid nodeset: no models specified");
            }

            NodeSetModel nodesetModel = await MapRESTNodesetXmlToNodeSetModel(nodeset.Models[0], uaNamespace, _dbContext).ConfigureAwait(false);

            // find our metadata model and link the two together
            NamespaceMetaDataModel metadataModel = await _dbContext.NamespaceMetaDataWithUnapproved.FirstOrDefaultAsync(n => n.NodesetId == nodesetModel.Identifier).ConfigureAwait(false);
            metadataModel.NodeSet = nodesetModel;
            nodesetModel.Metadata = metadataModel;

            new NodeModelFactoryOpc(nodesetModel, nodeset, _logger).ImportNodeSet();
            metadataModel.ValidationStatus = ValidationStatus.Indexed;

            await _dbContext.NodeSetsWithUnapproved.AddAsync(nodesetModel).ConfigureAwait(false);

            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<uint> IncrementDownloadCountAsync(uint nodesetId)
        {
            NamespaceMetaDataModel namespaceMeta = await _dbContext.NamespaceMetaDataWithUnapproved.FirstOrDefaultAsync(n => n.NodesetId == nodesetId.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);

            namespaceMeta.NumberOfDownloads++;
            uint newCount = namespaceMeta.NumberOfDownloads;

            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            return newCount;
        }

        public async Task DeleteAllRecordsForNodesetAsync(uint nodesetId)
        {
            try
            {
                string nodesetIdStr = nodesetId.ToString(CultureInfo.InvariantCulture);
                NodeSetModel nodeSetModel = await _dbContext.NodeSetsWithUnapproved.FirstOrDefaultAsync(n => n.Identifier == nodesetIdStr).ConfigureAwait(false);
                if (nodeSetModel != null)
                {
                    _dbContext.NodeSetsWithUnapproved.Remove(nodeSetModel);
                }

                NamespaceMetaDataModel namespaceModel = await _dbContext.NamespaceMetaDataWithUnapproved.FirstOrDefaultAsync(n => n.NodesetId == nodesetIdStr).ConfigureAwait(false);
                if (namespaceModel != null)
                {
                    _dbContext.NamespaceMetaDataWithUnapproved.Remove(namespaceModel);
                }

                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while deleting all records for {nodesetId}");

                throw;
            }
        }

        public async Task<UANameSpace> RetrieveAllMetadataAsync(uint nodesetId)
        {
            try
            {
#pragma warning disable CA1305 // Specify IFormatProvider: runs in database with single culture, can not use culture invariant
                NamespaceMetaDataModel namespaceModel = await _dbContext.NamespaceMetaDataWithUnapproved
                    .Where(md => md.NodesetId == nodesetId.ToString())
                    .Include(md => md.NodeSet)
                    .FirstOrDefaultAsync().ConfigureAwait(false);
#pragma warning restore CA1305 // Specify IFormatProvider

                if (namespaceModel == null)
                {
                    return null;
                }

                return MapNamespaceMetaDataModelToRESTNamespace(namespaceModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }

        internal IQueryable<NodeSetModel> SearchNodesets(string[] keywords)
        {
            IQueryable<NodeSetModel> matchingNodeSets;

            if ((keywords != null) && (keywords.Length != 0) && (keywords[0] != "*"))
            {
                string keywordRegex = $".*({string.Join('|', keywords)}).*";
#pragma warning disable CA1305 // Specify IFormatProvider - ToString() runs in the database, cultureinfo not supported
                matchingNodeSets =
                    NodeSets
                    .Where(nsm =>
                        NamespaceMetaData.Any(md =>
                            md.NodesetId == nsm.Identifier
                            && (Regex.IsMatch(md.Title, keywordRegex, RegexOptions.IgnoreCase)
                            || Regex.IsMatch(md.Description, keywordRegex, RegexOptions.IgnoreCase)
                            || Regex.IsMatch(md.NodeSet.ModelUri, keywordRegex, RegexOptions.IgnoreCase)
                            || Regex.IsMatch(string.Join(",", md.Keywords), keywordRegex, RegexOptions.IgnoreCase)
                            )
                        )
                    );
#pragma warning restore CA1305 // Specify IFormatProvider
            }
            else
            {
                matchingNodeSets = NodeSets.AsQueryable();
            }

            return matchingNodeSets;
        }

        public UANameSpace[] FindNodesets(string[] keywords, int? offset, int? limit)
        {
            var uaNamespaceModel = SearchNodesets(keywords)
                .OrderBy(n => n.ModelUri)
                .Skip(offset ?? 0)
                .Take(limit ?? 100)
                .Select(n => NamespaceMetaData.Where(nmd => nmd.NodesetId == n.Identifier).Include(nmd => nmd.NodeSet).FirstOrDefault())
                .ToList();

            return uaNamespaceModel.Select(MapNamespaceMetaDataModelToRESTNamespace).ToArray();
        }

        public Task<string[]> GetAllNamespacesAndNodesets()
        {
            try
            {
                string[] namesAndIds = NodeSets.Select(nsm => new { nsm.ModelUri, nsm.Identifier, nsm.Version, nsm.PublicationDate }).Select(n => $"{n.ModelUri},{n.Identifier},{n.Version},{n.PublicationDate}").ToArray();
                return Task.FromResult(namesAndIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Task.FromResult(Array.Empty<string>());
        }

        public Task<string[]> GetAllNamesAndNodesets()
        {
            try
            {
                var categoryAndNodesetIds = NamespaceMetaData.Select(md => new { md.Title, md.NodesetId }).ToList();
                string[] namesAndNodesetsString = categoryAndNodesetIds.Select(cn => $"{cn.Title},{cn.NodesetId}").ToArray();
                return Task.FromResult(namesAndNodesetsString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Task.FromResult(Array.Empty<string>());
        }

        public async Task<NamespaceMetaDataModel> ApproveNamespaceAsync(string identifier, ApprovalStatus status, string approvalInformation)
        {
            NamespaceMetaDataModel nodeSetMeta = await _dbContext.NamespaceMetaDataWithUnapproved.Where(n => n.NodesetId == identifier).FirstOrDefaultAsync().ConfigureAwait(false);
            if (nodeSetMeta == null) return null;

            nodeSetMeta.ApprovalStatus = status;
            nodeSetMeta.ApprovalInformation = approvalInformation;

            if (status == ApprovalStatus.Canceled)
            {
                try
                {
                    await _storage.DeleteFileAsync(nodeSetMeta.NodesetId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to delete file on Approval cancelation for {nodeSetMeta.NodesetId}: {ex.Message}");
                }

                await DeleteAllRecordsForNodesetAsync(uint.Parse(nodeSetMeta.NodesetId, CultureInfo.InvariantCulture)).ConfigureAwait(false);

                return nodeSetMeta;
            }

            _dbContext.Attach(nodeSetMeta);

            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            NamespaceMetaDataModel nodeSetMetaSaved = await _dbContext.NamespaceMetaDataWithUnapproved.Where(n => n.NodesetId == identifier).FirstOrDefaultAsync().ConfigureAwait(false);

            return nodeSetMetaSaved;
        }

        private NamespaceMetaDataModel MapRESTNamespaceToNamespaceMetaDataModel(UANameSpace uaNamespace, string userId)
        {
            // fix up license expression
            string licenseExpression = uaNamespace.License switch {
                "0" => "MIT",
                "1" or "ApacheLicense20" => "Apache-2.0",
                "2" => "Custom",
                _ => uaNamespace.License
            };

            NamespaceMetaDataModel metadataModel = new() {
                NodesetId = uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture),
                Title = uaNamespace.Title,
                License = licenseExpression,
                CopyrightText = uaNamespace.CopyrightText,
                Description = uaNamespace.Description,
                DocumentationUrl = uaNamespace.DocumentationUrl?.ToString(),
                IconUrl = uaNamespace.IconUrl?.ToString(),
                LicenseUrl = uaNamespace.LicenseUrl?.ToString(),
                Keywords = uaNamespace.Keywords,
                PurchasingInformationUrl = uaNamespace.PurchasingInformationUrl?.ToString(),
                ReleaseNotesUrl = uaNamespace.ReleaseNotesUrl?.ToString(),
                TestSpecificationUrl = uaNamespace.TestSpecificationUrl?.ToString(),
                SupportedLocales = uaNamespace.SupportedLocales,
                NumberOfDownloads = uaNamespace.NumberOfDownloads,
                ApprovalStatus = ApprovalStatus.Pending,
                ValidationStatus = ValidationStatus.Parsed,
                CreationTime = uaNamespace.CreationTime != null ? uaNamespace.CreationTime.GetNormalizedDate() : DateTime.UtcNow,
                UserId = userId
            };

            return metadataModel;
        }

        private async Task<NodeSetModel> MapRESTNodesetXmlToNodeSetModel(ModelTableEntry nodesetTableEntry, UANameSpace uaNamespace, AppDbContext dbContext)
        {
            NodeSetModel nodesetModel = new() {
                ModelUri = nodesetTableEntry.ModelUri,
                Version = nodesetTableEntry.Version,
                PublicationDate = nodesetTableEntry.GetNormalizedPublicationDate(),
                Identifier = uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture),
                LastModifiedDate = NodeModelUtils.GetNormalizedDate(uaNamespace.Nodeset.LastModifiedDate)
            };

            if (nodesetTableEntry.RequiredModel != null)
            {
                foreach (ModelTableEntry requiredModel in nodesetTableEntry.RequiredModel)
                {
                    DateTime? publicationDate = requiredModel.PublicationDateSpecified ? requiredModel.PublicationDate : null;
                    List<NodeSetModel> matchingNodeSets = await dbContext.Set<NodeSetModel>().Where(nsm => nsm.ModelUri == requiredModel.ModelUri).ToListAsync().ConfigureAwait(false);

                    NodeSetModel existingNodeSet = NodeModelUtils.GetMatchingOrHigherNodeSet(matchingNodeSets, publicationDate, requiredModel.Version);

                    var requiredModelInfo = new RequiredModelInfoModel {
                        ModelUri = requiredModel.ModelUri,
                        PublicationDate = requiredModel.PublicationDateSpecified ? ((DateTime?)requiredModel.PublicationDate).GetNormalizedDate() : null,
                        Version = requiredModel.Version,
                        AvailableModel = existingNodeSet,
                    };

                    nodesetModel.RequiredModels.Add(requiredModelInfo);
                }
            }

            return nodesetModel;
        }

        private UANameSpace MapNamespaceMetaDataModelToRESTNamespace(NamespaceMetaDataModel metadataModel)
        {
            return new UANameSpace {
                CreationTime = metadataModel.CreationTime,
                Title = metadataModel.Title,
                License = metadataModel.License,
                CopyrightText = metadataModel.CopyrightText,
                Description = metadataModel.Description,
                DocumentationUrl = metadataModel.DocumentationUrl != null ? new Uri(metadataModel.DocumentationUrl) : null,
                IconUrl = metadataModel.IconUrl != null ? new Uri(metadataModel.IconUrl) : null,
                LicenseUrl = metadataModel.LicenseUrl != null ? new Uri(metadataModel.LicenseUrl) : null,
                Keywords = metadataModel.Keywords,
                PurchasingInformationUrl = metadataModel.PurchasingInformationUrl != null ? new Uri(metadataModel.PurchasingInformationUrl) : null,
                ReleaseNotesUrl = metadataModel.ReleaseNotesUrl != null ? new Uri(metadataModel.ReleaseNotesUrl) : null,
                TestSpecificationUrl = metadataModel.TestSpecificationUrl != null ? new Uri(metadataModel.TestSpecificationUrl) : null,
                SupportedLocales = metadataModel.SupportedLocales,
                NumberOfDownloads = metadataModel.NumberOfDownloads,
                Nodeset = metadataModel.NodeSet == null ? null : MapNodeSetModelToRESTNodeSet(metadataModel.NodeSet)
            };
        }

        private Nodeset MapNodeSetModelToRESTNodeSet(NodeSetModel model)
        {
            return new Nodeset {
                Identifier = uint.Parse(model.Identifier, CultureInfo.InvariantCulture),
                NamespaceUri = model.ModelUri != null ? new Uri(model.ModelUri) : null,
                PublicationDate = model.PublicationDate ?? default,
                LastModifiedDate = model.LastModifiedDate ?? default,
                Version = model.Version,
                ValidationStatus = model.Metadata.ValidationStatus.ToString(),
                NodesetXml = null,
                RequiredModels = model.RequiredModels.Select(rm => {
                    Nodeset availableNodeSet = null;
                    if (rm.AvailableModel != null)
                    {
                        availableNodeSet = MapNodeSetModelToRESTNodeSet(rm.AvailableModel);
                    }

                    return new RequiredModelInfo {
                        NamespaceUri = rm.ModelUri,
                        PublicationDate = rm.PublicationDate,
                        Version = rm.Version,
                        AvailableModel = availableNodeSet
                    };
                }).ToList()
            };
        }

        public async Task<string[]> GetAllTypes(string nodeSetID)
        {
            NodeSetModel nodeSetMeta = await GetNodeSets(nodeSetID).FirstOrDefaultAsync().ConfigureAwait(false);
            if (nodeSetMeta != null)
            {
                List<NodeModel> types = new();
                types.AddRange(GetObjectTypes(nodeSetMeta.ModelUri));
                types.AddRange(GetVariableTypes(nodeSetMeta.ModelUri));
                types.AddRange(GetDataTypes(nodeSetMeta.ModelUri));
                types.AddRange(GetReferenceTypes(nodeSetMeta.ModelUri));

                List<string> typeList = new();
                foreach (NodeModel model in types)
                {
                    string expandedNodeIdWithBrowseName = "nsu=" + model.BrowseName + ";" + model.NodeIdIdentifier;
                    string[] tokens = expandedNodeIdWithBrowseName.Split(';');
                    if (tokens.Length >= 3)
                    {
                        string temp = tokens[1];
                        tokens[1] = tokens[2];
                        tokens[2] = temp;
                    }
                    expandedNodeIdWithBrowseName = string.Join(";", tokens);

                    typeList.Add(expandedNodeIdWithBrowseName);
                }

                return typeList.ToArray();
            }

            return Array.Empty<string>();
        }

        public Task<string> GetUAType(string expandedNodeId)
        {
            // create a substring from expandedNodeId by removing "nsu=" from the start and parsing until the first ";"
            string modelUri = expandedNodeId.Substring(4, expandedNodeId.IndexOf(';', StringComparison.OrdinalIgnoreCase) - 4);

            JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
                MaxDepth = 100,
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };

            ObjectTypeModel objectModel = GetNodeModels<ObjectTypeModel>(nsm => nsm.ObjectTypes, modelUri, null, expandedNodeId).FirstOrDefault();
            if (objectModel != null)
            {
                string expandedNodeIdWithBrowseName = "nsu=" + objectModel.BrowseName + ";" + objectModel.NodeIdIdentifier;
                string[] tokens = expandedNodeIdWithBrowseName.Split(';');
                if (tokens.Length >= 3)
                {
                    string temp = tokens[1];
                    tokens[1] = tokens[2];
                    tokens[2] = temp;
                }
                expandedNodeIdWithBrowseName = string.Join(";", tokens);

                return Task.FromResult("{\"JsonSchema\": " + options.GetJsonSchemaAsNode(typeof(UAObjectType)).ToString() + ",\r\n\"Value\": \"" + expandedNodeIdWithBrowseName + "\"}");
            }

            VariableTypeModel variableModel = GetNodeModels<VariableTypeModel>(nsm => nsm.VariableTypes, modelUri, null, expandedNodeId).FirstOrDefault();
            if (variableModel != null)
            {
                string expandedNodeIdWithBrowseName = "nsu=" + variableModel.BrowseName + ";" + variableModel.NodeIdIdentifier;
                string[] tokens = expandedNodeIdWithBrowseName.Split(';');
                if (tokens.Length >= 3)
                {
                    string temp = tokens[1];
                    tokens[1] = tokens[2];
                    tokens[2] = temp;
                }
                expandedNodeIdWithBrowseName = string.Join(";", tokens);

                return Task.FromResult("{\"JsonSchema\": " + options.GetJsonSchemaAsNode(typeof(UAVariableType)).ToString() + ",\r\n\"Value\": \"" + expandedNodeIdWithBrowseName + "\"}");
            }

            DataTypeModel dataModel = GetNodeModels<DataTypeModel>(nsm => nsm.DataTypes, modelUri, null, expandedNodeId).FirstOrDefault();
            if (dataModel != null)
            {
                string expandedNodeIdWithBrowseName = "nsu=" + dataModel.BrowseName + ";" + dataModel.NodeIdIdentifier;
                string[] tokens = expandedNodeIdWithBrowseName.Split(';');
                if (tokens.Length >= 3)
                {
                    string temp = tokens[1];
                    tokens[1] = tokens[2];
                    tokens[2] = temp;
                }
                expandedNodeIdWithBrowseName = string.Join(";", tokens);

                List<string> fields = new();
                foreach (DataTypeModel.StructureField field in dataModel.StructureFields)
                {
                    fields.Add(field.DataType + ":" + field.Name);
                }

                string fieldsSerialized = JsonSerializer.Serialize(fields, options);

                return Task.FromResult("{\"JsonSchema\": " + options.GetJsonSchemaAsNode(typeof(UADataType)).ToString() + ",\r\n\"Value\": \"" + expandedNodeIdWithBrowseName + "\",\r\n\"Structure Fields\": " + fieldsSerialized + "}");
            }

            ReferenceTypeModel referenceModel = GetNodeModels<ReferenceTypeModel>(nsm => nsm.ReferenceTypes, modelUri, null, expandedNodeId).FirstOrDefault();
            if (referenceModel != null)
            {
                string expandedNodeIdWithBrowseName = "nsu=" + referenceModel.BrowseName + ";" + referenceModel.NodeIdIdentifier;
                string[] tokens = expandedNodeIdWithBrowseName.Split(';');
                if (tokens.Length >= 3)
                {
                    string temp = tokens[1];
                    tokens[1] = tokens[2];
                    tokens[2] = temp;
                }
                expandedNodeIdWithBrowseName = string.Join(";", tokens);

                return Task.FromResult("{\"JsonSchema\": " + options.GetJsonSchemaAsNode(typeof(UAReferenceType)).ToString() + ",\r\n\"Value\": \"" + expandedNodeIdWithBrowseName + "\"}");
            }

            return Task.FromResult("{}");
        }
    }
}
