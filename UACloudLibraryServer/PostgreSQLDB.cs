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
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Logging;
    using Npgsql;
    using System;
    using System.Collections.Generic;
    using System.Data;
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
                "CREATE TABLE IF NOT EXISTS ReferenceType(ReferenceType_id serial PRIMARY KEY, Nodeset_id BIGINT, ReferenceType_BrowseName TEXT, ReferenceType_Value TEXT, ReferenceType_Namespace TEXT)"
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
    }
}
