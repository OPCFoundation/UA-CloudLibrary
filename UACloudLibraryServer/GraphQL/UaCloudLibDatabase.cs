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
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UACloudLibrary.DbContextModels;

    public class UaCloudLibDatabase : ObjectGraphType
    {
        private AppDbContext _context;
        private IDatabase _database;

        public UaCloudLibDatabase(AppDbContext context, IDatabase database)
        {
            _context = context;
            _database = database;
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

        public Task<List<AddressSpace>> GetAdressSpaceTypes(int limit, int offset)
        {
            string[] names = _database.GetAllNamesAndNodesets();

            // input validation
            if ((offset < 0) || (limit < 0) || (offset > names.Length))
            {
                return Task.FromResult(new List<AddressSpace>());
            }
            if ((offset + limit) > names.Length)
            {
                limit = names.Length - offset;
            }

            List<AddressSpace> result = new List<AddressSpace>();
            for (int i = offset; i < (offset + limit); i++)
            {
                try
                {
                    string[] tuple = names[i].Split(',');
                    if (uint.TryParse(tuple[1], out uint nodesetId))
                    {
                        AddressSpace addressSpace = new AddressSpace();

                        addressSpace.Description = _database.RetrieveMetaData(nodesetId, "description");
                        addressSpace.Version = _database.RetrieveMetaData(nodesetId, "version");
                        addressSpace.Title = _database.RetrieveMetaData(nodesetId, "nodesettitle");
                        addressSpace.CopyrightText = _database.RetrieveMetaData(nodesetId, "copyright");
                        addressSpace.DocumentationUrl = CreateUri(_database.RetrieveMetaData(nodesetId, "documentationurl"));
                        addressSpace.PurchasingInformationUrl = CreateUri(_database.RetrieveMetaData(nodesetId, "purchasinginfo"));
                        addressSpace.IconUrl = CreateUri(_database.RetrieveMetaData(nodesetId, "iconurl"));
                        addressSpace.LicenseUrl = CreateUri(_database.RetrieveMetaData(nodesetId, "licenseurl"));
                        addressSpace.ReleaseNotesUrl = CreateUri(_database.RetrieveMetaData(nodesetId, "releasenotes"));
                        addressSpace.TestSpecificationUrl = CreateUri(_database.RetrieveMetaData(nodesetId, "testspecification"));
                        addressSpace.NumberOfDownloads = Convert.ToUInt32(_database.RetrieveMetaData(nodesetId, "numdownloads"));
                        addressSpace.Keywords = _database.RetrieveMetaData(nodesetId, "keywords").Split(',');
                        addressSpace.SupportedLocales = _database.RetrieveMetaData(nodesetId, "locales").Split(',');
                        switch (_database.RetrieveMetaData(nodesetId, "license"))
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

                        addressSpace.Contributor.ContactEmail = _database.RetrieveMetaData(nodesetId, "orgcontact");
                        addressSpace.Contributor.Name = _database.RetrieveMetaData(nodesetId, "orgname");
                        addressSpace.Contributor.Description = _database.RetrieveMetaData(nodesetId, "orgdescription");
                        addressSpace.Contributor.Website = CreateUri(_database.RetrieveMetaData(nodesetId, "orgwebsite"));
                        addressSpace.Contributor.LogoUrl = CreateUri(_database.RetrieveMetaData(nodesetId, "orglogo"));

                        addressSpace.Category.Name = _database.RetrieveMetaData(nodesetId, "addressspacename");
                        addressSpace.Category.Description = _database.RetrieveMetaData(nodesetId, "addressspacedescription");
                        addressSpace.Category.IconUrl = CreateUri(_database.RetrieveMetaData(nodesetId, "addressspaceiconurl"));

                        result.Add(addressSpace);
                    }
                }
                catch (Exception)
                {
                    // ignore this entity
                }
            }

            return Task.FromResult(result);
        }

        public Task<List<AddressSpaceCategory>> GetCategoryTypes(int limit, int offset)
        {
            string[] names = _database.GetAllNamesAndNodesets();

            // input validation
            if ((offset < 0) || (limit < 0) || (offset > names.Length))
            {
                return Task.FromResult(new List<AddressSpaceCategory>());
            }
            if ((offset + limit) > names.Length)
            {
                limit = names.Length - offset;
            }

            List<AddressSpaceCategory> result = new List<AddressSpaceCategory>();
            for (int i = offset; i < (offset + limit); i++)
            {
                try
                {
                    string[] tuple = names[i].Split(',');
                    if (uint.TryParse(tuple[1], out uint nodesetId))
                    {
                        AddressSpaceCategory category = new AddressSpaceCategory();

                        category.Name = tuple[0];
                        category.Description = _database.RetrieveMetaData(nodesetId, "addressspacedescription");
                        category.IconUrl = CreateUri(_database.RetrieveMetaData(nodesetId, "addressspaceiconurl"));

                        result.Add(category);
                    }
                }
                catch (Exception)
                {
                    // ignire this entity
                }
            }

            return Task.FromResult(result);
        }

        public Task<List<Organisation>> GetOrganisationTypes(int limit, int offset)
        {
            string[] names = _database.GetAllNamesAndNodesets();

            // input validation
            if ((offset < 0) || (limit < 0) || (offset > names.Length))
            {
                return Task.FromResult(new List<Organisation>());
            }
            if ((offset + limit) > names.Length)
            {
                limit = names.Length - offset;
            }

            List<Organisation> result = new List<Organisation>();
            for (int i = offset; i < (offset + limit); i++)
            {
                try
                {
                    string[] tuple = names[i].Split(',');
                    if (uint.TryParse(tuple[1], out uint nodesetId))
                    {
                        Organisation organisation = new Organisation();

                        organisation.ContactEmail = _database.RetrieveMetaData(nodesetId, "orgcontact");
                        organisation.Name = _database.RetrieveMetaData(nodesetId, "orgname");
                        organisation.Description = _database.RetrieveMetaData(nodesetId, "orgdescription");
                        organisation.Website = CreateUri(_database.RetrieveMetaData(nodesetId, "orgwebsite"));
                        organisation.LogoUrl = CreateUri(_database.RetrieveMetaData(nodesetId, "orglogo"));

                        result.Add(organisation);
                    }
                }
                catch (Exception)
                {
                    // ignore this entity
                }
            }

            return Task.FromResult(result);
        }

        private Uri CreateUri(string uri)
        {
            if (!string.IsNullOrEmpty(uri))
            {
                return new Uri(uri);
            }
            else
            {
                return null;
            }
        }
    }
}