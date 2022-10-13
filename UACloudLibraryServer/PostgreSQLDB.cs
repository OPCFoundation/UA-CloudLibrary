/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
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

namespace Opc.Ua.Cloud.Library
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using HotChocolate.Types.Pagination.Extensions;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Cloud.Library.DbContextModels;
    using Opc.Ua.Cloud.Library.Models;

    public class PostgreSQLDB : IDatabase
    {
        private readonly ILogger _logger;
        private readonly AppDbContext _dbContext;

        public static string CreateConnectionString(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("CloudLibraryPostgreSQL");
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = CreateConnectionStringFromEnvironment();
            }
            return connectionString;
        }

        private static string CreateConnectionStringFromEnvironment()
        {
            // Obtain connection string information from the environment
            string Host = Environment.GetEnvironmentVariable("PostgreSQLEndpoint");
            string User = Environment.GetEnvironmentVariable("PostgreSQLUsername");
            string Password = Environment.GetEnvironmentVariable("PostgreSQLPassword");

            string DBname = "uacloudlib";
            string Port = "5432";

            // Build connection string using parameters from portal
            return $"Server={Host};Username={User};Database={DBname};Port={Port};Password={Password};SSLMode=Prefer";
        }

        public PostgreSQLDB(ILoggerFactory logger, IConfiguration configuration, AppDbContext dbContext)
        {
            _logger = logger.CreateLogger("PostgreSQLDB");
            _dbContext = dbContext;
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
            RetrieveAllMetadata(nodesetId, nameSpace);
            return nameSpace;
        }
        public void RetrieveAllMetadata(uint nodesetId, UANameSpace nameSpace)
        {
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

            nameSpace.Nodeset.Identifier = nodesetId;

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

        public IQueryable<CloudLibNodeSetModel> SearchNodesets(string[] keywords)
        {
            IQueryable<CloudLibNodeSetModel> matchingNodeSets;

            if (keywords != null && keywords[0] != "*")
            {
                string keywordRegex = $".*({string.Join('|', keywords)}).*";

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

    }
}
