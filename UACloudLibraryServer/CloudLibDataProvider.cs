using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CESMII.OpcUa.NodeSetModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using NpgsqlTypes;
using Opc.Ua.Cloud.Library.DbContextModels;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Export;

namespace Opc.Ua.Cloud.Library
{
    public partial class CloudLibDataProvider : IDatabase
    {
        private readonly AppDbContext _dbContext = null;
        private readonly ILogger _logger;

        public CloudLibDataProvider(AppDbContext context, ILoggerFactory logger)
        {
            _dbContext = context;
            _logger = logger.CreateLogger("CloudLibDataProvider");

        }

        public IQueryable<CloudLibNodeSetModel> GetNodeSets(
            string identifier = null,
            string nodeSetUrl = null,
            DateTime? publicationDate = null,
            string[] keywords = null)
        {

            IQueryable<CloudLibNodeSetModel> nodeSets;
            if (!string.IsNullOrEmpty(identifier))
            {
                if (nodeSetUrl != null || publicationDate != null || keywords != null)
                {
                    throw new ArgumentException($"Must not specify other parameters when providing identifier.");
                }
                nodeSets = _dbContext.nodeSets.AsQueryable().Where(nsm => nsm.Identifier == identifier);
            }
            else
            {
                var nodeSetQuery = SearchNodesets(keywords);
                if (nodeSetUrl != null && publicationDate != null)
                {
                    nodeSets = nodeSetQuery.Where(nsm => nsm.ModelUri == nodeSetUrl && nsm.PublicationDate == publicationDate);
                }
                else if (nodeSetUrl != null)
                {
                    nodeSets = nodeSetQuery.Where(nsm => nsm.ModelUri == nodeSetUrl);
                }
                else
                {
                    nodeSets = nodeSetQuery;
                }
            }
            return nodeSets;
        }

        public IQueryable<ObjectTypeModel> GetObjectTypes(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<ObjectTypeModel>(nsm => nsm.ObjectTypes, nodeSetUrl, publicationDate, nodeId);
        }

        public IQueryable<VariableTypeModel> GetVariableTypes(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<VariableTypeModel>(nsm => nsm.VariableTypes, nodeSetUrl, publicationDate, nodeId);
        }

        public IQueryable<DataTypeModel> GetDataTypes(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<DataTypeModel>(nsm => nsm.DataTypes, nodeSetUrl, publicationDate, nodeId);
        }

        public IQueryable<PropertyModel> GetProperties(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<PropertyModel>(nsm => nsm.Properties, nodeSetUrl, publicationDate, nodeId);
        }

        public IQueryable<DataVariableModel> GetDataVariables(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<DataVariableModel>(nsm => nsm.DataVariables, nodeSetUrl, publicationDate, nodeId);
        }

        public IQueryable<ReferenceTypeModel> GetReferenceTypes(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<ReferenceTypeModel>(nsm => nsm.ReferenceTypes, nodeSetUrl, publicationDate, nodeId);
        }

        public IQueryable<InterfaceModel> GetInterfaces(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<InterfaceModel>(nsm => nsm.Interfaces, nodeSetUrl, publicationDate, nodeId);
        }

        public IQueryable<ObjectModel> GetObjects(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<ObjectModel>(nsm => nsm.Objects, nodeSetUrl, publicationDate, nodeId);
        }


        public IQueryable<NodeModel> GetAllNodes(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            if (nodeId != null && nodeSetUrl == null)
            {
                var expandedNodeId = ExpandedNodeId.Parse(nodeId);
                if (expandedNodeId?.NamespaceUri != null)
                {
                    nodeSetUrl = expandedNodeId.NamespaceUri;
                }
            }

            IQueryable<NodeModel> nodeModels;
            if (nodeSetUrl != null && publicationDate != null)
            {
                nodeModels = _dbContext.nodeModels.AsQueryable().Where(nm => nm.Namespace == nodeSetUrl && nm.NodeSet.PublicationDate == publicationDate);
            }
            else if (nodeSetUrl != null)
            {
                nodeModels = _dbContext.nodeModels.AsQueryable().Where(nm => nm.Namespace == nodeSetUrl);
            }
            else
            {
                nodeModels = _dbContext.nodeModels.AsQueryable();
            }
            if (!string.IsNullOrEmpty(nodeId))
            {
                nodeModels = nodeModels.Where(ot => ot.NodeId == nodeId);
            }

            return nodeModels;
        }

        private IQueryable<T> GetNodeModels<T>(Expression<Func<CloudLibNodeSetModel, IEnumerable<T>>> selector, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
            where T : NodeModel
        {
            if (nodeId != null && nodeSetUrl == null)
            {
                var expandedNodeId = ExpandedNodeId.Parse(nodeId);
                if (expandedNodeId?.NamespaceUri != null)
                {
                    nodeSetUrl = expandedNodeId.NamespaceUri;
                }
            }

            IQueryable<CloudLibNodeSetModel> nodeSets;
            if (nodeSetUrl != null && publicationDate != null)
            {
                nodeSets = _dbContext.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == nodeSetUrl && nsm.PublicationDate == publicationDate);
            }
            else if (nodeSetUrl != null)
            {
                nodeSets = _dbContext.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == nodeSetUrl);
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

                    var nodeSetModel = await NodeSetModelIndexer.CreateNodeSetModelFromNodeSetAsync(_dbContext, nodeSet, uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture), userId).ConfigureAwait(false);
                    var existingModel = await _dbContext.nodeSetsWithUnapproved.FindAsync(nodeSetModel.ModelUri, nodeSetModel.PublicationDate).ConfigureAwait(false);
                    if (existingModel != null)
                    {
                        message = "Error: nodeset still exists after delete.";
                        return;
                    }
                    else
                    {
                        await _dbContext.nodeSetsWithUnapproved.AddAsync(nodeSetModel).ConfigureAwait(false);
                    }

                    // TODO validate that user has permission for this organisation
                    OrganisationModel contributor = uaNamespace.Contributor.Name != null ?
                        await _dbContext.Organisations.FirstOrDefaultAsync(c => c.Name == uaNamespace.Contributor.Name).ConfigureAwait(false)
                        : null;
                    CategoryModel category = uaNamespace.Category.Name != null ?
                        await _dbContext.Categories.FirstOrDefaultAsync(c => c.Name == uaNamespace.Category.Name).ConfigureAwait(false)
                        : null;

                    var nameSpaceModel = new NamespaceMetaDataModel();
                    MapToEntity(ref nameSpaceModel, uaNamespace, nodeSetModel, contributor, category);

                    nodeSetModel.ValidationStatus = ValidationStatus.Parsed;
                    nodeSetModel.ValidationStatusInfo = null;
                    nameSpaceModel.UserId = userId;

                    await _dbContext.AddAsync(nameSpaceModel).ConfigureAwait(false);

                    await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                },
                // This will only run on failures during transaction commit, where the EF can not determine if the Tx was committed or not
                () => _dbContext.nodeSetsWithUnapproved.AsNoTracking()
                    .AnyAsync(n => n.ModelUri == nodeSet.Models[0].ModelUri && n.PublicationDate == (nodeSet.Models[0].PublicationDateSpecified ? nodeSet.Models[0].PublicationDate : default))
                );
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
            var namespaceMeta = await _dbContext.NamespaceMetaData.FirstOrDefaultAsync(n => n.NodesetId == nodesetId.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
            namespaceMeta.NumberOfDownloads++;
            var newCount = namespaceMeta.NumberOfDownloads;
            await _dbContext.SaveChangesAsync();
            return newCount;
        }

        public async Task<bool> DeleteAllRecordsForNodesetAsync(uint nodesetId)
        {
            try
            {
                var nodesetIdStr = nodesetId.ToString(CultureInfo.InvariantCulture);
                List<CloudLibNodeSetModel> deletedNodeSets = new();
                await DeleteNodeSetIndexForNodesetAsync(nodesetIdStr, deletedNodeSets).ConfigureAwait(false);

                var namespaceModel = await _dbContext.NamespaceMetaDataWithUnapproved.FirstOrDefaultAsync(n => n.NodesetId == nodesetIdStr).ConfigureAwait(false);
                if (namespaceModel != null)
                {
                    _dbContext.NamespaceMetaDataWithUnapproved.Remove(namespaceModel);
                }

                await _dbContext.SaveChangesAsync().ConfigureAwait(false);

                // Re-add nodesets that were removed because they depend on the nodeset being deleted
                // This will cause them to be reindexed later on
                foreach (var deletedNodeSet in deletedNodeSets)
                {
                    if (deletedNodeSet.Identifier != nodesetIdStr)
                    {
                        var nodeSetModel = NodeSetModelIndexer.CreateNodeSetModelFromNodeSet(deletedNodeSet);
                        await _dbContext.nodeSetsWithUnapproved.AddAsync(nodeSetModel).ConfigureAwait(false);
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
            var nodeSetModel = await _dbContext.nodeSetsWithUnapproved.FirstOrDefaultAsync(n => n.Identifier == nodesetId).ConfigureAwait(false);
            if (nodeSetModel != null)
            {
                foreach (var dependentNodeset in _dbContext.nodeSetsWithUnapproved
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
                var namespaceModel = await _dbContext.NamespaceMetaData
                    .Where(md => md.NodesetId == nodesetId.ToString())
                    .Include(md => md.NodeSet)
                    .FirstOrDefaultAsync().ConfigureAwait(false);
                if (namespaceModel == null)
                {
                    return null;
                }
                MapToNamespace(nameSpace, namespaceModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return nameSpace;
        }

        internal IQueryable<CloudLibNodeSetModel> SearchNodesets(string[] keywords)
        {
            IQueryable<CloudLibNodeSetModel> matchingNodeSets;

            if (keywords?.Any() == true && keywords[0] != "*")
            {
                string keywordRegex = $".*({string.Join('|', keywords)}).*";
                string keywordTsQueryText = string.Join(" | ",
                    keywords
                    .Select(k => $"'{Regex.Replace(k, "\\d", "").Trim(' ')}'")
                    ); // PostgreSql text search doesn't like numbers: remove them for now
                var keywordTsQuery = NpgsqlTsQuery.Parse(keywordTsQueryText);
#pragma warning disable CA1305 // Specify IFormatProvider - ToString() runs in the database, cultureinfo not supported
                matchingNodeSets =
                    _dbContext.nodeSets
                    .Where(nsm =>
                        _dbContext.NamespaceMetaData.Any(md =>
                            md.NodesetId == nsm.Identifier
                            && Regex.IsMatch(md.Title + md.Description, keywordRegex, RegexOptions.IgnoreCase)
                            // Fulltext appears to be slower than regex: && EF.Functions.ToTsVector("english", md.Title + " || " + md.Description/* + " " + string.Join(' ', md.Keywords) + md.Category.Name + md.Contributor.Name*/).Matches(keywordTsQuery))
                            )

                        || _dbContext.nodeModels.Any(nm => nm.NodeSet.Identifier == nsm.Identifier && Regex.IsMatch(nm.BrowseName, keywordRegex, RegexOptions.IgnoreCase))
                        // Fulltext appears to be slower than regex: || _dbContext.nodeModels.Any(nm => nm.NodeSet.Identifier == nsm.Identifier && EF.Functions.ToTsVector("english", nm.BrowseName).Matches(keywordTsQuery))
                        // Displayname is localized and this query is slower, even with an index. We could add a DefaultDisplayName to speed this up, if BrowseName ends up being incorrect
                        //|| _dbContext.nodeModels.Any(nm => nm.NodeSet.Identifier == nsm.Identifier && Regex.IsMatch(nm.DisplayName.FirstOrDefault().Text, keywordRegex, RegexOptions.IgnoreCase))
                        );
#pragma warning restore CA1305 // Specify IFormatProvider
            }
            else
            {
                matchingNodeSets = _dbContext.nodeSets.AsQueryable();
            }
            return matchingNodeSets;
        }

        public UANodesetResult[] FindNodesets(string[] keywords, int? offset, int? limit)
        {
            var uaNamespaceModel = SearchNodesets(keywords)
                .OrderBy(n => n.ModelUri)
                .Skip(offset ?? 0)
                .Take(limit ?? 100)
                .Select(n => _dbContext.NamespaceMetaData.Where(nmd => nmd.NodesetId == n.Identifier).Include(nmd => nmd.NodeSet).FirstOrDefault())
                .ToList();

            var nodesetResults = uaNamespaceModel.Select(nameSpace => {
                var result = new UANodesetResult();
                MapToNamespace(result, nameSpace);

                return result;
            }).ToArray();
            return nodesetResults;
        }


        public string[] GetAllNamespacesAndNodesets()
        {
            try
            {
                var namesAndIds = _dbContext.nodeSets.Select(nsm => new { nsm.ModelUri, nsm.Identifier }).Select(n => $"{n.ModelUri},{n.Identifier}").ToArray();
                return namesAndIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Array.Empty<string>();
        }

        private CloudLibNodeSetModel GetNamespaceUriForNodeset(string nodesetId)
        {
            try
            {
                var model = _dbContext.nodeSets.FirstOrDefault(nsm => nsm.Identifier == nodesetId);
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return null;
        }

        public string[] GetAllNamesAndNodesets()
        {
            try
            {
                var categoryAndNodesetIds = _dbContext.NamespaceMetaData.Select(md => new { md.Category.Name, md.NodesetId }).ToList();
                var namesAndNodesetsString = categoryAndNodesetIds.Select(cn => $"{cn.Name},{cn.NodesetId}").ToArray();
                return namesAndNodesetsString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Array.Empty<string>();
        }


        public IQueryable<NamespaceMetaDataModel> GetNamespaces()
        {
            return _dbContext.NamespaceMetaData
                .Include(md => md.Category)
                .Include(md => md.Contributor)
                .Include(md => md.NodeSet);
        }
        public int GetNamespaceTotalCount()
        {
            return _dbContext.NamespaceMetaData.Count();
        }

        public async Task<UANameSpace> ApproveNamespaceAsync(string identifier, ApprovalStatus status, string approvalInformation)
        {
            var nodeSet = await _dbContext.NamespaceMetaDataWithUnapproved.Where(n => n.NodesetId == identifier).FirstOrDefaultAsync();
            if (nodeSet == null) return null;

            _dbContext.Attach(nodeSet);
            nodeSet.ApprovalStatus = status;
            nodeSet.ApprovalInformation = approvalInformation;
            await _dbContext.SaveChangesAsync();
            return await RetrieveAllMetadataAsync(uint.Parse(identifier, CultureInfo.InvariantCulture)).ConfigureAwait(false);
        }

        public IQueryable<CloudLibNodeSetModel> GetNodeSetsPendingApproval()
        {
            return _dbContext.nodeSetsWithUnapproved.Where(n => _dbContext.NamespaceMetaDataWithUnapproved.Any(nmd => nmd.NodesetId == n.Identifier && nmd.ApprovalStatus != ApprovalStatus.Approved));
        }

        public IQueryable<CategoryModel> GetCategories()
        {
            return _dbContext.Categories;
        }

        public IQueryable<OrganisationModel> GetOrganisations()
        {
            return _dbContext.Organisations.AsQueryable();
        }

        private void MapToEntity(ref NamespaceMetaDataModel entity, UANameSpace uaNamespace, CloudLibNodeSetModel nodeSetModel, OrganisationModel contributor, CategoryModel category)
        {
            var identifier = nodeSetModel != null ? nodeSetModel.Identifier : uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture);
            entity.NodesetId = identifier;
            entity.NodeSet = nodeSetModel;
            entity.Title = uaNamespace.Title;
            entity.ContributorId = contributor?.Id ?? 0;
            entity.Contributor = contributor ?? new OrganisationModel {
                Name = uaNamespace.Contributor.Name,
                Description = uaNamespace.Contributor.Description,
                ContactEmail = uaNamespace.Contributor.ContactEmail,
                LogoUrl = uaNamespace.Contributor.LogoUrl?.ToString(),
                Website = uaNamespace.Contributor.Website?.ToString(),
            };
            var licenseExpression = uaNamespace.License switch {
                "0" => "MIT",
                "1" or "ApacheLicense20" => "Apache-2.0",
                "2" => "Custom",
                _ => uaNamespace.License,
            };
            // TODO Validate license Expression
            entity.License = licenseExpression;
            entity.CopyrightText = uaNamespace.CopyrightText;
            entity.Description = uaNamespace.Description;
            entity.CategoryId = category?.Id ?? 0;
            entity.Category = category ?? new CategoryModel {
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
            uaNamespace.Title = model.Title;
            MapToOrganisation(uaNamespace.Contributor, model.Contributor);
            uaNamespace.License = model.License;
            uaNamespace.CopyrightText = model.CopyrightText;
            uaNamespace.Description = model.Description;
            MapToCategory(uaNamespace.Category, model.Category);
            uaNamespace.DocumentationUrl = model.DocumentationUrl != null ? new Uri(model.DocumentationUrl) : null;
            uaNamespace.IconUrl = model.IconUrl != null ? new Uri(model.IconUrl) : null;
            uaNamespace.LicenseUrl = model.LicenseUrl != null ? new Uri(model.LicenseUrl) : null;
            uaNamespace.Keywords = model.Keywords;
            uaNamespace.PurchasingInformationUrl = model.PurchasingInformationUrl != null ? new Uri(model.PurchasingInformationUrl) : null;
            uaNamespace.ReleaseNotesUrl = model.ReleaseNotesUrl != null ? new Uri(model.ReleaseNotesUrl) : null;
            uaNamespace.TestSpecificationUrl = model.TestSpecificationUrl != null ? new Uri(model.TestSpecificationUrl) : null;
            uaNamespace.SupportedLocales = model.SupportedLocales;
            uaNamespace.NumberOfDownloads = model.NumberOfDownloads;
            uaNamespace.AdditionalProperties = model.AdditionalProperties.Select(p => new UAProperty { Name = p.Name, Value = p.Value }).ToArray();
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
                return new CloudLibRequiredModelInfo { NamespaceUri = rm.ModelUri, PublicationDate = rm.PublicationDate, Version = rm.Version, AvailableModel = availableNodeSet };
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

#if !NOLEGACY
        #region legacy

        public IQueryable<MetadataModel> GetMetadataModel()
        {
            // TODO retrieve well-known properties from NamespaceMetaDataMoel
            return _dbContext.Metadata.AsQueryable();
        }


        public IQueryable<QueryModel.NodeSetGraphQLLegacy> GetNodeSet()
        {
            return _dbContext.nodeSets.AsQueryable().Select(nsm => new QueryModel.NodeSetGraphQLLegacy {
                Identifier = uint.Parse(nsm.Identifier, CultureInfo.InvariantCulture),
                NamespaceUri = nsm.ModelUri,
                Version = nsm.Version,
                PublicationDate = nsm.PublicationDate ?? default,
                LastModifiedDate = nsm.LastModifiedDate ?? default,
            });
        }

        public IQueryable<ObjecttypeModel> GetObjectType()
        {
            var objectTypes = GetNodeModels<ObjectTypeModel>(nsm => nsm.ObjectTypes).Select(ot => new ObjecttypeModel {
                BrowseName = ot.BrowseName,
                NameSpace = ot.Namespace,
                NodesetId = long.Parse(ot.NodeSet.Identifier, CultureInfo.InvariantCulture),
                Id = ot.NodeId.GetDeterministicHashCode(),
                Value = ot.DisplayName.FirstOrDefault().Text,
            });
            return objectTypes;
        }

        public IQueryable<DatatypeModel> GetDataType()
        {
            var dataTypes = GetNodeModels<DataTypeModel>(nsm => nsm.DataTypes).Select(dt => new DatatypeModel {
                BrowseName = dt.BrowseName,
                NameSpace = dt.Namespace,
                NodesetId = long.Parse(dt.NodeSet.Identifier, CultureInfo.InvariantCulture),
                Id = dt.NodeId.GetDeterministicHashCode(),
                Value = dt.DisplayName.FirstOrDefault().Text,
            });
            return dataTypes;
        }
        public IQueryable<ReferencetypeModel> GetReferenceType()
        {
            var referenceTypes = GetNodeModels<ReferenceTypeModel>(nsm => nsm.ReferenceTypes).Select(rt => new ReferencetypeModel {
                BrowseName = rt.BrowseName,
                NameSpace = rt.Namespace,
                NodesetId = long.Parse(rt.NodeSet.Identifier, CultureInfo.InvariantCulture),
                Id = rt.NodeId.GetDeterministicHashCode(),
                Value = rt.DisplayName.FirstOrDefault().Text,
            });
            return referenceTypes;
        }
        public IQueryable<VariabletypeModel> GetVariableType()
        {
            var referenceTypes = GetNodeModels<VariableTypeModel>(nsm => nsm.VariableTypes).Select(vt => new VariabletypeModel {
                BrowseName = vt.BrowseName,
                NameSpace = vt.Namespace,
                NodesetId = long.Parse(vt.NodeSet.Identifier, CultureInfo.InvariantCulture),
                Id = vt.NodeId.GetDeterministicHashCode(),
                Value = vt.DisplayName.FirstOrDefault().Text,
            });
            return referenceTypes;
        }
        #endregion
#endif // legacy
    }
}
