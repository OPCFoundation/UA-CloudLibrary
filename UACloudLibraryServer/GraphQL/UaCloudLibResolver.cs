﻿/* ========================================================================
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

namespace UACloudLibrary
{
    using GraphQL.Types;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using UACloudLibrary.DbContextModels;
    using UACloudLibrary.Models;

    public class UaCloudLibResolver : ObjectGraphType
    {
        private AppDbContext _context;

        public UaCloudLibResolver(AppDbContext context, IDatabase database)
        {
            _context = context;
        }

        public Task<List<DatatypeModel>> GetDataTypes()
        {
            return _context.DataType.ToListAsync();
        }

        public Task<List<MetadataModel>> GetMetaData()
        {
            return _context.Metadata.ToListAsync();
        }

        public Task<List<ObjecttypeModel>> GetObjectTypes()
        {
            return _context.ObjectType.ToListAsync();
        }

        public Task<List<ReferencetypeModel>> GetReferenceTypes()
        {
            return _context.ReferenceType.ToListAsync();
        }

        public Task<List<Nodeset>> GetNodesetTypes()
        {
            List<Nodeset> result = new List<Nodeset>();

            List<long> nodesetIds = _context.Metadata.Select(p => p.NodesetId).Distinct().ToList();

            for (int i = 0; i < nodesetIds.Count; i++)
            {
                try
                {
                    Nodeset nodeset = new Nodeset();

                    Dictionary<string, MetadataModel> metadataForNodeset = _context.Metadata.Where(p => p.NodesetId == nodesetIds[i]).ToDictionary(x => x.Name);

                    if (metadataForNodeset.ContainsKey("nodesetcreationtime"))
                    {
                        if (DateTime.TryParse(metadataForNodeset["nodesetcreationtime"].Value, out DateTime parsedDateTime))
                        {
                            nodeset.PublicationDate = parsedDateTime;
                        }
                    }

                    if (metadataForNodeset.ContainsKey("nodesetmodifiedtime"))
                    {
                        if (DateTime.TryParse(metadataForNodeset["nodesetmodifiedtime"].Value, out DateTime parsedDateTime))
                        {
                            nodeset.LastModifiedDate = parsedDateTime;
                        }
                    }

                    if (metadataForNodeset.ContainsKey("version"))
                    {
                        nodeset.Version = metadataForNodeset["version"].Value;
                    }

                    nodeset.Identifier = (uint)nodesetIds[i];

                    List<ObjecttypeModel> objectTypesForNodeset = _context.ObjectType.Where(p => p.NodesetId == nodesetIds[i]).ToList();
                    if (objectTypesForNodeset.Count > 0)
                    {
                        nodeset.NamespaceUri = new Uri(objectTypesForNodeset[0].NameSpace);
                    }

                    result.Add(nodeset);
                }
                catch (Exception)
                {
                    // ignore this entity
                }
            }

            return Task.FromResult(result);
        }

        public Task<List<VariabletypeModel>> GetVariableTypes()
        {   
            return _context.VariableType.ToListAsync();
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
                            List<MetadataModel> results = _context.Metadata.Where(p => (p.Name == fields[i]) && p.Value.ToLower().Contains(values[i].ToLower())).ToList();
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

        public Task<List<AddressSpace>> GetAdressSpaceTypes(int limit, int offset, string where, string orderBy)
        {
            List<long> nodesetIds = ApplyWhereExpression(where);

            // input validation
            if ((offset < 0) || (limit < 0) || (offset > nodesetIds.Count))
            {
                return Task.FromResult(new List<AddressSpace>());
            }
            if ((offset + limit) > nodesetIds.Count)
            {
                limit = nodesetIds.Count - offset;
            }

            List<AddressSpace> result = new List<AddressSpace>();

            for (int i = offset; i < (offset + limit); i++)
            {
                try
                {
                    AddressSpace addressSpace = new AddressSpace();

                    Dictionary<string, MetadataModel> metadataForNodeset = _context.Metadata.Where(p => p.NodesetId == nodesetIds[i]).ToDictionary(x => x.Name);

                    if (metadataForNodeset.ContainsKey("nodesetcreationtime"))
                    {
                        if (DateTime.TryParse(metadataForNodeset["nodesetcreationtime"].Value, out DateTime parsedDateTime))
                        {
                            addressSpace.Nodeset.PublicationDate = parsedDateTime;
                        }
                    }

                    if (metadataForNodeset.ContainsKey("nodesetmodifiedtime"))
                    {
                        if (DateTime.TryParse(metadataForNodeset["nodesetmodifiedtime"].Value, out DateTime parsedDateTime))
                        {
                            addressSpace.Nodeset.LastModifiedDate = parsedDateTime;
                        }
                    }

                    if (metadataForNodeset.ContainsKey("nodesettitle"))
                    {
                        addressSpace.Title = metadataForNodeset["nodesettitle"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("version"))
                    {
                        addressSpace.Nodeset.Version = metadataForNodeset["version"].Value;
                    }

                    addressSpace.Nodeset.Identifier = (uint)nodesetIds[i];

                    List<ObjecttypeModel> objectTypesForNodeset = _context.ObjectType.Where(p => p.NodesetId == nodesetIds[i]).ToList();
                    if (objectTypesForNodeset.Count > 0)
                    {
                        addressSpace.Nodeset.NamespaceUri = new Uri(objectTypesForNodeset[0].NameSpace);
                    }

                    if (metadataForNodeset.ContainsKey("license"))
                    {
                        switch (metadataForNodeset["license"].Value)
                        {
                            case "MIT":
                                addressSpace.License = License.MIT;
                                break;
                            case "ApacheLicense20":
                                addressSpace.License = License.ApacheLicense20;
                                break;
                            case "Custom":
                                addressSpace.License = License.Custom;
                                break;
                            default:
                                addressSpace.License = License.Custom;
                                break;
                        }
                    }

                    if (metadataForNodeset.ContainsKey("copyright"))
                    {
                        addressSpace.CopyrightText = metadataForNodeset["copyright"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("description"))
                    {
                        addressSpace.Description = metadataForNodeset["description"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("addressspacename"))
                    {
                        addressSpace.Category.Name = metadataForNodeset["addressspacename"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("addressspacedescription"))
                    {
                        addressSpace.Category.Description = metadataForNodeset["addressspacedescription"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("addressspaceiconurl"))
                    {
                        string uri = metadataForNodeset["addressspaceiconurl"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.Category.IconUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("documentationurl"))
                    {
                        string uri = metadataForNodeset["documentationurl"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.DocumentationUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("iconurl"))
                    {
                        string uri = metadataForNodeset["iconurl"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.IconUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("licenseurl"))
                    {
                        string uri = metadataForNodeset["licenseurl"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.LicenseUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("purchasinginfo"))
                    {
                        string uri = metadataForNodeset["purchasinginfo"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.PurchasingInformationUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("releasenotes"))
                    {
                        string uri = metadataForNodeset["releasenotes"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.ReleaseNotesUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("testspecification"))
                    {
                        string uri = metadataForNodeset["testspecification"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.TestSpecificationUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("keywords"))
                    {
                        string keywords = metadataForNodeset["keywords"].Value;
                        if (!string.IsNullOrEmpty(keywords))
                        {
                            addressSpace.Keywords = keywords.Split(',');
                        }
                    }

                    if (metadataForNodeset.ContainsKey("locales"))
                    {
                        string locales = metadataForNodeset["locales"].Value;
                        if (!string.IsNullOrEmpty(locales))
                        {
                            addressSpace.SupportedLocales = locales.Split(',');
                        }
                    }

                    if (metadataForNodeset.ContainsKey("orgname"))
                    {
                        addressSpace.Contributor.Name = metadataForNodeset["orgname"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("orgdescription"))
                    {
                        addressSpace.Contributor.Description = metadataForNodeset["orgdescription"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("orglogo"))
                    {
                        string uri = metadataForNodeset["orglogo"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.Contributor.LogoUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("orgcontact"))
                    {
                        addressSpace.Contributor.ContactEmail = metadataForNodeset["orgcontact"].Value;
                    }

                    if (metadataForNodeset.ContainsKey("orgwebsite"))
                    {
                        string uri = metadataForNodeset["orgwebsite"].Value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.Contributor.Website = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("numdownloads"))
                    {
                        if (uint.TryParse(metadataForNodeset["numdownloads"].Value, out uint parsedDownloads))
                        {
                            addressSpace.NumberOfDownloads = parsedDownloads;
                        }
                    }

                    result.Add(addressSpace);
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
                return Task.FromResult(result.OrderByDescending(p => p, new AddressSpaceComparer(orderBy)).ToList());
            }
        }

        public Task<List<Category>> GetCategoryTypes(int limit, int offset, string where, string orderBy)
        {
            List<long> nodesetIds = ApplyWhereExpression(where);

            // input validation
            if ((offset < 0) || (limit < 0) || (offset > nodesetIds.Count))
            {
                return Task.FromResult(new List<Category>());
            }
            if ((offset + limit) > nodesetIds.Count)
            {
                limit = nodesetIds.Count - offset;
            }

            List<Category> result = new List<Category>();

            for (int i = offset; i < (offset + limit); i++)
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

                    result.Add(category);
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
                return Task.FromResult(result.OrderByDescending(p => p, new AddressSpaceCategoryComparer(orderBy)).ToList());
            }
        }

        public Task<List<Organisation>> GetOrganisationTypes(int limit, int offset, string where, string orderBy)
        {
            List<long> nodesetIds = ApplyWhereExpression(where);

            // input validation
            if ((offset < 0) || (limit < 0) || (offset > nodesetIds.Count))
            {
                return Task.FromResult(new List<Organisation>());
            }
            if ((offset + limit) > nodesetIds.Count)
            {
                limit = nodesetIds.Count - offset;
            }

            List<Organisation> result = new List<Organisation>();

            for (int i = offset; i < (offset + limit); i++)
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

                    result.Add(organisation);
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
