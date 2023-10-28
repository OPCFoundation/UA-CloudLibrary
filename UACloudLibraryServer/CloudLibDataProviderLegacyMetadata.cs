using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua.Cloud.Library.Controllers;
using Opc.Ua.Cloud.Library.DbContextModels;
using Opc.Ua.Cloud.Library.Interfaces;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    public partial class CloudLibDataProvider : IDatabase
    {
        internal async Task MigrateLegacyMetadataAsync(IFileStorage storage)
        {
            if (!await _dbContext.LegacyMetadata.AnyAsync().ConfigureAwait(false))
            {
                _logger.LogInformation($"No legacy metadata found to migrate.");
                return;
            }
            if ((await _dbContext.LegacyMetadata.AnyAsync(md => md.NodesetId == -1).ConfigureAwait(false)))
            {
                _logger.LogInformation($"Found legacy metadata migration ran once to completion. Remove metadata table entry with NodeSetId = -1 to run again.");
                return;
            }
            // Migrate any legacy meta data
            _logger.LogInformation($"Starting legacy metadata migration.");

            bool legacyMigrationError = false;
#pragma warning disable CA1305 // Specify IFormatProvider - .ToString(0 runs in DB
            var missingLegacyMetaDataIds = await _dbContext.LegacyMetadata
                .Select(md => md.NodesetId)
                .Distinct()
                .Where(id => !_dbContext.NamespaceMetaDataWithUnapproved.Any(nmd => nmd.NodesetId == id.ToString()))
                .ToListAsync().ConfigureAwait(false);
#pragma warning restore CA1305 // Specify IFormatProvider
            _logger.LogInformation($"Migrating legacy metadata for {missingLegacyMetaDataIds.Count} nodesets.");
            foreach (var legacyNodesetId in missingLegacyMetaDataIds)
            {
                var uaNamespace = await RetrieveAllLegacyMetadataAsync((uint)legacyNodesetId).ConfigureAwait(false);
                if (uaNamespace == null)
                {
                    legacyMigrationError = true;
                    _logger.LogError($"Legacy Metadata for nodeset id {legacyNodesetId} could not be read.");
                    continue;
                }
                try
                {
                    _logger.LogDebug($"Downloading nodeset for legacy metadata migration {legacyNodesetId}");
                    var nodeSetXml = await storage.DownloadFileAsync(legacyNodesetId.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);

                    if (nodeSetXml != null)
                    {
                        _logger.LogDebug($"Parsing missing nodeset for legacy metadata migration {legacyNodesetId}");
                        var uaNodeSet = InfoModelController.ReadUANodeSet(nodeSetXml);
                        var message = await AddMetaDataAsync(uaNamespace, uaNodeSet, 0, null).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(message))
                        {
                            legacyMigrationError = true;
                            _logger.LogError($"Error while migrating legacy metadata for nodeset {legacyNodesetId}: {message}");
                        }
                        _logger.LogInformation($"Migrated legacy metadata for {legacyNodesetId} - {uaNodeSet?.Models?.FirstOrDefault()?.ModelUri} {uaNodeSet?.Models?.FirstOrDefault()?.Version} {uaNodeSet?.Models?.FirstOrDefault()?.PublicationDate}.");
                    }
                    else
                    {
                        legacyMigrationError = true;
                        _logger.LogError($"Error while migrating legacy nodeset {legacyNodesetId}: nodeset XML not found in storage.");
                    }
                }
                catch (Exception ex)
                {
                    legacyMigrationError = true;
                    _logger.LogError(ex, $"Error while migrating legacy metadata for  nodeset {legacyNodesetId}");
                }
            }
            if (!legacyMigrationError)
            {
                // Add a dummy entry (with nodesetid -1) to the metadata table so we can check later if migration ran to completion
                var migrateSucceeded = new MetadataModel { NodesetId = -1, Name = "Migrated", Value = $"{missingLegacyMetaDataIds.Count}: {DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}" };
                await _dbContext.LegacyMetadata.AddAsync(migrateSucceeded).ConfigureAwait(false);
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<UANameSpace> RetrieveAllLegacyMetadataAsync(uint nodesetId)
        {
            UANameSpace nameSpace = null;
            try
            {
                nameSpace = new();
                var allMetaData = await _dbContext.LegacyMetadata.Where(md => md.NodesetId == nodesetId).ToListAsync().ConfigureAwait(false);
                var model = GetNamespaceUriForNodeset(nodesetId.ToString(CultureInfo.InvariantCulture));
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

            if (DateTime.TryParse(allMetaData.GetValueOrDefault("creationtime"), out parsedDateTime))
            {
                nameSpace.CreationTime = parsedDateTime;
            }

            nameSpace.Title = allMetaData.GetValueOrDefault("nodesettitle", string.Empty);

            nameSpace.Nodeset.Version = allMetaData.GetValueOrDefault("version", string.Empty);

            if (!string.IsNullOrEmpty(model?.ModelUri))
            {
                nameSpace.Nodeset.NamespaceUri = new Uri(model.ModelUri);
            }
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
                            ValidationStatus = (rm.AvailableModel as CloudLibNodeSetModel)?.ValidationStatus.ToString(),
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
                    nameSpace.License = License.MIT.ToString();
                    break;
                case "ApacheLicense20":
                    nameSpace.License = License.ApacheLicense20.ToString();
                    break;
                case "Custom":
                    nameSpace.License = License.Custom.ToString();
                    break;
                default:
                    nameSpace.License = License.Custom.ToString();
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

            nameSpace.Nodeset.ValidationStatus = allMetaData.GetValueOrDefault("validationstatus", string.Empty);

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
            "addressspacedescription", "addressspaceiconurl", "addressspacename", "copyright", "creationtime", "description", "documentationurl", "iconurl",
            "keywords", "license", "licenseurl", "locales", "nodesetcreationtime", "nodesetmodifiedtime", "nodesettitle", "numdownloads",
            "orgcontact", "orgdescription", "orglogo", "orgname", "orgwebsite", "purchasinginfo", "releasenotes", "testspecification", "validationstatus", "version",
            };

        static Dictionary<string, string> legacyFilterNames = new Dictionary<string, string> {
            // TODO add all filter name maps
            // TODO Potentially support different mappings for each type
            { "nodesetTitle", "Title" },
        };

        private Expression<Func<TModel, bool>> ApplyWhereExpression<TModel>(string where)
        {
            //List<long> nodesetIds = null;

            List<string> fields = new List<string>();
            List<string> comparions = new List<string>();
            List<string> values = new List<string>();

            Expression expressionSoFar = null;

            // check if there was a where expression/filter specified
            if (!string.IsNullOrEmpty(where))
            {
                // parse where expression
                try
                {
                    JArray whereExpression = (JArray)JsonConvert.DeserializeObject(where);
                    foreach (JObject clause in whereExpression)
                    {
                        fields.Add(((JProperty)clause.First).Name);
                        comparions.Add(((JProperty)((JObject)clause.First.First).First).Name);
                        values.Add(((JValue)((JObject)clause.First.First).First.First).Value.ToString());
                    }
                }
                catch (Exception)
                {
                    // do nothing
                }

                // apply where expression
                var paramExp = Expression.Parameter(typeof(TModel), typeof(TModel).Name);
                if ((fields.Count > 0) && (fields.Count == comparions.Count) && (fields.Count == values.Count))
                {
                    //nodesetIds = new List<long>();

                    for (int i = 0; i < fields.Count; i++)
                    {
                        // map search fields to internal names
                        if (fields[i] == "publicationDate")
                        {
                            fields[i] = "nodesetcreationtime";
                        }

                        if (fields[i] == "lastModified")
                        {
                            fields[i] = "nodesetmodifiedtime";
                        }

                        if (legacyFilterNames.TryGetValue(fields[i], out var newName))
                        {
                            fields[i] = newName;
                        }

                        // do the search
                        var propMemberInfo = typeof(TModel).GetProperty(fields[i]);
                        if (propMemberInfo == null)
                        {
                            // Field not found: fail
                            return null;
                        }
                        var memberExp = Expression.MakeMemberAccess(paramExp, propMemberInfo);
                        var valueExp = Expression.Constant(values[i]);
                        Expression comparison = null;
                        if (comparions[i] == "equals")
                        {
                            comparison = Expression.Equal(memberExp, valueExp);
                            //List<MetadataModel> results = _dbContext.Metadata.Where(p => (p.Name == fields[i]) && (p.Value == values[i])).ToList();
                            //nodesetIds.AddRange(results.Select(p => p.NodesetId).Distinct().ToList());
                        }
                        else if (comparions[i] == "contains")
                        {
                            comparison = Expression.Equal(memberExp, valueExp);
                            //List<MetadataModel> results = _dbContext.Metadata.Where(p => (p.Name == fields[i]) && p.Value.Contains(values[i])).ToList();
                            //nodesetIds.AddRange(results.Select(p => p.NodesetId).Distinct().ToList());
                        }
                        else if (comparions[i] == "like")
                        {
                            comparison = Expression.Equal(memberExp, valueExp);

                            //#pragma warning disable CA1304 // Specify CultureInfo
                            //                            List<MetadataModel> results = _dbContext.Metadata.Where(p => (p.Name == fields[i]) && p.Value.ToLower().Contains(values[i].ToLower())).ToList();
                            //#pragma warning restore CA1304 // Specify CultureInfo
                            //                            nodesetIds.AddRange(results.Select(p => p.NodesetId).Distinct().ToList());
                        }
                        if (comparison != null)
                        {
                            if (expressionSoFar == null)
                            {
                                expressionSoFar = comparison;
                            }
                            else
                            {
                                expressionSoFar = Expression.Or(expressionSoFar, comparison);
                            }
                        }
                    }
                    var expr = Expression.Lambda<Func<TModel, bool>>(expressionSoFar, paramExp);
                    return expr;
                }
                else
                {
                    // where expression was invalid, return empty list
                    //nodesetIds = new List<long>();
                    return null;
                }
            }
            else
            {
                // where expression was not specified, so get all distinct nodeset IDs
                return /*Expression.Lambda<Func<TModel, bool>>*/(TModel x) => true; //Expression.Constant(true)
                //nodesetIds = _dbContext.Metadata.Select(p => p.NodesetId).Distinct().ToList();
            }

            //return nodesetIds;
        }
    }
}
