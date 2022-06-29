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
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Npgsql;
    using Opc.Ua.Cloud.Library.Models;

    public class PostgreSQLDB : IDatabase, IDisposable
    {
        private NpgsqlConnection _connection = null;
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
            return string.Format(
                "Server={0};Username={1};Database={2};Port={3};Password={4};SSLMode=Prefer",
                Host,
                User,
                DBname,
                Port,
                Password);
        }

        public PostgreSQLDB(ILoggerFactory logger, IConfiguration configuration, AppDbContext dbContext)
        {
            _logger = logger.CreateLogger("PostgreSQLDB");
            _dbContext = dbContext;
            // Setup the database tables
            string[] dbInitCommands = {
                "CREATE TABLE IF NOT EXISTS Metadata(Metadata_id serial PRIMARY KEY, Nodeset_id BIGINT, Metadata_Name TEXT, Metadata_Value TEXT)",
            };

            try
            {
                _connection = new NpgsqlConnection(CreateConnectionString(configuration));
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

        public bool UpdateMetaDataForNodeSet(uint nodesetId, string name, string value)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Open();
                }

                string sqlInsert = string.Format("UPDATE public.Metadata SET metadata_value=@metadatavalue WHERE Nodeset_id=@nodesetid AND metadata_name=@metadataname");
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

            return true;
        }

        public void RetrieveAllMetadata(uint nodesetId, UANameSpace nameSpace)
        {
            if (DateTime.TryParse(RetrieveMetaData(nodesetId, "nodesetcreationtime"), out DateTime parsedDateTime))
            {
                nameSpace.Nodeset.PublicationDate = parsedDateTime;
            }

            if (DateTime.TryParse(RetrieveMetaData(nodesetId, "nodesetmodifiedtime"), out parsedDateTime))
            {
                nameSpace.Nodeset.LastModifiedDate = parsedDateTime;
            }

            nameSpace.Title = RetrieveMetaData(nodesetId, "nodesettitle");

            nameSpace.Nodeset.Version = RetrieveMetaData(nodesetId, "version");

            nameSpace.Nodeset.Identifier = nodesetId;

            string uri = GetNamespaceUriForNodeset(nodesetId);
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.Nodeset.NamespaceUri = new Uri(uri);
            }

            switch (RetrieveMetaData(nodesetId, "license"))
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

            nameSpace.CopyrightText = RetrieveMetaData(nodesetId, "copyright");

            nameSpace.Description = RetrieveMetaData(nodesetId, "description");

            nameSpace.Category.Name = RetrieveMetaData(nodesetId, "addressspacename");

            nameSpace.Category.Description = RetrieveMetaData(nodesetId, "addressspacedescription");

            uri = RetrieveMetaData(nodesetId, "addressspaceiconurl");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.Category.IconUrl = new Uri(uri);
            }

            uri = RetrieveMetaData(nodesetId, "documentationurl");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.DocumentationUrl = new Uri(uri);
            }

            uri = RetrieveMetaData(nodesetId, "iconurl");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.IconUrl = new Uri(uri);
            }

            uri = RetrieveMetaData(nodesetId, "licenseurl");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.LicenseUrl = new Uri(uri);
            }

            uri = RetrieveMetaData(nodesetId, "purchasinginfo");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.PurchasingInformationUrl = new Uri(uri);
            }

            uri = RetrieveMetaData(nodesetId, "releasenotes");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.ReleaseNotesUrl = new Uri(uri);
            }

            uri = RetrieveMetaData(nodesetId, "testspecification");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.TestSpecificationUrl = new Uri(uri);
            }

            string keywords = RetrieveMetaData(nodesetId, "keywords");
            if (!string.IsNullOrEmpty(keywords))
            {
                nameSpace.Keywords = keywords.Split(',');
            }

            string locales = RetrieveMetaData(nodesetId, "locales");
            if (!string.IsNullOrEmpty(locales))
            {
                nameSpace.SupportedLocales = locales.Split(',');
            }

            nameSpace.Contributor.Name = RetrieveMetaData(nodesetId, "orgname");

            nameSpace.Contributor.Description = RetrieveMetaData(nodesetId, "orgdescription");

            uri = RetrieveMetaData(nodesetId, "orglogo");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.Contributor.LogoUrl = new Uri(uri);
            }

            nameSpace.Contributor.ContactEmail = RetrieveMetaData(nodesetId, "orgcontact");

            uri = RetrieveMetaData(nodesetId, "orgwebsite");
            if (!string.IsNullOrEmpty(uri))
            {
                nameSpace.Contributor.Website = new Uri(uri);
            }

            nameSpace.ValidationStatus = RetrieveMetaData(nodesetId, "validationstatus");

            if (uint.TryParse(RetrieveMetaData(nodesetId, "numdownloads"), out uint parsedDownloads))
            {
                nameSpace.NumberOfDownloads = parsedDownloads;
            }
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

            if (!(keywords?[0] == "*"))
            {
                foreach (var keyword in keywords)
                {
                    var matchesForKeyword = _dbContext.nodeModels
                        .Where(nm => Regex.IsMatch(nm.DisplayName.FirstOrDefault().Text, keyword))
                        .Select(nm => nm.NodeSet.Identifier)
                        .Distinct()
                        .ToList();
                    foreach (var match in matchesForKeyword)
                    {
                        if (!matches.Contains(match))
                        {
                            matches.Add(match);
                        }
                    };
                };
            }
            else
            {
                matches = _dbContext.nodeSets.Select(nsm => nsm.Identifier).Distinct().ToList();
            }

            foreach (string result in FindNodesetsInTable(keywords, "Metadata"))
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
                    thisResult.ValidationStatus = RetrieveMetaData(matchId, "validationstatus") ?? string.Empty; ;
                    var pubDate = RetrieveMetaData(matchId, "nodesetcreationtime");
                    if (DateTime.TryParse(pubDate, out DateTime useDate))
                    {
                        thisResult.PublicationDate = useDate;
                    }

                    var namespaceUri = GetNamespaceUriForNodeset(matchId);
                    thisResult.NameSpaceUri = namespaceUri;

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
                        sqlSelect = string.Format("SELECT DISTINCT Nodeset_id FROM public.{0} WHERE LOWER({0}_value) ~ '{1}'", tableName, keyword.ToLower(CultureInfo.InvariantCulture));
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
                                string result = reader.GetInt64(0).ToString(CultureInfo.InvariantCulture);
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

            return Array.Empty<string>();
        }

        public string[] GetAllNamespacesAndNodesets()
        {
            List<string> results = new List<string>();

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

        public string GetNamespaceUriForNodeset(uint nodesetId)
        {
            try
            {
                var identifier = nodesetId.ToString(CultureInfo.InvariantCulture);
                var modelUri = _dbContext.nodeSets.Where(nsm => nsm.Identifier == identifier).Select(nsm => nsm.ModelUri).FirstOrDefault();
                return modelUri;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return string.Empty;
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
                            results.Add(reader.GetString(0) + "," + reader.GetInt64(1).ToString(CultureInfo.InvariantCulture));
                        }
                    }
                }

                return results.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Array.Empty<string>();
        }

        public void Dispose()
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Close();
            }

            _connection.Dispose();
        }
    }
}
