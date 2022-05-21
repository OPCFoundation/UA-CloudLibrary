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

                    Dictionary<string, MetadataModel> metadataForNodeset = _context.Metadata.Where(p => p.NodesetId == nodesetIds[i]).ToDictionary(x => x.Name);

                    if (metadataForNodeset.ContainsKey("nodesetcreationtime"))
                    {
                        if (DateTime.TryParse(metadataForNodeset["nodesetcreationtime"].Value, out DateTime parsedDateTime))
                        {
                            nameSpace.Nodeset.PublicationDate = parsedDateTime;
                        }
                    }

                    if (metadataForNodeset.ContainsKey("nodesetmodifiedtime"))
                    {
                        if (DateTime.TryParse(metadataForNodeset["nodesetmodifiedtime"].Value, out DateTime parsedDateTime))
                        {
                            nameSpace.Nodeset.LastModifiedDate = parsedDateTime;
                        }
                    }

                    if (metadataForNodeset.ContainsKey("nodesettitle"))
                    {
                        nameSpace.Title = metadataForNodeset["nodesettitle"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("version"))
                    {
                        nameSpace.Nodeset.Version = metadataForNodeset["version"].Value;
                    }

                    nameSpace.Nodeset.Identifier = (uint)nodesetIds[i];

                    var nodeSetIdentifier = nodesetIds[i].ToString(CultureInfo.InvariantCulture);
                    var modelUri = (await _context.nodeSets.Where(nsm => nsm.Identifier == nodeSetIdentifier).FirstOrDefaultAsync().ConfigureAwait(false))?.ModelUri;
                    nameSpace.Nodeset.NamespaceUri = new Uri(modelUri);

                    if (metadataForNodeset.ContainsKey("license"))
                    {
                        switch (metadataForNodeset["license"].Value)
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
                    }

                    if (metadataForNodeset.ContainsKey("copyright"))
                    {
                        nameSpace.CopyrightText = metadataForNodeset["copyright"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("description"))
                    {
                        nameSpace.Description = metadataForNodeset["description"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("addressspacename"))
                    {
                        nameSpace.Category.Name = metadataForNodeset["addressspacename"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("addressspacedescription"))
                    {
                        nameSpace.Category.Description = metadataForNodeset["addressspacedescription"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("addressspaceiconurl"))
                    {
                        string uri = metadataForNodeset["addressspaceiconurl"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            nameSpace.Category.IconUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("documentationurl"))
                    {
                        string uri = metadataForNodeset["documentationurl"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            nameSpace.DocumentationUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("iconurl"))
                    {
                        string uri = metadataForNodeset["iconurl"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            nameSpace.IconUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("licenseurl"))
                    {
                        string uri = metadataForNodeset["licenseurl"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            nameSpace.LicenseUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("purchasinginfo"))
                    {
                        string uri = metadataForNodeset["purchasinginfo"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            nameSpace.PurchasingInformationUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("releasenotes"))
                    {
                        string uri = metadataForNodeset["releasenotes"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            nameSpace.ReleaseNotesUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("testspecification"))
                    {
                        string uri = metadataForNodeset["testspecification"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            nameSpace.TestSpecificationUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("keywords"))
                    {
                        string keywords = metadataForNodeset["keywords"].Value;
                        if (!string.IsNullOrEmpty(keywords))
                        {
                            nameSpace.Keywords = keywords.Split(',');
                        }
                    }

                    if (metadataForNodeset.ContainsKey("locales"))
                    {
                        string locales = metadataForNodeset["locales"].Value;
                        if (!string.IsNullOrEmpty(locales))
                        {
                            nameSpace.SupportedLocales = locales.Split(',');
                        }
                    }

                    if (metadataForNodeset.ContainsKey("orgname"))
                    {
                        nameSpace.Contributor.Name = metadataForNodeset["orgname"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("orgdescription"))
                    {
                        nameSpace.Contributor.Description = metadataForNodeset["orgdescription"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("orglogo"))
                    {
                        string uri = metadataForNodeset["orglogo"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            nameSpace.Contributor.LogoUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("orgcontact"))
                    {
                        nameSpace.Contributor.ContactEmail = metadataForNodeset["orgcontact"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("orgwebsite"))
                    {
                        string uri = metadataForNodeset["orgwebsite"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            nameSpace.Contributor.Website = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("validationstatus"))
                    {
                        nameSpace.ValidationStatus = metadataForNodeset["validationstatus"].Value;
                    }
                    if (metadataForNodeset.ContainsKey("numdownloads"))
                    {
                        if (uint.TryParse(metadataForNodeset["numdownloads"].Value, out uint parsedDownloads))
                        {
                            nameSpace.NumberOfDownloads = parsedDownloads;
                        }
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
