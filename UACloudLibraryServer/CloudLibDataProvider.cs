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

        public CloudLibDataProvider(AppDbContext context, ILoggerFactory logger, DbFileStorage storage)
        {
            _dbContext = context;
            _storage = storage;
            _logger = logger.CreateLogger("CloudLibDataProvider");

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
                nodeSets = _dbContext.nodeSetsWithUnapproved.AsQueryable().Where(nsm => nsm.Identifier == identifier);
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
                nodeSets = _dbContext.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == modelUri && nsm.PublicationDate == publicationDate);
            }
            else if (modelUri != null)
            {
                nodeSets = _dbContext.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == modelUri);
            }
            else
            {
                nodeSets = _dbContext.nodeSets.AsQueryable();
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
                // To achieve consistency across delete and re-add, the operations need to run under a database trasaction

                await _dbContext.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(async () => {
                    message = null;

                    // delete any existing records for this nodeset in the database
                    if (!await DeleteAllRecordsForNodesetAsync(uaNamespace.Nodeset.Identifier).ConfigureAwait(false))
                    {
                        message = "Error: Could not delete existing records for nodeset!";
                        return;
                    }

                    // delete any matching legacy nodesets
                    if (legacyNodesetHashCode != 0 && !await DeleteAllRecordsForNodesetAsync(legacyNodesetHashCode).ConfigureAwait(false))
                    {
                        message = "Error: Could not delete existing legacy records for nodeset!";
                        return;
                    }

                    CloudLibNodeSetModel nodeSetModel = await CloudLibNodeSetModel.FromModelAsync(nodeSet.Models[0], _dbContext).ConfigureAwait(false);

                    nodeSetModel.Identifier = uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture);
                    nodeSetModel.LastModifiedDate = nodeSet.LastModifiedSpecified ? ((DateTime?)nodeSet.LastModified).GetNormalizedPublicationDate() : null;

                    await _dbContext.AddAsync(nodeSetModel).ConfigureAwait(false);
                    await _dbContext.SaveChangesAsync().ConfigureAwait(false);

                    CloudLibNodeSetModel existingModel = await _dbContext.nodeSetsWithUnapproved.FindAsync(nodeSetModel.ModelUri, nodeSetModel.PublicationDate).ConfigureAwait(false);
                    if (existingModel != null)
                    {
                        message = "Error: nodeset still exists after delete.";
                        return;
                    }
                    else
                    {
                        await _dbContext.nodeSetsWithUnapproved.AddAsync(nodeSetModel).ConfigureAwait(false);
                    }

                    var nameSpaceModel = new NamespaceMetaDataModel();
                    MapToEntity(ref nameSpaceModel, uaNamespace, nodeSetModel);

                    nodeSetModel.ValidationStatus = ValidationStatus.Parsed;
                    nodeSetModel.ValidationStatusInfo = null;
                    nameSpaceModel.UserId = userId;

                    await _dbContext.AddAsync(nameSpaceModel).ConfigureAwait(false);

                    await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                },
                // This will only run on failures during transaction commit, where the EF can not determine if the Tx was committed or not
                () => _dbContext.nodeSetsWithUnapproved.AsNoTracking()
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

        public async Task<bool> DeleteAllRecordsForNodesetAsync(uint nodesetId)
        {
            try
            {
                string nodesetIdStr = nodesetId.ToString(CultureInfo.InvariantCulture);
                List<CloudLibNodeSetModel> deletedNodeSets = new();
                await DeleteNodeSetIndexForNodesetAsync(nodesetIdStr, deletedNodeSets).ConfigureAwait(false);

                NamespaceMetaDataModel namespaceModel = await _dbContext.NamespaceMetaDataWithUnapproved.FirstOrDefaultAsync(n => n.NodesetId == nodesetIdStr).ConfigureAwait(false);
                if (namespaceModel != null)
                {
                    _dbContext.NamespaceMetaDataWithUnapproved.Remove(namespaceModel);
                }

                await _dbContext.SaveChangesAsync().ConfigureAwait(false);

                // Re-add nodesets that were removed because they depend on the nodeset being deleted
                // This will cause them to be reindexed later on
                foreach (CloudLibNodeSetModel deletedNodeSet in deletedNodeSets)
                {
                    if (deletedNodeSet.Identifier != nodesetIdStr)
                    {
                        var newNodeSetModel = new CloudLibNodeSetModel {
                            Identifier = deletedNodeSet.Identifier,
                            ModelUri = deletedNodeSet.ModelUri,
                            PublicationDate = deletedNodeSet.PublicationDate,
                            Version = deletedNodeSet.Version,
                            LastModifiedDate = deletedNodeSet.LastModifiedDate,
                            ValidationStatus = ValidationStatus.Parsed,
                            RequiredModels = deletedNodeSet.RequiredModels.Select(rm => new RequiredModelInfoModel { ModelUri = rm.ModelUri, PublicationDate = rm.PublicationDate, Version = rm.Version }).ToList(),
                        };

                        await _dbContext.nodeSetsWithUnapproved.AddAsync(newNodeSetModel).ConfigureAwait(false);
                    }
                }
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while deleting all records for {nodesetId}");
                // rethrow so db execution policy can do retries if so configured
                throw;
            }
        }

        public async Task DeleteNodeSetIndexForNodesetAsync(string nodesetId, List<CloudLibNodeSetModel> deletedNodeSets)
        {
            CloudLibNodeSetModel nodeSetModel = await _dbContext.nodeSetsWithUnapproved.FirstOrDefaultAsync(n => n.Identifier == nodesetId).ConfigureAwait(false);
            if (nodeSetModel != null)
            {
                foreach (CloudLibNodeSetModel dependentNodeset in _dbContext.nodeSetsWithUnapproved
                    .Where(n => n.RequiredModels
                        .Any(rm => rm.AvailableModel.ModelUri == nodeSetModel.ModelUri && rm.AvailableModel.PublicationDate == nodeSetModel.PublicationDate)))
                {
                    // Delete any dependent nodesets, so we can re-index them from scratch
                    await DeleteNodeSetIndexForNodesetAsync(dependentNodeset.Identifier, deletedNodeSets).ConfigureAwait(false);
                }

                if (!deletedNodeSets.Contains(nodeSetModel))
                {
                    _dbContext.nodeSetsWithUnapproved.Remove(nodeSetModel);
                    deletedNodeSets.Add(nodeSetModel);
                }
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
                    _dbContext.nodeSets
                    .Where(nsm =>
                        _dbContext.NamespaceMetaData.Any(md =>
                            md.NodesetId == nsm.Identifier
                            && (Regex.IsMatch(md.Title, keywordRegex, RegexOptions.IgnoreCase)
                            || Regex.IsMatch(md.Description, keywordRegex, RegexOptions.IgnoreCase)
                            || Regex.IsMatch(md.NodeSet.ModelUri, keywordRegex, RegexOptions.IgnoreCase)
                            || Regex.IsMatch(string.Join(",", md.Keywords), keywordRegex, RegexOptions.IgnoreCase)
                            || Regex.IsMatch(md.Category.Name, keywordRegex, RegexOptions.IgnoreCase)
                            || Regex.IsMatch(md.Contributor.Name, keywordRegex, RegexOptions.IgnoreCase))
                            // Fulltext appears to be slower than regex: && EF.Functions.ToTsVector("english", md.Title + " || " + md.Description/* + " " + string.Join(' ', md.Keywords) + md.Category.Name + md.Contributor.Name*/).Matches(keywordTsQuery))
                            )
                        );
#pragma warning restore CA1305 // Specify IFormatProvider
            }
            else
            {
                matchingNodeSets = _dbContext.nodeSets.AsQueryable();
            }
            return matchingNodeSets;
        }

        public UANameSpace[] FindNodesets(string[] keywords, int? offset, int? limit)
        {
            var uaNamespaceModel = SearchNodesets(keywords)
                .OrderBy(n => n.ModelUri)
                .Skip(offset ?? 0)
                .Take(limit ?? 100)
                .Select(n => _dbContext.NamespaceMetaData.Where(nmd => nmd.NodesetId == n.Identifier).Include(nmd => nmd.NodeSet).FirstOrDefault())
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
                string[] namesAndIds = _dbContext.nodeSets.Select(nsm => new { nsm.ModelUri, nsm.Identifier, nsm.Version, nsm.PublicationDate }).Select(n => $"{n.ModelUri},{n.Identifier},{n.Version},{n.PublicationDate}").ToArray();
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
                var categoryAndNodesetIds = _dbContext.NamespaceMetaData.Select(md => new { md.Category.Name, md.NodesetId }).ToList();
                string[] namesAndNodesetsString = categoryAndNodesetIds.Select(cn => $"{cn.Name},{cn.NodesetId}").ToArray();
                return Task.FromResult(namesAndNodesetsString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Task.FromResult(Array.Empty<string>());
        }

        public async Task<NamespaceMetaDataModel> ApproveNamespaceAsync(string identifier, ApprovalStatus status, string approvalInformation, List<UAProperty> additionalProperties)
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

                if (!await DeleteAllRecordsForNodesetAsync(uint.Parse(nodeSetMeta.NodesetId, CultureInfo.InvariantCulture)).ConfigureAwait(false))
                {
                    _logger.LogWarning($"Failed to delete records on Approval cancelation for {nodeSetMeta.NodesetId}");
                    return null;
                }
                return nodeSetMeta;
            }
            _dbContext.Attach(nodeSetMeta);
            if (additionalProperties != null)
            {
                foreach (UAProperty prop in additionalProperties)
                {
                    if (string.IsNullOrEmpty(prop.Value))
                    {
                        nodeSetMeta.AdditionalProperties.RemoveAll(p => p.Name == prop.Name);
                    }
                    else
                    {
                        AdditionalPropertyModel existingProp = nodeSetMeta.AdditionalProperties.FirstOrDefault(p => p.Name == prop.Name);
                        if (existingProp != null)
                        {
                            existingProp.Value = prop.Value;
                        }
                        else
                        {
                            var newProp = new AdditionalPropertyModel {
                                Name = prop.Name,
                                Value = prop.Value,
                            };
                            nodeSetMeta.AdditionalProperties.Add(newProp);
                        }
                    }
                }
            }
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
            entity.ContributorId = 0;
            entity.Contributor = new OrganisationModel {
                Name = uaNamespace.Contributor.Name,
                Description = uaNamespace.Contributor.Description,
                ContactEmail = uaNamespace.Contributor.ContactEmail,
                LogoUrl = uaNamespace.Contributor.LogoUrl?.ToString(),
                Website = uaNamespace.Contributor.Website?.ToString(),
            };

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
            entity.CategoryId = 0;
            entity.Category = new CategoryModel {
                Name = uaNamespace.Category.Name,
                Description = uaNamespace.Category.Description,
                IconUrl = uaNamespace.Category.IconUrl?.ToString(),
            };
            entity.DocumentationUrl = uaNamespace.DocumentationUrl?.ToString();
            entity.IconUrl = uaNamespace.IconUrl?.ToString();
            entity.LicenseUrl = uaNamespace.LicenseUrl?.ToString();
            entity.Keywords = uaNamespace.Keywords;
            entity.PurchasingInformationUrl = uaNamespace.PurchasingInformationUrl?.ToString();
            entity.ReleaseNotesUrl = uaNamespace.ReleaseNotesUrl?.ToString();
            entity.TestSpecificationUrl = uaNamespace.TestSpecificationUrl?.ToString();
            entity.SupportedLocales = uaNamespace.SupportedLocales;
            entity.NumberOfDownloads = uaNamespace.NumberOfDownloads;
            entity.AdditionalProperties = uaNamespace.AdditionalProperties?.Select(p => new AdditionalPropertyModel { NodeSetId = identifier, Name = p.Name, Value = p.Value })?.ToList();
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
            if (model.Contributor == null)
            {
                uaNamespace.Contributor = null;
            }
            else
            {
                MapToOrganisation(uaNamespace.Contributor, model.Contributor);
            }
            uaNamespace.License = model.License;
            uaNamespace.CopyrightText = model.CopyrightText;
            uaNamespace.Description = model.Description;
            if (model.Category == null)
            {
                uaNamespace.Category = null;
            }
            else
            {
                MapToCategory(uaNamespace.Category, model.Category);
            }
            uaNamespace.DocumentationUrl = model.DocumentationUrl != null ? new Uri(model.DocumentationUrl) : null;
            uaNamespace.IconUrl = model.IconUrl != null ? new Uri(model.IconUrl) : null;
            uaNamespace.LicenseUrl = model.LicenseUrl != null ? new Uri(model.LicenseUrl) : null;
            uaNamespace.Keywords = model.Keywords;
            uaNamespace.PurchasingInformationUrl = model.PurchasingInformationUrl != null ? new Uri(model.PurchasingInformationUrl) : null;
            uaNamespace.ReleaseNotesUrl = model.ReleaseNotesUrl != null ? new Uri(model.ReleaseNotesUrl) : null;
            uaNamespace.TestSpecificationUrl = model.TestSpecificationUrl != null ? new Uri(model.TestSpecificationUrl) : null;
            uaNamespace.SupportedLocales = model.SupportedLocales;
            uaNamespace.NumberOfDownloads = model.NumberOfDownloads;
            uaNamespace.AdditionalProperties = model.AdditionalProperties?.Select(p => new UAProperty { Name = p.Name, Value = p.Value })?.ToArray();
        }

        private void MapToCategory(Category category, CategoryModel model)
        {
            category.Name = model.Name;
            category.Description = model.Description;
            category.IconUrl = model.IconUrl != null ? new Uri(model.IconUrl) : null;
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

        private void MapToOrganisation(Organisation org, OrganisationModel model)
        {
            org.Name = model.Name;
            org.Description = model.Description;
            org.ContactEmail = model.ContactEmail;
            org.Website = model.Website != null ? new Uri(model.Website) : null;
            org.LogoUrl = model.LogoUrl != null ? new Uri(model.LogoUrl) : null;
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
