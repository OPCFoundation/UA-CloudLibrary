
namespace UACloudLibrary
{
    using Npgsql;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using UACloudLibrary.Models;

    public class PostgreSQLDB : IDatabase
    {
        private string _connectionString;

        public PostgreSQLDB()
        {
            // Obtain connection string information from the environment
            string Host = Environment.GetEnvironmentVariable("PostgreSQLEndpoint");
            string User = Environment.GetEnvironmentVariable("PostgreSQLUsername");
            string Password = Environment.GetEnvironmentVariable("PostgreSQLPassword");

            string DBname = "uacloudlib";
            string Port = "5432";

            // Build connection string using parameters from portal
            _connectionString = string.Format(
                "Server={0};Username={1};Database={2};Port={3};Password={4};SSLMode=Prefer",
                Host,
                User,
                DBname,
                Port,
                Password);

            // Setup the database tables
            string[] dbInitCommands = {
                "CREATE TABLE IF NOT EXISTS Metadata(Metadata_id serial PRIMARY KEY, Nodeset_id BIGINT, Metadata_Name TEXT, Metadata_Value TEXT)",
                "CREATE TABLE IF NOT EXISTS ObjectTypes(ObjectType_id serial PRIMARY KEY, Nodeset_id BIGINT, ObjectType_BrowseName TEXT, ObjectType_DisplayName TEXT, ObjectType_Namespace TEXT)",
                "CREATE TABLE IF NOT EXISTS VariableTypes(VariableType_id serial PRIMARY KEY, Nodeset_id BIGINT, VariableType_BrowseName TEXT, VariableType_DisplayName TEXT, VariableType_Namespace TEXT)",
                "CREATE TABLE IF NOT EXISTS DataTypes(DataType_id serial PRIMARY KEY, Nodeset_id BIGINT, DataType_BrowseName TEXT, DataType_DisplayName TEXT, DataType_Namespace TEXT)",
                "CREATE TABLE IF NOT EXISTS ReferenceTypes(ReferenceType_id serial PRIMARY KEY, Nodeset_id BIGINT, ReferenceType_BrowseName TEXT, ReferenceType_DisplayName TEXT, ReferenceType_Namespace TEXT)"
            };

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    foreach (string initCommand in dbInitCommands)
                    {
                        var sqlCommand = new NpgsqlCommand(initCommand, connection);
                        sqlCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public bool AddUATypeToNodeset(uint nodesetId, UATypes uaType, string browseName, string displayName, string nameSpace)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    // DELETE FROM Customers WHERE CustomerName='Alfreds Futterkiste';
                    var sqlInsert = String.Format("INSERT INTO public.{0}s (Nodeset_id, {0}_browsename, {0}_displayname, {0}_namespace) VALUES(@nodesetid, @browsename, @displayname, @namespace)", uaType);
                    var sqlCommand = new NpgsqlCommand(sqlInsert, connection);
                    sqlCommand.Parameters.AddWithValue("nodesetid", (long)nodesetId);
                    sqlCommand.Parameters.AddWithValue("browsename", browseName);
                    sqlCommand.Parameters.AddWithValue("displayname", displayName);
                    sqlCommand.Parameters.AddWithValue("namespace", nameSpace);
                    sqlCommand.ExecuteNonQuery();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        public bool AddMetaDataToNodeSet(uint nodesetId, string name, string value)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    // insert the record
                    var sqlInsert = String.Format("INSERT INTO public.Metadata (Nodeset_id, metadata_name, metadata_value) VALUES(@nodesetid, @metadataname, @metadatavalue)");
                    var sqlCommand = new NpgsqlCommand(sqlInsert, connection);
                    sqlCommand.Parameters.AddWithValue("nodesetid", (long)nodesetId);
                    sqlCommand.Parameters.AddWithValue("metadataname", name);
                    sqlCommand.Parameters.AddWithValue("metadatavalue", value);
                    sqlCommand.ExecuteNonQuery();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        public bool DeleteAllRecordsForNodeset(uint nodesetId)
        {
            if (!DeleteAllTableRecordsForNodeset(nodesetId, "Metadata"))
            {
                return false;
            }

            if (!DeleteAllTableRecordsForNodeset(nodesetId, "ObjectTypes"))
            {
                return false;
            }

            if (!DeleteAllTableRecordsForNodeset(nodesetId, "VariableTypes"))
            {
                return false;
            }

            if (!DeleteAllTableRecordsForNodeset(nodesetId, "DataTypes"))
            {
                return false;
            }

            if (!DeleteAllTableRecordsForNodeset(nodesetId, "ReferenceTypes"))
            {
                return false;
            }

            return true;
        }

        public string RetrieveMetaData(uint nodesetId, string metaDataTag)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    var sqlInsert = String.Format("SELECT metadata_value FROM Metadata WHERE (Nodeset_id='{0}' AND Metadata_Name='{1}')", (long)nodesetId, metaDataTag);
                    var sqlCommand = new NpgsqlCommand(sqlInsert, connection);
                    var result = sqlCommand.ExecuteScalar();
                    if (result != null)
                    {
                        return result.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }


            return string.Empty;
        }

        private bool DeleteAllTableRecordsForNodeset(uint nodesetId, string tableName)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    var sqlInsert = String.Format("DELETE FROM {1} WHERE Nodeset_id='{0}'", (long)nodesetId, tableName);
                    var sqlCommand = new NpgsqlCommand(sqlInsert, connection);
                    sqlCommand.ExecuteNonQuery();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        public string FindNodesets(string[] keywords)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    var mySqlCmd = new NpgsqlCommand();
                    mySqlCmd.Connection = connection;

                    // Search for matching metadata fields
                    // Add keywords as parameters
                    string sqlParams = string.Empty;
                    int i = 0;
                    foreach (string keyword in keywords)
                    {
                        string paramName = string.Format("keyword{0}", i);
                        mySqlCmd.Parameters.AddWithValue(paramName, "%" + keyword + "%");
                        sqlParams += string.Format(@"
                         LOWER(public.metadata.metadata_value)
                         LIKE LOWER(@{0}) or", paramName);
                        i++;
                    }

                    sqlParams = sqlParams.Substring(0, sqlParams.Length - 2);
                    // Build parameterized query string
                    var sqlQuery = String.Format(@"
                        SELECT public.nodesets.nodeset_filename
                        FROM public.nodesets
                        NATURAL JOIN public.metadata
                        WHERE {0}", sqlParams);

                    // Search for matching objecttype fields
                    // TODO: This can be done in a loop with the other tables (variabletypes, referencetypes) since their naming convention is similar
                    // Re-use existing parameters
                    sqlParams = string.Empty;
                    foreach (NpgsqlParameter param in mySqlCmd.Parameters)
                    {
                        sqlParams += string.Format(@"
                         LOWER(public.objecttypes.objecttype_browsename)
                         LIKE LOWER(@{0}) or
                         LOWER(public.objecttypes.objecttype_displayname)
                         LIKE LOWER(@{0}) or", param.ParameterName);
                    }

                    sqlParams = sqlParams.Substring(0, sqlParams.Length - 2);
                    // Update parameterized query string
                    sqlQuery += String.Format(@"
                        UNION
                        SELECT public.nodesets.nodeset_filename
                        FROM public.nodesets
                        NATURAL JOIN public.objecttypes
                        WHERE {0}", sqlParams);

#if DEBUG
                    mySqlCmd.CommandText = sqlQuery;
                    Console.WriteLine(mySqlCmd.CommandText);
                    string debugSQL = mySqlCmd.CommandText;
                    foreach (NpgsqlParameter param in mySqlCmd.Parameters)
                    {
                        debugSQL = debugSQL.Replace(("@" + param.ParameterName), "'" + param.Value.ToString() + "'");
                    }
                    Console.WriteLine(debugSQL);
#endif

                    var result = mySqlCmd.ExecuteScalar();
                    if (result != null)
                    {
                        return result.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return string.Empty;
        }
    }
}
