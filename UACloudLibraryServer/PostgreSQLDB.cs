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
    using Microsoft.Extensions.Logging;
    using Npgsql;
    using NpgsqlTypes;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using UACloudLibrary.Models;

    public class PostgreSQLDB : IDatabase
    {
        private NpgsqlConnection _connection = null;
        private readonly ILogger _logger;

        public static string CreateConnectionString()
        {
            // Obtain connection string information from the environment
            string Host = Environment.GetEnvironmentVariable("PostgreSQLEndpoint");
            string User = Environment.GetEnvironmentVariable("PostgreSQLUsername");
            string Password = Environment.GetEnvironmentVariable("PostgreSQLPassword");

            string DBname = "uacloudlib";
            string Port = "5432";

            // Build connection string using parameters from portal
            return string.Format(
                "Server={0};Username={1};Database={2};Port={3};Password={4};SSLMode=Prefer",
                Host,
                User,
                DBname,
                Port,
                Password);
        }

        public PostgreSQLDB(ILoggerFactory logger)
        {
            _logger = logger.CreateLogger("PostgreSQLDB");

            // Setup the database tables
            string[] dbInitCommands = {
                "CREATE TABLE IF NOT EXISTS Metadata(Metadata_id serial PRIMARY KEY, Nodeset_id BIGINT, Metadata_Name TEXT, Metadata_Value TEXT)",
                "CREATE TABLE IF NOT EXISTS ObjectType(ObjectType_id serial PRIMARY KEY, Nodeset_id BIGINT, ObjectType_BrowseName TEXT, ObjectType_Value TEXT, ObjectType_Namespace TEXT)",
                "CREATE TABLE IF NOT EXISTS VariableType(VariableType_id serial PRIMARY KEY, Nodeset_id BIGINT, VariableType_BrowseName TEXT, VariableType_Value TEXT, VariableType_Namespace TEXT)",
                "CREATE TABLE IF NOT EXISTS DataType(DataType_id serial PRIMARY KEY, Nodeset_id BIGINT, DataType_BrowseName TEXT, DataType_Value TEXT, DataType_Namespace TEXT)",
                "CREATE TABLE IF NOT EXISTS ReferenceType(ReferenceType_id serial PRIMARY KEY, Nodeset_id BIGINT, ReferenceType_BrowseName TEXT, ReferenceType_Value TEXT, ReferenceType_Namespace TEXT)",
                "CREATE TABLE IF NOT EXISTS Organisation(Contributor_Id serial PRIMARY KEY, Name TEXT, Description TEXT, LogoUrl TEXT, ContactEmail TEXT, Website TEXT, CreationTime TIMESTAMP, LastModificationTime TIMESTAMP)",
                "CREATE TABLE IF NOT EXISTS Category(Category_Id serial PRIMARY KEY, Name TEXT, Description TEXT, IconUrl TEXT, CreationTime TIMESTAMP, LastModificationTime TIMESTAMP)",
                "CREATE TABLE IF NOT EXISTS AddressSpace(AddressSpace_Id serial PRIMARY KEY, Title TEXT, VersionNumber Text, CopyrightText TEXT, CreationTime TIMESTAMP, LastModificationTime TIMESTAMP, Description TEXT, DocumentationUrl TEXT, IconUrl TEXT, LicenseUrl TEXT, License INTEGER, PurchasingInformationUrl TEXT, TestSpecificationUrl TEXT, ReleaseNotesUrl TEXT, Keywords TEXT[], SupportedLocales TEXT[], NumberOfDownloads BIGINT, Contributor_Id INTEGER, Category_Id INTEGER, Nodeset_Id BIGINT, CONSTRAINT ContributorId FOREIGN KEY (Contributor_id) REFERENCES Organisation(contributor_id),CONSTRAINT CategoryId FOREIGN KEY (Category_Id) REFERENCES Category(Category_Id))"
            };

            try
            {
                _connection = new NpgsqlConnection(PostgreSQLDB.CreateConnectionString());
                _connection.Open();

                foreach (string initCommand in dbInitCommands)
                {
                    NpgsqlCommand sqlCommand = new NpgsqlCommand(initCommand, _connection);
                    sqlCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        ~PostgreSQLDB()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection = null;
            }
        }

        public bool AddUATypeToNodeset(uint nodesetId, UATypes uaType, string browseName, string displayName, string nameSpace)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Open();
                }

                string sqlInsert = string.Format("INSERT INTO public.{0} (Nodeset_id, {0}_browsename, {0}_value, {0}_namespace) VALUES(@nodesetid, @browsename, @displayname, @namespace)", uaType);
                NpgsqlCommand sqlCommand = new NpgsqlCommand(sqlInsert, _connection);
                sqlCommand.Parameters.AddWithValue("nodesetid", (long)nodesetId);
                sqlCommand.Parameters.AddWithValue("browsename", browseName);
                sqlCommand.Parameters.AddWithValue("displayname", displayName);
                sqlCommand.Parameters.AddWithValue("namespace", nameSpace);
                sqlCommand.ExecuteNonQuery();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return false;
        }

        public bool AddMetaDataToNodeSet(uint nodesetId, string name, string value)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Open();
                }

                string sqlInsert = string.Format("INSERT INTO public.Metadata (Nodeset_id, metadata_name, metadata_value) VALUES(@nodesetid, @metadataname, @metadatavalue)");
                NpgsqlCommand sqlCommand = new NpgsqlCommand(sqlInsert, _connection);
                sqlCommand.Parameters.AddWithValue("nodesetid", (long)nodesetId);
                sqlCommand.Parameters.AddWithValue("metadataname", name);
                sqlCommand.Parameters.AddWithValue("metadatavalue", value);
                sqlCommand.ExecuteNonQuery();

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
            if (!DeleteAllTableRecordsForNodeset(nodesetId, "Metadata"))
            {
                return false;
            }

            if (!DeleteAllTableRecordsForNodeset(nodesetId, "ObjectType"))
            {
                return false;
            }

            if (!DeleteAllTableRecordsForNodeset(nodesetId, "VariableType"))
            {
                return false;
            }

            if (!DeleteAllTableRecordsForNodeset(nodesetId, "DataType"))
            {
                return false;
            }

            if (!DeleteAllTableRecordsForNodeset(nodesetId, "ReferenceType"))
            {
                return false;
            }

            return true;
        }

        public string RetrieveMetaData(uint nodesetId, string metaDataTag)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Open();
                }

                string sqlSelect = string.Format("SELECT metadata_value FROM public.Metadata WHERE (Nodeset_id='{0}' AND Metadata_Name='{1}')", (long)nodesetId, metaDataTag);
                NpgsqlCommand sqlCommand = new NpgsqlCommand(sqlSelect, _connection);
                object result = sqlCommand.ExecuteScalar();
                if (result != null)
                {
                    return result.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return string.Empty;
        }

        private bool DeleteAllTableRecordsForNodeset(uint nodesetId, string tableName)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Open();
                }

                string sqlDelete = string.Format("DELETE FROM public.{1} WHERE Nodeset_id='{0}'", (long)nodesetId, tableName);
                NpgsqlCommand sqlCommand = new NpgsqlCommand(sqlDelete, _connection);
                sqlCommand.ExecuteNonQuery();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return false;
        }

        public UANodesetResult[] FindNodesets(string[] keywords)
        {
            List<string> matches = new List<string>();
            List<UANodesetResult> nodesetResults = new List<UANodesetResult>();

            foreach (string result in FindNodesetsInTable(keywords, "Metadata"))
            {
                if (!matches.Contains(result))
                {
                    matches.Add(result);
                }
            }

            foreach (string result in FindNodesetsInTable(keywords, "ObjectType"))
            {
                if (!matches.Contains(result))
                {
                    matches.Add(result);
                }
            }

            foreach (string result in FindNodesetsInTable(keywords, "VariableType"))
            {
                if (!matches.Contains(result))
                {
                    matches.Add(result);
                }
            }

            foreach (string result in FindNodesetsInTable(keywords, "DataType"))
            {
                if (!matches.Contains(result))
                {
                    matches.Add(result);
                }
            }

            foreach (string result in FindNodesetsInTable(keywords, "ReferenceType"))
            {
                if (!matches.Contains(result))
                {
                    matches.Add(result);
                }
            }
            //Get additional metadata (if present and valid) for each match
            foreach (string match in matches)
            {
                if (uint.TryParse(match, out uint matchId))
                {
                    var thisResult = new UANodesetResult();
                    thisResult.Id = matchId;
                    thisResult.Title = RetrieveMetaData(matchId, "nodesettitle") ?? string.Empty;
                    thisResult.Contributor = RetrieveMetaData(matchId, "orgname") ?? string.Empty;
                    thisResult.License = RetrieveMetaData(matchId, "license") ?? string.Empty;
                    thisResult.Version = RetrieveMetaData(matchId, "version") ?? string.Empty;
                    var pubDate = RetrieveMetaData(matchId, "nodesetcreationtime");
                    if (DateTime.TryParse(pubDate, out DateTime useDate))
                        thisResult.CreationTime = useDate;
                    nodesetResults.Add(thisResult);
                }
            }
            return nodesetResults.ToArray();
        }

        private string[] FindNodesetsInTable(string[] keywords, string tableName)
        {
            List<string> results = new List<string>();

            try
            {
                foreach (string keyword in keywords)
                {
                    // special case: * is a wildecard and will return everything
                    string sqlSelect;
                    if (keyword == "*")
                    {
                        sqlSelect = string.Format("SELECT DISTINCT Nodeset_id FROM public.{0}", tableName);
                    }
                    else
                    {
                        sqlSelect = string.Format("SELECT DISTINCT Nodeset_id FROM public.{0} WHERE LOWER({0}_value) ~ '{1}'", tableName, keyword.ToLower());
                    }

                    if (_connection.State != ConnectionState.Open)
                    {
                        _connection.Close();
                        _connection.Open();
                    }

                    NpgsqlCommand sqlCommand = new NpgsqlCommand(sqlSelect, _connection);
                    using (NpgsqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string result = reader.GetInt64(0).ToString();
                                if (!results.Contains(result))
                                {
                                    results.Add(result);
                                }
                            }
                        }
                    }
                }

                return results.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return new string[0];
        }

        public string[] GetAllNamespacesAndNodesets()
        {
            List<string> results = new List<string>();

            try
            {
                string sqlSelect = "SELECT DISTINCT objecttype_namespace, nodeset_id FROM public.objecttype";

                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Open();
                }

                NpgsqlCommand sqlCommand = new NpgsqlCommand(sqlSelect, _connection);
                using (NpgsqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            results.Add(reader.GetString(0) + "," + reader.GetInt64(1).ToString());
                        }
                    }
                }

                return results.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return new string[0];
        }

        public string[] GetAllNamesAndNodesets()
        {
            List<string> results = new List<string>();

            try
            {
                string sqlSelect = "SELECT metadata_value, nodeset_id FROM public.metadata WHERE metadata_name = 'addressspacename'";

                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Open();
                }

                NpgsqlCommand sqlCommand = new NpgsqlCommand(sqlSelect, _connection);
                using (NpgsqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            results.Add(reader.GetString(0) + "," + reader.GetInt64(1).ToString());
                        }
                    }
                }

                return results.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return new string[0];
        }

        public bool AddAddressSpace(uint nodesetId, AddressSpace addressSpace, bool overwrite)
        {
            bool result = false;
            long orgindex = -1;
            long categoryindex = -1;
            if (overwrite)
            {
                result = OverwriteOldAddressspace(nodesetId, addressSpace, ref orgindex, ref categoryindex);
            }
            else
            {
                if (orgindex == -1)
                {
                    orgindex = AddOrganisation(addressSpace.Contributor, overwrite);
                }

                if (categoryindex == -1)
                {
                    categoryindex = AddCategory(addressSpace.Category, overwrite);
                }

                if (orgindex >= 0 && categoryindex >= 0)
                {
                    try
                    {
                        if (_connection.State != ConnectionState.Open)
                        {
                            _connection.Close();
                            _connection.Open();
                        }

                        string sqlQuery = @$"INSERT INTO addressspace 
(title, versionnumber, iconurl, license, licenseurl, description, copyrighttext, creationtime, lastmodificationtime, contributor_id, category_id, nodeset_id, numberofdownloads, keywords, supportedlocales, purchasinginformationurl, testspecificationurl, releasenotesurl, documentationurl) 
VALUES (@title, @versionnumber, @iconurl, @license, @licenseurl, @description, @copyright, @creationtime, @lastmodified, @orgindex, @categoryindex, @nodesetid, @numdownloads, @keywords, @supportedlocales, @purchasingurl, @testurl, @releaseurl, @docurl)";

                        NpgsqlCommand sqlCommand = new NpgsqlCommand(sqlQuery, _connection);
                        sqlCommand.Parameters.AddWithValue("title", addressSpace.Title);
                        sqlCommand.Parameters.AddWithValue("versionnumber", addressSpace.Version);
                        sqlCommand.Parameters.AddWithValue("license", (int)addressSpace.License);
                        sqlCommand.Parameters.AddWithValue("licenseurl", addressSpace.LicenseUrl?.ToString());
                        sqlCommand.Parameters.AddWithValue("iconurl", addressSpace.IconUrl?.ToString());
                        sqlCommand.Parameters.AddWithValue("supportedlocales", addressSpace?.SupportedLocales);
                        sqlCommand.Parameters.AddWithValue("purchasingurl", addressSpace.PurchasingInformationUrl?.ToString());
                        sqlCommand.Parameters.AddWithValue("testurl", addressSpace.TestSpecificationUrl?.ToString());
                        sqlCommand.Parameters.AddWithValue("releaseurl", addressSpace.ReleaseNotesUrl?.ToString());
                        sqlCommand.Parameters.AddWithValue("docurl", addressSpace.DocumentationUrl?.ToString());
                        sqlCommand.Parameters.AddWithValue("description", addressSpace?.Description);
                        sqlCommand.Parameters.AddWithValue("copyright", addressSpace?.CopyrightText);
                        sqlCommand.Parameters.AddWithValue("creationtime", addressSpace?.CreationTime);
                        sqlCommand.Parameters.AddWithValue("lastmodified", addressSpace?.LastModificationTime);
                        sqlCommand.Parameters.AddWithValue("numdownloads", (long)addressSpace?.NumberOfDownloads);
                        sqlCommand.Parameters.AddWithValue("keywords", addressSpace?.Keywords);

                        sqlCommand.Parameters.AddWithValue("orgindex", orgindex);
                        sqlCommand.Parameters.AddWithValue("categoryindex", categoryindex);
                        sqlCommand.Parameters.AddWithValue("nodesetid", (long)nodesetId);
                        sqlCommand.CommandText = sqlQuery;
                        sqlCommand.ExecuteNonQuery();
                        sqlCommand.Parameters.Clear();
                        result = true;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                        result = false;
                    }
                }
            }
            return result;
        }

        private long AddOrganisation(Organisation org, bool overwrite)
        {
            long result = -1;
            try
            {
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Open();
                }

                string sqlQuery = "SELECT name, contributor_id FROM organisation WHERE name = @orgname";
                NpgsqlCommand sqlCommand = new NpgsqlCommand(sqlQuery, _connection);
                sqlCommand.Parameters.AddWithValue("orgname", org.Name);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = reader.GetInt64("contributor_id");
                    }
                }

                sqlCommand.Parameters.Clear();
                if (result == -1)
                {

                    sqlQuery = $"INSERT INTO organisation (name, website, logourl, creationtime, lastmodificationtime, description, contactemail) VALUES (@name, @website, @logourl, @creationtime, @lastmodification, @description, @contactemail); SELECT currval(pg_get_serial_sequence('organisation', 'contributor_id'))";
                    sqlCommand.CommandText = sqlQuery;
                    sqlCommand.CommandText = sqlQuery;
                    sqlCommand.Parameters.AddWithValue("name", org.Name);
                    sqlCommand.Parameters.AddWithValue("website", org.Website?.ToString());
                    sqlCommand.Parameters.AddWithValue("logourl", org.LogoUrl?.ToString());
                    sqlCommand.Parameters.AddWithValue("creationtime", org?.CreationTime);
                    sqlCommand.Parameters.AddWithValue("lastmodification", org?.LastModificationTime);
                    sqlCommand.Parameters.AddWithValue("description", org?.Description);
                    sqlCommand.Parameters.AddWithValue("contactemail", org?.ContactEmail);
                    result = (long)sqlCommand.ExecuteScalar();
                    sqlCommand.Parameters.Clear();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
            return result;
        }

        private long AddCategory(AddressSpaceCategory category, bool overwrite)
        {
            long result = -1;
            try
            {
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Open();
                }

                string sqlQuery = "SELECT name, category_id FROM category WHERE name = @catname";
                NpgsqlCommand sqlCommand = new NpgsqlCommand(sqlQuery, _connection);
                sqlCommand.Parameters.AddWithValue("catname", category.Name);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = reader.GetInt64("category_id");
                    }
                }

                sqlCommand.Parameters.Clear();
                if (result == -1)
                {
                    sqlQuery = "INSERT INTO category (name, description, iconurl, creationtime, lastmodificationtime) VALUES (@name, @description, @iconurl, @creationtime, @lastmodification); SELECT currval(pg_get_serial_sequence('category', 'category_id'))";
                    sqlCommand.CommandText = sqlQuery;
                    sqlCommand.Parameters.AddWithValue("name", category?.Name);
                    sqlCommand.Parameters.AddWithValue("description", category?.Description);
                    sqlCommand.Parameters.AddWithValue("iconurl", category.IconUrl?.ToString());
                    sqlCommand.Parameters.AddWithValue("creationtime", category?.CreationTime);
                    sqlCommand.Parameters.AddWithValue("lastmodification", category?.LastModificationTime);
                    result = (long)sqlCommand.ExecuteScalar();
                    sqlCommand.Parameters.Clear();
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
            return result;
        }

        #region Overwriting AddressSpace
        private bool OverwriteOldAddressspace(uint nodesetid, AddressSpace newAddressSpace, ref long orgId, ref long categoryId)
        {
            bool result = false;
            try
            {
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Open();
                }
                string sqlQuery = "SELECT addressspace_id, contributor_id, category_id FROM addressspace WHERE nodeset_id = @id";
                NpgsqlCommand sqlCommand = new NpgsqlCommand(sqlQuery, _connection);
                sqlCommand.Parameters.AddWithValue("id", (long)nodesetid);

                long addressspace_id = -1;

                using (NpgsqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        addressspace_id = reader.GetInt64("addressspace_id");
                        orgId = reader.GetInt64("contributor_id");
                        categoryId = reader.GetInt64("category_id");
                    }
                }

                // Check if organisation info changed
                OverwriteOrganisation(newAddressSpace.Contributor, orgId, sqlCommand);

                // Check if category info changed
                OverwriteCategory(newAddressSpace.Category, categoryId, sqlCommand);

                sqlCommand.Parameters.Clear();
                if (addressspace_id >= 0)
                {
                    sqlQuery = string.Format("UPDATE addressspace SET title = @title, lastmodificationtime = @lastmod, versionnumber = @version, copyrighttext = @copyright, description = @description, documentationurl = @docurl, iconurl = @iconurl, licenseurl = @licenseurl, license = @license, purchasinginformationurl = @purchasingurl, testspecificationurl = @testurl, releasenotesurl = @releaseurl, keywords = @keywords, supportedlocales = @keywords WHERE nodeset_id = {0}", nodesetid);
                    sqlCommand.CommandText = sqlQuery;
                    sqlCommand.Parameters.AddWithValue("title", newAddressSpace.Title);
                    sqlCommand.Parameters.AddWithValue("lastmod", newAddressSpace.LastModificationTime);
                    sqlCommand.Parameters.AddWithValue("version", newAddressSpace.Version);
                    sqlCommand.Parameters.AddWithValue("copyright", newAddressSpace.CopyrightText);
                    sqlCommand.Parameters.AddWithValue("description", newAddressSpace.Description);
                    sqlCommand.Parameters.AddWithValue("docurl", newAddressSpace.DocumentationUrl?.ToString());
                    sqlCommand.Parameters.AddWithValue("iconurl", newAddressSpace.IconUrl?.ToString());
                    sqlCommand.Parameters.AddWithValue("licenseurl", newAddressSpace.LicenseUrl?.ToString());
                    sqlCommand.Parameters.AddWithValue("license", (int)newAddressSpace.License);
                    sqlCommand.Parameters.AddWithValue("purchasingurl", newAddressSpace.PurchasingInformationUrl?.ToString());
                    sqlCommand.Parameters.AddWithValue("testurl", newAddressSpace.TestSpecificationUrl?.ToString());
                    sqlCommand.Parameters.AddWithValue("releaseurl", newAddressSpace.ReleaseNotesUrl?.ToString());
                    sqlCommand.Parameters.AddWithValue("keywords", newAddressSpace.Keywords);
                    sqlCommand.Parameters.AddWithValue("locales", newAddressSpace.SupportedLocales);
                    sqlCommand.ExecuteNonQuery();
                    sqlCommand.Parameters.Clear();
                }
                result = true;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                result = false;
            }
            return result;
        }

        private void OverwriteCategory(AddressSpaceCategory category, long category_id, NpgsqlCommand sqlCommand)
        {
            string sqlQuery = string.Format("SELECT name, iconurl, description FROM category WHERE category_id = {0}", category_id);
            sqlCommand.CommandText = sqlQuery;
            using (NpgsqlDataReader reader = sqlCommand.ExecuteReader())
            {
                if (reader.Read())
                {
                    string name = reader.GetString("name");
                    string description = reader.GetString("description");
                    string logourl = reader.GetString("iconurl");

                    if (name != category.Name
                        || description != category.Description
                        || logourl != category.IconUrl.ToString())
                    {
                        sqlQuery = "UPDATE category SET name = @name, description = @description, iconurl = @iconurl, lastmodificationtime = @lastmodificationtime WHERE category_id = @id";
                        sqlCommand.CommandText = sqlQuery;
                        sqlCommand.Parameters.AddWithValue("name", category.Name);
                        sqlCommand.Parameters.AddWithValue("description", category.Description);
                        sqlCommand.Parameters.AddWithValue("logourl", category.IconUrl);
                        sqlCommand.Parameters.AddWithValue("lastmodification", category.LastModificationTime);
                        sqlCommand.Parameters.AddWithValue("id", category_id);
                        sqlCommand.ExecuteNonQuery();
                        sqlCommand.Parameters.Clear();
                    }
                }
            }
        }

        private void OverwriteOrganisation(Organisation organisation, long organisation_id, NpgsqlCommand sqlCommand)
        {
            string sqlQuery = string.Format("SELECT name, contactemail, website, description, logourl FROM organisation WHERE contributor_id = {0}", organisation_id);
            sqlCommand.CommandText = sqlQuery;
            using (NpgsqlDataReader reader = sqlCommand.ExecuteReader())
            {
                if (reader.Read())
                {
                    string name = reader.GetString("name");
                    string contactemail = reader.GetString("contactemail");
                    string website = reader.GetString("website");
                    string description = reader.GetString("description");
                    string logourl = reader.GetString("logourl");

                    if (name != organisation.Name
                        || contactemail != organisation.ContactEmail
                        || website != organisation.Website.ToString()
                        || description != organisation.Description
                        || logourl != organisation.LogoUrl.ToString())
                    {
                        sqlQuery = "UPDATE organisation SET name = @name, contactemail = @mail, website = @website, description = @description, logourl = @logourl WHERE contributor_id = @id";
                        sqlCommand.CommandText = sqlQuery;
                        sqlCommand.Parameters.AddWithValue("name", organisation.Name);
                        sqlCommand.Parameters.AddWithValue("mail", organisation.ContactEmail);
                        sqlCommand.Parameters.AddWithValue("website", organisation.Website);
                        sqlCommand.Parameters.AddWithValue("description", organisation.Description);
                        sqlCommand.Parameters.AddWithValue("logourl", organisation.LogoUrl);
                        sqlCommand.Parameters.AddWithValue("lastmodification", organisation.LastModificationTime);
                        sqlCommand.Parameters.AddWithValue("id", organisation_id);
                        sqlCommand.ExecuteNonQuery();
                        sqlCommand.Parameters.Clear();
                    }
                }
            }
        }

        #endregion

        #region MigrationTool
        /// <summary>
        /// Gets nodesets that aren't in table addressspace and migrates them
        /// </summary>
        /// <returns></returns>
        public bool TryMigrateTables()
        {
            bool migrationResult = false;
            try
            {
                string[] nodesetsToTransfer = GetMissingNodesets();

                foreach (string nodeset in nodesetsToTransfer)
                {
                    if (uint.TryParse(nodeset, out uint nodesetId))
                    {
                        AddressSpace addressSpace = new AddressSpace();

                        addressSpace.Description = RetrieveMetaData(nodesetId, "description");
                        addressSpace.CreationTime = DateTime.ParseExact(RetrieveMetaData(nodesetId, "adressspacecreationtime"), "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        addressSpace.LastModificationTime = DateTime.ParseExact(RetrieveMetaData(nodesetId, "adressspacemodifiedtime"), "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        addressSpace.Version = RetrieveMetaData(nodesetId, "version");
                        addressSpace.Title = RetrieveMetaData(nodesetId, "nodesettitle");
                        addressSpace.CopyrightText = RetrieveMetaData(nodesetId, "copyright");
                        addressSpace.DocumentationUrl = CreateUri(RetrieveMetaData(nodesetId, "documentationurl"));
                        addressSpace.PurchasingInformationUrl = CreateUri(RetrieveMetaData(nodesetId, "purchasinginfo"));
                        addressSpace.IconUrl = CreateUri(RetrieveMetaData(nodesetId, "iconurl"));
                        addressSpace.LicenseUrl = CreateUri(RetrieveMetaData(nodesetId, "licenseurl"));
                        addressSpace.ReleaseNotesUrl = CreateUri(RetrieveMetaData(nodesetId, "releasenotes"));
                        addressSpace.TestSpecificationUrl = CreateUri(RetrieveMetaData(nodesetId, "testspecification"));
                        addressSpace.NumberOfDownloads = Convert.ToUInt32(RetrieveMetaData(nodesetId, "numdownloads"));
                        addressSpace.Keywords = RetrieveMetaData(nodesetId, "keywords").Split(',');
                        addressSpace.SupportedLocales = RetrieveMetaData(nodesetId, "locales").Split(',');
                        switch (RetrieveMetaData(nodesetId, "license"))
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

                        addressSpace.Contributor.ContactEmail = RetrieveMetaData(nodesetId, "orgcontact");
                        addressSpace.Contributor.Name = RetrieveMetaData(nodesetId, "orgname");
                        addressSpace.Contributor.Description = RetrieveMetaData(nodesetId, "orgdescription");
                        addressSpace.Contributor.Website = CreateUri(RetrieveMetaData(nodesetId, "orgwebsite"));
                        addressSpace.Contributor.LogoUrl = CreateUri(RetrieveMetaData(nodesetId, "orglogo"));
                        addressSpace.Contributor.CreationTime = DateTime.ParseExact(RetrieveMetaData(nodesetId, "contributorcreationtime"), "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        addressSpace.Contributor.LastModificationTime = DateTime.ParseExact(RetrieveMetaData(nodesetId, "contributormodifiedtime"), "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                        addressSpace.Category.Name = RetrieveMetaData(nodesetId, "addressspacename");
                        addressSpace.Category.Description = RetrieveMetaData(nodesetId, "addressspacedescription");
                        addressSpace.Category.IconUrl = CreateUri(RetrieveMetaData(nodesetId, "addressspaceiconurl"));
                        addressSpace.Category.CreationTime = DateTime.ParseExact(RetrieveMetaData(nodesetId, "categorycreationtime"), "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        addressSpace.Category.LastModificationTime = DateTime.ParseExact(RetrieveMetaData(nodesetId, "categorymodifiedtime"), "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                        if (!AddAddressSpace(nodesetId, addressSpace, true))
                        {
                            throw new Exception(String.Format("Migrating Nodeset data failed for nodeset {0}", nodesetId));
                        }
                    }
                }
                migrationResult = true;
            }
            catch (Exception e)
            {
                _logger.LogError("Failed migration: {0}", e.Message);
                migrationResult = false;
            }
            return migrationResult;
        }

        /// <summary>
        /// Gets the nodesets from metadata that aren't in addressspace
        /// </summary>
        /// <returns></returns>
        private string[] GetMissingNodesets()
        {
            List<string> nodesets = new List<string>();
            try
            {

                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Open();
                }

                string sqlQuery = "SELECT DISTINCT(nodeset_id) FROM metadata EXCEPT SELECT nodeset_id FROM addressspace";
                NpgsqlCommand sqlCommand = new NpgsqlCommand(sqlQuery, _connection);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        nodesets.Add(reader.GetValue("nodeset_id").ToString());
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Getting missing nodesets failed");
            }
            return nodesets.ToArray();
        }

        private Uri CreateUri(string uri)
        {
            Uri result = null;
            if (!string.IsNullOrEmpty(uri))
            {
                result = new Uri(uri);
            }
            return result;
        }
        #endregion
    }

}
