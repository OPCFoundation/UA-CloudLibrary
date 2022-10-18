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
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Opc.Ua.Cloud.Library.DbContextModels;
    using Opc.Ua.Cloud.Library.Models;

    public class UaCloudLibResolver
    {
        private AppDbContext _context;

        public UaCloudLibResolver(AppDbContext context, IDatabase database)
        {
            _context = context;
        }

        public Task<List<MetadataModel>> GetMetaData()
        {
            return _context.Metadata.ToListAsync();
        }

        private List<long> ApplyWhereExpression(string where)
        {
            List<long> nodesetIds = null;

            List<string> fields = new List<string>();
            List<string> comparions = new List<string>();
            List<string> values = new List<string>();

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
                if ((fields.Count > 0) && (fields.Count == comparions.Count) && (fields.Count == values.Count))
                {
                    nodesetIds = new List<long>();

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

                        // do the search
                        if (comparions[i] == "equals")
                        {
                            List<MetadataModel> results = _context.Metadata.Where(p => (p.Name == fields[i]) && (p.Value == values[i])).ToList();
                            nodesetIds.AddRange(results.Select(p => p.NodesetId).Distinct().ToList());
                        }

                        if (comparions[i] == "contains")
                        {
                            List<MetadataModel> results = _context.Metadata.Where(p => (p.Name == fields[i]) && p.Value.Contains(values[i])).ToList();
                            nodesetIds.AddRange(results.Select(p => p.NodesetId).Distinct().ToList());
                        }

                        if (comparions[i] == "like")
                        {
#pragma warning disable CA1304 // Specify CultureInfo
                            List<MetadataModel> results = _context.Metadata.Where(p => (p.Name == fields[i]) && p.Value.ToLower().Contains(values[i].ToLower())).ToList();
#pragma warning restore CA1304 // Specify CultureInfo
                            nodesetIds.AddRange(results.Select(p => p.NodesetId).Distinct().ToList());
                        }
                    }
                }
                else
                {
                    // where expression was invalid, return empty list
                    nodesetIds = new List<long>();
                }
            }
            else
            {
                // where expression was not specified, so get all distinct nodeset IDs
                nodesetIds = _context.Metadata.Select(p => p.NodesetId).Distinct().ToList();
            }

            return nodesetIds;
        }

        public int GetNameSpaceTypesTotalCount(string where)
        {
            var count = ApplyWhereExpression(where).Count;
            return count;
        }

        public async Task<List<UANameSpace>> GetNameSpaceTypes(int limit, int offset, string where, string orderBy)
        {
            List<long> nodesetIds = ApplyWhereExpression(where);

            // input validation
            if ((offset < 0) || (limit < 0) || (offset > nodesetIds.Count))
            {
                return new List<UANameSpace>();
            }
            if ((offset + limit) > nodesetIds.Count)
            {
                limit = nodesetIds.Count - offset;
            }

            List<UANameSpace> result = new List<UANameSpace>();

            for (int i = offset; i < (offset + limit); i++)
            {
                try
                {
                    UANameSpace nameSpace = new UANameSpace();

                    var additionalProperties = new List<UAProperty>();
                    nameSpace.Nodeset.Identifier = (uint)nodesetIds[i];

                    var nodeSetIdentifier = nodesetIds[i].ToString(CultureInfo.InvariantCulture);
                    var nodeSet = (await _context.nodeSets.Where(nsm => nsm.Identifier == nodeSetIdentifier).FirstOrDefaultAsync().ConfigureAwait(false));
                    var modelUri = nodeSet?.ModelUri;
                    var requiredModels = nodeSet?.RequiredModels;
                    nameSpace.Nodeset.NamespaceUri = new Uri(modelUri);
                    nameSpace.Nodeset.RequiredModels = requiredModels?.Select(rm => new CloudLibRequiredModelInfo {
                        NamespaceUri = rm.ModelUri,
                        PublicationDate = rm.PublicationDate,
                        Version = rm.Version,
                        AvailableModel = new Nodeset {
                            NamespaceUri = new Uri(rm.AvailableModel.ModelUri),
                            PublicationDate = rm.AvailableModel.PublicationDate ?? default,
                            Version = rm.AvailableModel.Version,
                            Identifier = uint.Parse(rm.AvailableModel.Identifier, CultureInfo.InvariantCulture),
                        }
                    })?.ToList();

                    var metadataListForNodeset = _context.Metadata.Where(p => p.NodesetId == nodesetIds[i]).ToList();

                    foreach (var metadataForNodeset in metadataListForNodeset)
                    {
                        switch (metadataForNodeset.Name)
                        {
                            case "nodesetcreationtime":
                            {
                                if (DateTime.TryParse(metadataForNodeset.Value, out DateTime parsedDateTime))
                                {
                                    nameSpace.Nodeset.PublicationDate = parsedDateTime;
                                }
                                break;
                            }
                            case "nodesetmodifiedtime":
                            {
                                if (DateTime.TryParse(metadataForNodeset.Value, out DateTime parsedDateTime))
                                {
                                    nameSpace.Nodeset.LastModifiedDate = parsedDateTime;
                                }
                                break;
                            }
                            case "nodesettitle":
                            {
                                nameSpace.Title = metadataForNodeset.Value;
                                break;
                            }
                            case "version":
                            {
                                nameSpace.Nodeset.Version = metadataForNodeset.Value;
                                break;
                            }
                            case "license":
                            {
                                switch (metadataForNodeset.Value)
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
                                break;
                            }
                            case "copyright":
                                nameSpace.CopyrightText = metadataForNodeset.Value;
                                break;
                            case "description":
                                nameSpace.Description = metadataForNodeset.Value;
                                break;
                            case "addressspacename":
                                nameSpace.Category.Name = metadataForNodeset.Value;
                                break;
                            case "addressspacedescription":
                                nameSpace.Category.Description = metadataForNodeset.Value;
                                break;
                            case "addressspaceiconurl":
                            {
                                string uri = metadataForNodeset.Value;
                                if (!string.IsNullOrEmpty(uri))
                                {
                                    nameSpace.Category.IconUrl = new Uri(uri);
                                }
                                break;
                            }
                            case "documentationurl":
                            {
                                string uri = metadataForNodeset.Value;
                                if (!string.IsNullOrEmpty(uri))
                                {
                                    nameSpace.DocumentationUrl = new Uri(uri);
                                }
                                break;
                            }
                            case "iconurl":
                            {
                                string uri = metadataForNodeset.Value;
                                if (!string.IsNullOrEmpty(uri))
                                {
                                    nameSpace.IconUrl = new Uri(uri);
                                }
                                break;
                            }
                            case "licenseurl":
                            {
                                string uri = metadataForNodeset.Value;
                                if (!string.IsNullOrEmpty(uri))
                                {
                                    nameSpace.LicenseUrl = new Uri(uri);
                                }
                                break;
                            }
                            case "purchasinginfo":
                            {
                                string uri = metadataForNodeset.Value;
                                if (!string.IsNullOrEmpty(uri))
                                {
                                    nameSpace.PurchasingInformationUrl = new Uri(uri);
                                }
                                break;
                            }
                            case "releasenotes":
                            {
                                string uri = metadataForNodeset.Value;
                                if (!string.IsNullOrEmpty(uri))
                                {
                                    nameSpace.ReleaseNotesUrl = new Uri(uri);
                                }
                                break;
                            }
                            case "testspecification":
                            {
                                string uri = metadataForNodeset.Value;
                                if (!string.IsNullOrEmpty(uri))
                                {
                                    nameSpace.TestSpecificationUrl = new Uri(uri);
                                }
                                break;
                            }
                            case "keywords":
                            {
                                string keywords = metadataForNodeset.Value;
                                if (!string.IsNullOrEmpty(keywords))
                                {
                                    nameSpace.Keywords = keywords.Split(',');
                                }
                                break;
                            }
                            case "locales":
                            {
                                string locales = metadataForNodeset.Value;
                                if (!string.IsNullOrEmpty(locales))
                                {
                                    nameSpace.SupportedLocales = locales.Split(',');
                                }
                                break;
                            }
                            case "orgname":
                                nameSpace.Contributor.Name = metadataForNodeset.Value;
                                break;
                            case "orgdescription":
                                nameSpace.Contributor.Description = metadataForNodeset.Value;
                                break;
                            case "orglogo":
                            {
                                string uri = metadataForNodeset.Value;
                                if (!string.IsNullOrEmpty(uri))
                                {
                                    nameSpace.Contributor.LogoUrl = new Uri(uri);
                                }
                                break;
                            }
                            case "orgcontact":
                                nameSpace.Contributor.ContactEmail = metadataForNodeset.Value;
                                break;
                            case "orgwebsite":
                            {
                                string uri = metadataForNodeset.Value;
                                if (!string.IsNullOrEmpty(uri))
                                {
                                    nameSpace.Contributor.Website = new Uri(uri);
                                }
                                break;
                            }
                            case "validationstatus":
                                nameSpace.ValidationStatus = metadataForNodeset.Value;
                                nameSpace.Nodeset.ValidationStatus = metadataForNodeset.Value;
                                break;
                            case "numdownloads":
                            {
                                if (uint.TryParse(metadataForNodeset.Value, out uint parsedDownloads))
                                {
                                    nameSpace.NumberOfDownloads = parsedDownloads;
                                }
                                break;
                            }
                            default:
                                additionalProperties.Add(new UAProperty { Name = metadataForNodeset.Name, Value = metadataForNodeset.Value });
                                break;
                        }
                    }
                    if (additionalProperties.Any())
                    {
                        nameSpace.AdditionalProperties = additionalProperties.ToArray();
                    }
                    result.Add(nameSpace);
                }
                catch (Exception)
                {
                    // ignore this entity
                }
            }

            if (string.IsNullOrEmpty(orderBy))
            {
                // return unordered list
                return result;
            }
            else
            {
                // return odered list
                return result.OrderByDescending(p => p, new NameSpaceComparer(orderBy)).ToList();
            }
        }

        public Task<List<Category>> GetCategoryTypes(int limit, string where, string orderBy)
        {
            List<long> nodesetIds = ApplyWhereExpression(where);

            List<Category> result = new List<Category>();

            for (int i = 0; (i < nodesetIds.Count) && (result.Count < limit); i++)
            {
                try
                {
                    Category category = new Category();

                    Dictionary<string, MetadataModel> metadataForNodeset = _context.Metadata.Where(p => p.NodesetId == nodesetIds[i]).ToDictionary(x => x.Name);

                    if (metadataForNodeset.ContainsKey("addressspacename"))
                    {
                        category.Name = metadataForNodeset["addressspacename"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("addressspacedescription"))
                    {
                        category.Description = metadataForNodeset["addressspacedescription"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("addressspaceiconurl"))
                    {
                        string uri = metadataForNodeset["addressspaceiconurl"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            category.IconUrl = new Uri(uri);
                        }
                    }

                    if (!result.Contains(category))
                    {
                        result.Add(category);
                    }
                }
                catch (Exception)
                {
                    // ignore this entity
                }
            }

            if (string.IsNullOrEmpty(orderBy))
            {
                // return unordered list
                return Task.FromResult(result);
            }
            else
            {
                // return odered list
                return Task.FromResult(result.OrderByDescending(p => p, new NameSpaceCategoryComparer(orderBy)).ToList());
            }
        }

        public Task<List<Organisation>> GetOrganisationTypes(int limit, string where, string orderBy)
        {
            List<long> nodesetIds = ApplyWhereExpression(where);

            List<Organisation> result = new List<Organisation>();

            for (int i = 0; (i < nodesetIds.Count) && (result.Count < limit); i++)
            {
                try
                {
                    Organisation organisation = new Organisation();

                    Dictionary<string, MetadataModel> metadataForNodeset = _context.Metadata.Where(p => p.NodesetId == nodesetIds[i]).ToDictionary(x => x.Name);

                    if (metadataForNodeset.ContainsKey("orgname"))
                    {
                        organisation.Name = metadataForNodeset["orgname"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("orgdescription"))
                    {
                        organisation.Description = metadataForNodeset["orgdescription"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("orglogo"))
                    {
                        string uri = metadataForNodeset["orglogo"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            organisation.LogoUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("orgcontact"))
                    {
                        organisation.ContactEmail = metadataForNodeset["orgcontact"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("orgwebsite"))
                    {
                        string uri = metadataForNodeset["orgwebsite"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            organisation.Website = new Uri(uri);
                        }
                    }

                    if (!result.Contains(organisation))
                    {
                        result.Add(organisation);
                    }
                }
                catch (Exception)
                {
                    // ignore this entity
                }
            }

            if (string.IsNullOrEmpty(orderBy))
            {
                // return unordered list
                return Task.FromResult(result);
            }
            else
            {
                // return odered list
                return Task.FromResult(result.OrderByDescending(p => p, new OrganisationComparer(orderBy)).ToList());
            }
        }
    }
}
