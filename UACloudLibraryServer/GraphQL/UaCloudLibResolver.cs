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

namespace UACloudLibrary
{
    using GraphQL.Types;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using UACloudLibrary.DbContextModels;

    public class UaCloudLibResolver : ObjectGraphType
    {
        private AppDbContext _context;

        public UaCloudLibResolver(AppDbContext context, IDatabase database)
        {
            _context = context;
        }

        public Task<List<DatatypeModel>> GetDataTypes()
        {
            return _context.datatype.ToListAsync();
        }

        public Task<List<MetadataModel>> GetMetaData()
        {
            return _context.metadata.ToListAsync();
        }

        public Task<List<ObjecttypeModel>> GetObjectTypes()
        {
            return _context.objecttype.ToListAsync();
        }

        public Task<List<ReferencetypeModel>> GetReferenceTypes()
        {
            return _context.referencetype.ToListAsync();
        }

        public Task<List<VariabletypeModel>> GetVariableTypes()
        {   
            return _context.variabletype.ToListAsync();
        }

        public Task<List<AddressSpace>> GetAdressSpaceTypes(int limit, int offset, object where)
        {
            List<long> nodesetIds = _context.metadata.Select(p => p.nodeset_id).Distinct().ToList();

            // input validation
            if ((offset < 0) || (limit < 0) || (offset > nodesetIds.Count))
            {
                return Task.FromResult(new List<AddressSpace>());
            }
            if ((offset + limit) > nodesetIds.Count)
            {
                limit = nodesetIds.Count - offset;
            }

            string whereExpression;
            try
            {
                whereExpression = JsonConvert.SerializeObject(where);
            }
            catch (Exception)
            {
                whereExpression = null;
            }

            List<AddressSpace> result = new List<AddressSpace>();

            for (int i = offset; i < (offset + limit); i++)
            {
                try
                {
                    AddressSpace addressSpace = new AddressSpace();

                    Dictionary<string, MetadataModel> metadataForNodeset = _context.metadata.Where(p => p.nodeset_id == nodesetIds[i]).ToDictionary(x => x.metadata_name);
                        
                    if (metadataForNodeset.ContainsKey("nodesetcreationtime"))
                    {
                        if (DateTime.TryParse(metadataForNodeset["nodesetcreationtime"].metadata_value, out DateTime parsedDateTime))
                        {
                            addressSpace.Nodeset.PublicationDate = parsedDateTime;
                        }
                    }

                    if (metadataForNodeset.ContainsKey("nodesetmodifiedtime"))
                    {
                        if (DateTime.TryParse(metadataForNodeset["nodesetmodifiedtime"].metadata_value, out DateTime parsedDateTime))
                        {
                            addressSpace.Nodeset.LastModifiedDate = parsedDateTime;
                        }
                    }

                    if (metadataForNodeset.ContainsKey("nodesettitle"))
                    {
                        addressSpace.Title = metadataForNodeset["nodesettitle"].metadata_value;
                    }

                    if (metadataForNodeset.ContainsKey("version"))
                    {
                        addressSpace.Version = metadataForNodeset["version"].metadata_value;
                    }

                    if (metadataForNodeset.ContainsKey("license"))
                    {
                        switch (metadataForNodeset["license"].metadata_value)
                        {
                            case "MIT":
                                addressSpace.License = AddressSpaceLicense.MIT;
                                break;
                            case "ApacheLicense20":
                                addressSpace.License = AddressSpaceLicense.ApacheLicense20;
                                break;
                            case "Custom":
                                addressSpace.License = AddressSpaceLicense.Custom;
                                break;
                            default:
                                addressSpace.License = AddressSpaceLicense.Custom;
                                break;
                        }
                    }

                    if (metadataForNodeset.ContainsKey("copyright"))
                    {
                        addressSpace.CopyrightText = metadataForNodeset["copyright"].metadata_value;
                    }

                    if (metadataForNodeset.ContainsKey("description"))
                    {
                        addressSpace.Description = metadataForNodeset["description"].metadata_value;
                    }

                    if (metadataForNodeset.ContainsKey("addressspacename"))
                    {
                        addressSpace.Category.Name = metadataForNodeset["addressspacename"].metadata_value;
                    }

                    if (metadataForNodeset.ContainsKey("addressspacedescription"))
                    {
                        addressSpace.Category.Description = metadataForNodeset["addressspacedescription"].metadata_value;
                    }

                    if (metadataForNodeset.ContainsKey("addressspaceiconurl"))
                    {
                        string uri = metadataForNodeset["addressspaceiconurl"].metadata_value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.Category.IconUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("documentationurl"))
                    {
                        string uri = metadataForNodeset["documentationurl"].metadata_value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.DocumentationUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("iconurl"))
                    {
                        string uri = metadataForNodeset["iconurl"].metadata_value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.IconUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("licenseurl"))
                    {
                        string uri = metadataForNodeset["licenseurl"].metadata_value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.LicenseUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("purchasinginfo"))
                    {
                        string uri = metadataForNodeset["purchasinginfo"].metadata_value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.PurchasingInformationUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("releasenotes"))
                    {
                        string uri = metadataForNodeset["releasenotes"].metadata_value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.ReleaseNotesUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("testspecification"))
                    {
                        string uri = metadataForNodeset["testspecification"].metadata_value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.TestSpecificationUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("keywords"))
                    {
                        string keywords = metadataForNodeset["keywords"].metadata_value;
                        if (!string.IsNullOrEmpty(keywords))
                        {
                            addressSpace.Keywords = keywords.Split(',');
                        }
                    }

                    if (metadataForNodeset.ContainsKey("locales"))
                    {
                        string locales = metadataForNodeset["locales"].metadata_value;
                        if (!string.IsNullOrEmpty(locales))
                        {
                            addressSpace.SupportedLocales = locales.Split(',');
                        }
                    }

                    if (metadataForNodeset.ContainsKey("orgname"))
                    {
                        addressSpace.Contributor.Name = metadataForNodeset["orgname"].metadata_value;
                    }

                    if (metadataForNodeset.ContainsKey("orgdescription"))
                    {
                        addressSpace.Contributor.Description = metadataForNodeset["orgdescription"].metadata_value;
                    }

                    if (metadataForNodeset.ContainsKey("orglogo"))
                    {
                        string uri = metadataForNodeset["orglogo"].metadata_value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.Contributor.LogoUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("orgcontact"))
                    {
                        addressSpace.Contributor.ContactEmail = metadataForNodeset["orgcontact"].metadata_value;
                    }

                    if (metadataForNodeset.ContainsKey("orgwebsite"))
                    {
                        string uri = metadataForNodeset["orgwebsite"].metadata_value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            addressSpace.Contributor.Website = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("numdownloads"))
                    {
                        if (uint.TryParse(metadataForNodeset["numdownloads"].metadata_value, out uint parsedDownloads))
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

            return Task.FromResult(result);
        }

        public Task<List<AddressSpaceCategory>> GetCategoryTypes(int limit, int offset, object where)
        {
            List<long> nodesetIds = _context.metadata.Select(p => p.nodeset_id).Distinct().ToList();

            // input validation
            if ((offset < 0) || (limit < 0) || (offset > nodesetIds.Count))
            {
                return Task.FromResult(new List<AddressSpaceCategory>());
            }
            if ((offset + limit) > nodesetIds.Count)
            {
                limit = nodesetIds.Count - offset;
            }

            List<AddressSpaceCategory> result = new List<AddressSpaceCategory>();

            for (int i = offset; i < (offset + limit); i++)
            {
                try
                {
                    AddressSpaceCategory category = new AddressSpaceCategory();

                    Dictionary<string, MetadataModel> metadataForNodeset = _context.metadata.Where(p => p.nodeset_id == nodesetIds[i]).ToDictionary(x => x.metadata_name);

                    if (metadataForNodeset.ContainsKey("addressspacename"))
                    {
                        category.Name = metadataForNodeset["addressspacename"].metadata_value;
                    }

                    if (metadataForNodeset.ContainsKey("addressspacedescription"))
                    {
                        category.Description = metadataForNodeset["addressspacedescription"].metadata_value;
                    }

                    if (metadataForNodeset.ContainsKey("addressspaceiconurl"))
                    {
                        string uri = metadataForNodeset["addressspaceiconurl"].metadata_value;
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

            return Task.FromResult(result);
        }

        public Task<List<Organisation>> GetOrganisationTypes(int limit, int offset, object where)
        {
            List<long> nodesetIds = _context.metadata.Select(p => p.nodeset_id).Distinct().ToList();

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

                    Dictionary<string, MetadataModel> metadataForNodeset = _context.metadata.Where(p => p.nodeset_id == nodesetIds[i]).ToDictionary(x => x.metadata_name);

                    if (metadataForNodeset.ContainsKey("orgname"))
                    {
                        organisation.Name = metadataForNodeset["orgname"].metadata_value;
                    }

                    if (metadataForNodeset.ContainsKey("orgdescription"))
                    {
                        organisation.Description = metadataForNodeset["orgdescription"].metadata_value;
                    }

                    if (metadataForNodeset.ContainsKey("orglogo"))
                    {
                        string uri = metadataForNodeset["orglogo"].metadata_value;
                        if (!string.IsNullOrEmpty(uri))
                        {
                            organisation.LogoUrl = new Uri(uri);
                        }
                    }

                    if (metadataForNodeset.ContainsKey("orgcontact"))
                    {
                        organisation.ContactEmail = metadataForNodeset["orgcontact"].metadata_value;
                    }

                    if (metadataForNodeset.ContainsKey("orgwebsite"))
                    {
                        string uri = metadataForNodeset["orgwebsite"].metadata_value;
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

            return Task.FromResult(result);
        }
    }
}
