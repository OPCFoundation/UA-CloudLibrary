using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CESMII.OpcUa.NodeSetModel;
using Extensions;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.DbContextModels;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    public class CloudLibDataProvider : IDatabase
    {
        private readonly AppDbContext _dbContext = null;
        private readonly UaCloudLibResolver _resolver = null;
        private readonly ILogger _logger;

        public CloudLibDataProvider(AppDbContext context, UaCloudLibResolver resolver, ILoggerFactory logger)
        {
            _dbContext = context;
            _resolver = resolver;
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

        //public Task<List<Models.UANameSpace>> GetNamespaces()
        //{
        //    return GetNamespaces(short.MaxValue, 0, null, null);
        //}

        public Task<List<Models.UANameSpace>> GetNamespaces(int limit, int offset, string where, string orderBy)
        {
            // TODO run as DB query
            return _resolver.GetNameSpaceTypes(limit, offset, where, orderBy);
        }

        public int GetNamespaceTotalCount()
        {
            return _resolver.GetNameSpaceTypesTotalCount();
        }

        //public Task<List<Models.Category>> GetCategories()
        //{
        //    // TODO run as DB query
        //    return _resolver.GetCategoryTypes(short.MaxValue, null, null);
        //}

        public Task<List<Category>> GetCategory(int limit, int offset, string where, string orderBy)
        {
            return _resolver.GetCategoryTypes(limit, where, orderBy);
        }

        public Task<List<MetadataModel>> GetMetadataModel()
        {
            return _resolver.GetMetaData();
        }
        //public UANameSpaceMetadata GetMetadata(uint nodeSetId)
        //{
        //    return RetrieveAllMetadata(nodeSetId);
        //}

        //public Task<List<Models.Organisation>> GetOrganisations()
        //{
        //    // TODO run as DB query
        //    return _resolver.GetOrganisationTypes(short.MaxValue, null, null);
        //}

        public Task<List<Models.Organisation>> GetOrganisation(int limit, int offset, string where, string orderBy)
        {
            return _resolver.GetOrganisationTypes(limit, where, orderBy);
        }

        public bool AddMetaDataToNodeSet(uint nodesetId, string name, string value)
        {
            try
            {
                var metaData = new MetadataModel {
                    NodesetId = nodesetId,
                    Name = name,
                    Value = value,
                };
                _dbContext.Metadata.Add(metaData);
                _dbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return false;
        }

        public bool UpdateMetaDataForNodeSet(uint nodesetId, string name, string value)
        {
            try
            {
                var metaData = _dbContext.Metadata.FirstOrDefault(md => md.NodesetId == nodesetId && md.Name == name);
                if (metaData == null)
                {
                    return false;
                }
                metaData.Value = value;
                _dbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return false;
        }

        public bool DeleteAllRecordsForNodeset(uint nodesetId)
        {
            try
            {
                var recordIds = _dbContext.Metadata.Where(md => md.NodesetId == nodesetId).Select(md => md.Id);
                foreach (var id in recordIds)
                {
                    var mdToDelete = new MetadataModel { Id = id };
                    _dbContext.Entry(mdToDelete).State = EntityState.Deleted;
                }
                _dbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return false;
        }

        public UANameSpace RetrieveAllMetadata(uint nodesetId)
        {
            UANameSpace nameSpace = new();
            try
            {
                var allMetaData = _dbContext.Metadata.Where(md => md.NodesetId == nodesetId).ToList();
                var model = GetNamespaceUriForNodeset(nodesetId);
                ConvertNodeSetMetadata(nodesetId, allMetaData, model, nameSpace);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return nameSpace;
        }
        private static void ConvertNodeSetMetadata(uint nodesetId, List<MetadataModel> metaDataList, CloudLibNodeSetModel model, UANameSpace nameSpace)
        {
            var allMetaData = metaDataList.ToDictionary(md => md.Name, md => md.Value);

            nameSpace.Nodeset.Identifier = nodesetId;

            if (DateTime.TryParse(allMetaData.GetValueOrDefault("nodesetcreationtime"), out DateTime parsedDateTime))
            {
                nameSpace.Nodeset.PublicationDate = parsedDateTime;
            }

            if (DateTime.TryParse(allMetaData.GetValueOrDefault("nodesetmodifiedtime"), out parsedDateTime))
            {
                nameSpace.Nodeset.LastModifiedDate = parsedDateTime;
            }

            nameSpace.Title = allMetaData.GetValueOrDefault("nodesettitle", string.Empty);

            nameSpace.Nodeset.Version = allMetaData.GetValueOrDefault("version", string.Empty);

            if (!string.IsNullOrEmpty(model?.ModelUri))
            {
                nameSpace.Nodeset.NamespaceUri = new Uri(model.ModelUri);
            }
            nameSpace.Nodeset.ValidationStatus = model?.ValidationStatus.ToString();
            if (model?.RequiredModels != null)
            {
                nameSpace.Nodeset.RequiredModels = model?.RequiredModels.Select(rm => {
                    Nodeset availableModel = null;
                    if (rm.AvailableModel != null)
                    {
                        uint? identifier = null;
                        if (uint.TryParse(rm.AvailableModel?.Identifier, out var identifierParsed))
                        {
                            identifier = identifierParsed;
                        }
                        availableModel = new Nodeset {
                            NamespaceUri = new Uri(rm.AvailableModel.ModelUri),
                            PublicationDate = rm.AvailableModel.PublicationDate ?? default,
                            Version = rm.AvailableModel.Version,
                            Identifier = identifier ?? 0,
                        };
                    }
                    var rn = new CloudLibRequiredModelInfo {
                        NamespaceUri = rm.ModelUri,
                        PublicationDate = rm.PublicationDate ?? DateTime.MinValue,
                        Version = rm.Version,
                        AvailableModel = availableModel,
                    };
                    return rn;
                }
                ).ToList();
            }

            switch (allMetaData.GetValueOrDefault("license"))
            {
                case "MIT":
                    nameSpace.License = License.MIT;
                    break;
                case "ApacheLicense20":
                    nameSpace.License = License.ApacheLicense20;
                    break;
                case "Custom":
                    nameSpace.License = License.Custom;
                    break;
                default:
                    nameSpace.License = License.Custom;
                    break;
            }

            nameSpace.CopyrightText = allMetaData.GetValueOrDefault("copyright", string.Empty);

            nameSpace.Description = allMetaData.GetValueOrDefault("description", string.Empty);

            nameSpace.Category.Name = allMetaData.GetValueOrDefault("addressspacename", string.Empty);

            nameSpace.Category.Description = allMetaData.GetValueOrDefault("addressspacedescription", string.Empty);

            var uri = allMetaData.GetValueOrDefault("addressspaceiconurl");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.Category.IconUrl = new Uri(uri);
            }

            uri = allMetaData.GetValueOrDefault("documentationurl");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.DocumentationUrl = new Uri(uri);
            }

            uri = allMetaData.GetValueOrDefault("iconurl");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.IconUrl = new Uri(uri);
            }

            uri = allMetaData.GetValueOrDefault("licenseurl");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.LicenseUrl = new Uri(uri);
            }

            uri = allMetaData.GetValueOrDefault("purchasinginfo");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.PurchasingInformationUrl = new Uri(uri);
            }

            uri = allMetaData.GetValueOrDefault("releasenotes");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.ReleaseNotesUrl = new Uri(uri);
            }

            uri = allMetaData.GetValueOrDefault("testspecification");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.TestSpecificationUrl = new Uri(uri);
            }

            string keywords = allMetaData.GetValueOrDefault("keywords");
            if (!string.IsNullOrEmpty(keywords))
            {
                nameSpace.Keywords = keywords.Split(',');
            }

            string locales = allMetaData.GetValueOrDefault("locales");
            if (!string.IsNullOrEmpty(locales))
            {
                nameSpace.SupportedLocales = locales.Split(',');
            }

            nameSpace.Contributor.Name = allMetaData.GetValueOrDefault("orgname", string.Empty);

            nameSpace.Contributor.Description = allMetaData.GetValueOrDefault("orgdescription", string.Empty);

            uri = allMetaData.GetValueOrDefault("orglogo");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.Contributor.LogoUrl = new Uri(uri);
            }

            nameSpace.Contributor.ContactEmail = allMetaData.GetValueOrDefault("orgcontact", string.Empty);

            uri = allMetaData.GetValueOrDefault("orgwebsite");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.Contributor.Website = new Uri(uri);
            }

            nameSpace.ValidationStatus = allMetaData.GetValueOrDefault("validationstatus", string.Empty);

            if (uint.TryParse(allMetaData.GetValueOrDefault("numdownloads"), out uint parsedDownloads))
            {
                nameSpace.NumberOfDownloads = parsedDownloads;
            }

            var additionalProperties = allMetaData.Where(kv => !_knownProperties.Contains(kv.Key)).ToList();
            if (additionalProperties.Any())
            {
                nameSpace.AdditionalProperties = additionalProperties.Select(p => new UAProperty { Name = p.Key, Value = p.Value }).OrderBy(p => p.Name).ToArray();
            }
        }

        static readonly string[] _knownProperties = new string[] {
            "addressspacedescription", "addressspaceiconurl", "addressspacename", "copyright", "description", "documentationurl", "iconurl",
            "keywords", "license", "licenseurl", "locales", "nodesetcreationtime", "nodesetmodifiedtime", "nodesettitle", "numdownloads",
            "orgcontact", "orgdescription", "orglogo", "orgname", "orgwebsite", "purchasinginfo", "releasenotes", "testspecification", "validationstatus", "version",
            };

        public string RetrieveMetaData(uint nodesetId, string metaDataTag)
        {
            try
            {
                var value = _dbContext.Metadata.Where(md => md.NodesetId == nodesetId)?.Select(md => md.Value)?.FirstOrDefault();
                if (value != null)
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return string.Empty;
        }

        internal IQueryable<CloudLibNodeSetModel> SearchNodesets(string[] keywords)
        {
            IQueryable<CloudLibNodeSetModel> matchingNodeSets;

            if (keywords?.Any() == true && keywords[0] != "*")
            {
                string keywordRegex = $".*({string.Join('|', keywords)}).*";

#pragma warning disable CA1305 // Specify IFormatProvider - ToString() runs in the database, cultureinfo not supported
                matchingNodeSets =
                    _dbContext.nodeSets
                    .Where(nsm =>
                        nsm.ObjectTypes.Any(nm => Regex.IsMatch(nm.DisplayName.FirstOrDefault().Text, keywordRegex, RegexOptions.IgnoreCase))
                        || nsm.Objects.Any(nm => Regex.IsMatch(nm.DisplayName.FirstOrDefault().Text, keywordRegex, RegexOptions.IgnoreCase))
                        || nsm.VariableTypes.Any(nm => Regex.IsMatch(nm.DisplayName.FirstOrDefault().Text, keywordRegex, RegexOptions.IgnoreCase))
                        || nsm.Properties.Any(nm => Regex.IsMatch(nm.DisplayName.FirstOrDefault().Text, keywordRegex, RegexOptions.IgnoreCase))
                        || nsm.DataVariables.Any(nm => Regex.IsMatch(nm.DisplayName.FirstOrDefault().Text, keywordRegex, RegexOptions.IgnoreCase))
                        || nsm.DataTypes.Any(nm => Regex.IsMatch(nm.DisplayName.FirstOrDefault().Text, keywordRegex, RegexOptions.IgnoreCase))
                        || nsm.ReferenceTypes.Any(nm => Regex.IsMatch(nm.DisplayName.FirstOrDefault().Text, keywordRegex, RegexOptions.IgnoreCase))
                        || nsm.Interfaces.Any(nm => Regex.IsMatch(nm.DisplayName.FirstOrDefault().Text, keywordRegex, RegexOptions.IgnoreCase))
                        || nsm.UnknownNodes.Any(nm => Regex.IsMatch(nm.DisplayName.FirstOrDefault().Text, keywordRegex, RegexOptions.IgnoreCase))
                        || _dbContext.Metadata.Any(md => md.NodesetId.ToString() == nsm.Identifier && Regex.IsMatch(md.Value, keywordRegex, RegexOptions.IgnoreCase))
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
            List<UANodesetResult> nodesetResults = new List<UANodesetResult>();

            var nodeSets = SearchNodesets(keywords)
                .OrderBy(n => n.ModelUri)
                .Skip(offset ?? 0)
                .Take(limit ?? 100)
                .ToList();

            var matchesLong = nodeSets.Select(n => long.Parse(n.Identifier, CultureInfo.InvariantCulture)).ToList();
            var metaDataForMatches = _dbContext.Metadata.Where(md => matchesLong.Contains(md.NodesetId)).ToList();

            Dictionary<long, List<MetadataModel>> metaDataForMatchesByNodeSetId = new();
            metaDataForMatches.ForEach(md => {
                if (!metaDataForMatchesByNodeSetId.TryGetValue(md.NodesetId, out var mdList))
                {
                    mdList = new();
                    metaDataForMatchesByNodeSetId[md.NodesetId] = mdList;
                }
                mdList.Add(md);
            });

            //Get additional metadata (if present and valid) for each match
            foreach (var nodeSet in nodeSets)
            {
                UANameSpace nameSpace = new UANameSpace();

                var idStr = /*nodeSetAndMd.N*/nodeSet.Identifier;
                var id = uint.Parse(idStr, CultureInfo.InvariantCulture);
                ConvertNodeSetMetadata(id,
                    metaDataForMatchesByNodeSetId[id],
                    nodeSet,
                    nameSpace);

                var thisResult = new UANodesetResult {
                    Id = id,
                    Title = nameSpace.Title,
                    Contributor = nameSpace.Contributor.Name,
                    License = nameSpace.License.ToString(),
                    Version = nameSpace.Nodeset.Version,
                    ValidationStatus = nameSpace.ValidationStatus,
                    PublicationDate = nameSpace.Nodeset.PublicationDate,
                    NameSpaceUri = nameSpace.Nodeset.NamespaceUri?.ToString(),
                    RequiredNodesets = nameSpace.Nodeset.RequiredModels,

                    CopyrightText = nameSpace.CopyrightText,
                    Description = nameSpace.Description,
                    Category = nameSpace.Category,
                    DocumentationUrl = nameSpace.DocumentationUrl,
                    IconUrl = nameSpace.IconUrl,
                    LicenseUrl = nameSpace.LicenseUrl,
                    Keywords = nameSpace.Keywords,
                    PurchasingInformationUrl = nameSpace.PurchasingInformationUrl,
                    ReleaseNotesUrl = nameSpace.ReleaseNotesUrl,
                    TestSpecificationUrl = nameSpace.TestSpecificationUrl,
                    SupportedLocales = nameSpace.SupportedLocales,
                    NumberOfDownloads = nameSpace.NumberOfDownloads,
                    AdditionalProperties = nameSpace.AdditionalProperties,
                };

                nodesetResults.Add(thisResult);
            }
            return nodesetResults.ToArray();
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

        private CloudLibNodeSetModel GetNamespaceUriForNodeset(uint nodesetId)
        {
            try
            {
                var identifier = nodesetId.ToString(CultureInfo.InvariantCulture);
                var model = _dbContext.nodeSets.FirstOrDefault(nsm => nsm.Identifier == identifier);
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
                var nameSpaceUriAndId = _dbContext.Metadata.Where(md => md.Name == "addressspacename").Select(md => new { NamespaceUri = md.Value, md.NodesetId }).ToList();
                return nameSpaceUriAndId.Select(ni => $"{ni.NamespaceUri},{ni.NodesetId}").ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Array.Empty<string>();
        }



#if !NOLEGACY
        #region legacy

        public IQueryable<QueryModel.NodeSetGraphQLLegacy> GetNodeSet()
        {
            return _dbContext.nodeSets.AsQueryable().Select(nsm => new QueryModel.NodeSetGraphQLLegacy {
                Identifier = uint.Parse(nsm.Identifier, CultureInfo.InvariantCulture),
                NamespaceUri = nsm.ModelUri,
                Version = nsm.Version,
                PublicationDate = nsm.PublicationDate ?? default,
                LastModifiedDate = default, // TODO
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
