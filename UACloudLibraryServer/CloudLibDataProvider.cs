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
using AdminShell;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Interfaces;
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

        public IQueryable<CloudLibNodeSetModel> GetNodeSets(
            string identifier = null,
            string modelUri = null,
            DateTime? publicationDate = null,
            string[] keywords = null)
        {

            IQueryable<CloudLibNodeSetModel> nodeSets;
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
                IQueryable<CloudLibNodeSetModel> nodeSetQuery = SearchNodesets(keywords);
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

        public IQueryable<CloudLibNodeSetModel> NodeSets
        {
            get =>
                _approvalRequired
                ? _dbContext.NodeSetsWithUnapproved.Where(n => _dbContext.NamespaceMetaDataWithUnapproved.Any(nmd => nmd.NodesetId == n.Identifier && nmd.ApprovalStatus == ApprovalStatus.Approved))
                : _dbContext.NodeSetsWithUnapproved;
        }

        private IQueryable<T> GetNodeModels<T>(Expression<Func<CloudLibNodeSetModel, IEnumerable<T>>> selector, string modelUri = null, DateTime? publicationDate = null, string nodeId = null)
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

            IQueryable<CloudLibNodeSetModel> nodeSets;
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

                    CloudLibNodeSetModel nodeSetModel = await CloudLibNodeSetModel.FromModelAsync(nodeSet.Models[0], _dbContext).ConfigureAwait(false);
                    nodeSetModel.Identifier = uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture);
                    nodeSetModel.LastModifiedDate = nodeSet.LastModifiedSpecified ? ((DateTime?)nodeSet.LastModified).GetNormalizedPublicationDate() : null;

                    CloudLibNodeSetModel existingModel = await _dbContext.NodeSetsWithUnapproved.FindAsync(nodeSetModel.ModelUri, nodeSetModel.PublicationDate).ConfigureAwait(false);
                    if (existingModel != null)
                    {
                        message = "Error: nodeset still exists after delete.";
                        return;
                    }

                    var nameSpaceModel = new NamespaceMetaDataModel();
                    MapToEntity(ref nameSpaceModel, uaNamespace, nodeSetModel);

                    nodeSetModel.ValidationStatus = ValidationStatus.Parsed;
                    nodeSetModel.ValidationStatusInfo = null;

                    nameSpaceModel.UserId = userId;

                    await _dbContext.AddAsync(nameSpaceModel).ConfigureAwait(false);
                    await _dbContext.NodeSetsWithUnapproved.AddAsync(nodeSetModel).ConfigureAwait(false);

                    await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                },
                // This will only run on failures during transaction commit, where the EF can not determine if the Tx was committed or not
                () => _dbContext.NodeSetsWithUnapproved.AsNoTracking()
                    .AnyAsync(n => n.ModelUri == nodeSet.Models[0].ModelUri && n.PublicationDate == (nodeSet.Models[0].PublicationDateSpecified ? nodeSet.Models[0].PublicationDate : default))
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                message = "Error: Could not save nodeset metadata";
            }

            return message;
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
                CloudLibNodeSetModel nodeSetModel = await _dbContext.NodeSetsWithUnapproved.FirstOrDefaultAsync(n => n.Identifier == nodesetIdStr).ConfigureAwait(false);
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
            UANameSpace nameSpace = new();
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
                MapToNamespace(nameSpace, namespaceModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
            return nameSpace;
        }

        internal IQueryable<CloudLibNodeSetModel> SearchNodesets(string[] keywords)
        {
            IQueryable<CloudLibNodeSetModel> matchingNodeSets;

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

            UANameSpace[] nodesetResults = uaNamespaceModel.Select(nameSpace => {
                var result = new UANameSpace();
                MapToNamespace(result, nameSpace);

                return result;
            }).ToArray();
            return nodesetResults;
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

        private void MapToEntity(ref NamespaceMetaDataModel entity, UANameSpace uaNamespace, CloudLibNodeSetModel nodeSetModel)
        {
            string identifier = nodeSetModel != null ? nodeSetModel.Identifier : uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture);
            entity.NodesetId = identifier;
            if (uaNamespace.CreationTime != null)
            {
                entity.CreationTime = uaNamespace.CreationTime.GetNormalizedPublicationDate();
            }
            else
            {
                entity.CreationTime = DateTime.Now;
            }

            entity.NodeSet = nodeSetModel;
            entity.Title = uaNamespace.Title;

            string licenseExpression = uaNamespace.License switch {
                "0" => "MIT",
                "1" or "ApacheLicense20" => "Apache-2.0",
                "2" => "Custom",
                _ => uaNamespace.License,
            };

            // TODO Validate license Expression
            entity.License = licenseExpression;
            entity.CopyrightText = uaNamespace.CopyrightText;
            entity.Description = uaNamespace.Description;
            entity.DocumentationUrl = uaNamespace.DocumentationUrl?.ToString();
            entity.IconUrl = uaNamespace.IconUrl?.ToString();
            entity.LicenseUrl = uaNamespace.LicenseUrl?.ToString();
            entity.Keywords = uaNamespace.Keywords;
            entity.PurchasingInformationUrl = uaNamespace.PurchasingInformationUrl?.ToString();
            entity.ReleaseNotesUrl = uaNamespace.ReleaseNotesUrl?.ToString();
            entity.TestSpecificationUrl = uaNamespace.TestSpecificationUrl?.ToString();
            entity.SupportedLocales = uaNamespace.SupportedLocales;
            entity.NumberOfDownloads = uaNamespace.NumberOfDownloads;
            entity.ApprovalStatus = ApprovalStatus.Pending;
        }

        private void MapToNamespace(UANameSpace uaNamespace, NamespaceMetaDataModel model)
        {
            if (model.NodeSet == null)
            {
                uaNamespace.Nodeset = null;
            }
            else
            {
                MapToNodeSet(uaNamespace.Nodeset, model.NodeSet);
            }
            uaNamespace.CreationTime = model.CreationTime;
            uaNamespace.Title = model.Title;
            uaNamespace.License = model.License;
            uaNamespace.CopyrightText = model.CopyrightText;
            uaNamespace.Description = model.Description;
            uaNamespace.DocumentationUrl = model.DocumentationUrl != null ? new Uri(model.DocumentationUrl) : null;
            uaNamespace.IconUrl = model.IconUrl != null ? new Uri(model.IconUrl) : null;
            uaNamespace.LicenseUrl = model.LicenseUrl != null ? new Uri(model.LicenseUrl) : null;
            uaNamespace.Keywords = model.Keywords;
            uaNamespace.PurchasingInformationUrl = model.PurchasingInformationUrl != null ? new Uri(model.PurchasingInformationUrl) : null;
            uaNamespace.ReleaseNotesUrl = model.ReleaseNotesUrl != null ? new Uri(model.ReleaseNotesUrl) : null;
            uaNamespace.TestSpecificationUrl = model.TestSpecificationUrl != null ? new Uri(model.TestSpecificationUrl) : null;
            uaNamespace.SupportedLocales = model.SupportedLocales;
            uaNamespace.NumberOfDownloads = model.NumberOfDownloads;
        }

        private void MapToNodeSet(Nodeset nodeset, NodeSetModel model)
        {
            nodeset.Identifier = uint.Parse(model.Identifier, CultureInfo.InvariantCulture);
            nodeset.NamespaceUri = model.ModelUri != null ? new Uri(model.ModelUri) : null;
            nodeset.PublicationDate = model.PublicationDate ?? default;
            nodeset.Version = model.Version;
            nodeset.ValidationStatus = (model as CloudLibNodeSetModel)?.ValidationStatus.ToString();
            nodeset.LastModifiedDate = (model as CloudLibNodeSetModel)?.LastModifiedDate ?? default;
            nodeset.NodesetXml = null;
            nodeset.RequiredModels = model.RequiredModels.Select(rm => {
                Nodeset availableNodeSet = null;
                if (rm.AvailableModel != null)
                {
                    availableNodeSet = new();
                    MapToNodeSet(availableNodeSet, rm.AvailableModel);
                }

                return new RequiredModelInfo { NamespaceUri = rm.ModelUri, PublicationDate = rm.PublicationDate, Version = rm.Version, AvailableModel = availableNodeSet };
            }).ToList();
        }

        public async Task<string[]> GetAllTypes(string nodeSetID)
        {
            CloudLibNodeSetModel nodeSetMeta = await GetNodeSets(nodeSetID).FirstOrDefaultAsync().ConfigureAwait(false);
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
                    fields.Add(field.DataType.NodeId + ":" + field.DataType.BrowseName + ":" + field.Name);
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
