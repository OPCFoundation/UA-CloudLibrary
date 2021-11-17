
namespace UACloudLibrary
{
    using Npgsql;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using UACloudLibrary.Models;

    /// <summary>
    /// PostgresSQL storage class
    /// </summary>
    public class PostgreSQLDB
    {
        private string _connectionString;

        /// <summary>
        /// Default constructor
        /// </summary>
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

            // Setup the database
            string[] dbInitCommands = {
                "CREATE TABLE IF NOT EXISTS Nodesets(Nodeset_id serial PRIMARY KEY, Nodeset_Filename TEXT)",
                "CREATE TABLE IF NOT EXISTS Metadata(Metadata_id serial PRIMARY KEY, Nodeset_id INT, Metadata_Name TEXT, Metadata_Value TEXT, CONSTRAINT fk_Nodeset FOREIGN KEY(Nodeset_id) REFERENCES Nodesets(Nodeset_id))",
                "CREATE TABLE IF NOT EXISTS ObjectTypes(ObjectType_id serial PRIMARY KEY, Nodeset_id INT, ObjectType_BrowseName TEXT, ObjectType_DisplayName TEXT, ObjectType_Namespace TEXT, CONSTRAINT fk_Nodeset FOREIGN KEY(Nodeset_id) REFERENCES Nodesets(Nodeset_id))",
                "CREATE TABLE IF NOT EXISTS VariableTypes(VariableType_id serial PRIMARY KEY, Nodeset_id INT, VariableType_BrowseName TEXT, VariableType_DisplayName TEXT, VariableType_Namespace TEXT, CONSTRAINT fk_Nodeset FOREIGN KEY(Nodeset_id) REFERENCES Nodesets(Nodeset_id))",
                "CREATE TABLE IF NOT EXISTS DataTypes(DataType_id serial PRIMARY KEY, Nodeset_id INT, DataType_BrowseName TEXT, DataType_DisplayName TEXT, DataType_Namespace TEXT, CONSTRAINT fk_Nodeset FOREIGN KEY(Nodeset_id) REFERENCES Nodesets(Nodeset_id))",
                "CREATE TABLE IF NOT EXISTS ReferenceTypes(ReferenceType_id serial PRIMARY KEY, Nodeset_id INT, ReferenceType_BrowseName TEXT, ReferenceType_DisplayName TEXT, ReferenceType_Namespace TEXT, CONSTRAINT fk_Nodeset FOREIGN KEY(Nodeset_id) REFERENCES Nodesets(Nodeset_id))"
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

        /// <summary>
        /// Create a record for a newly uploaded nodeset file. This is the first step in database ingestion.
        /// </summary>
        /// <param name="filename">Path to where the nodeset file was stored</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The database record ID for the new nodeset</returns>
        public Task<int> AddNodeSetToDatabaseAsync(string filename, CancellationToken cancellationToken = default)
        {
            int retVal = -1;
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    // insert the record
                    var sqlInsert = String.Format("INSERT INTO public.nodesets (nodeset_filename) VALUES(@filename)");
                    var sqlCommand = new NpgsqlCommand(sqlInsert, connection);
                    sqlCommand.Parameters.AddWithValue("filename", filename);
                    sqlCommand.ExecuteNonQuery();

                    // query for the id of the record
                    var sqlQuery = String.Format("SELECT nodeset_id from public.nodesets where nodeset_filename = @filename");
                    sqlCommand = new NpgsqlCommand(sqlQuery, connection);
                    sqlCommand.Parameters.AddWithValue("filename", filename);
                    var result = sqlCommand.ExecuteScalar();
                    if (int.TryParse(result.ToString(), out retVal))
                    {
                        return Task.FromResult(retVal);
                    }
                    else
                    {
                        Console.WriteLine("Record could be inserted or found!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return Task.FromResult(retVal);
        }

        /// <summary>
        /// Add a record of an ObjectType to a Nodeset Record in the DB. Call this foreach ObjectType discovered in an uploaded Nodeset
        /// </summary>
        public Task<bool> AddUATypeToNodesetAsync(int NodesetId, UATypes UAType, string UATypeBrowseName, string UATypeDisplayName, string UATypeNamespace, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    // insert the record
                    var sqlInsert = String.Format("INSERT INTO public.{0}s ({0}_browsename, {0}_displayname, {0}_namespace) VALUES('@browsename, @displayname, @namespace') WHERE nodeset_id = @nodesetid");
                    var sqlCommand = new NpgsqlCommand(sqlInsert, connection);
                    sqlCommand.Parameters.AddWithValue("browsename", UATypeBrowseName);
                    sqlCommand.Parameters.AddWithValue("displayname", UATypeDisplayName);
                    sqlCommand.Parameters.AddWithValue("namespace", UATypeNamespace);
                    sqlCommand.Parameters.AddWithValue("nodesetid", NodesetId);

                    sqlCommand.ExecuteNonQuery();
                    return Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return Task.FromResult(false);
        }

        /// <summary>
        /// Add a record of an arbitrary MetaData field to a Nodeset Record in the DB. You can insert any metadata field at any time, as long as you know the ID of the NodeSet you want to attach it to
        /// Note these metadata fields are what are used in the FindNodeSet method
        /// </summary>
        public Task<bool> AddMetaDataToNodeSet(int NodesetId, string MetaDataName, string MetaDataValue, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    // insert the record
                    var sqlInsert = String.Format("INSERT INTO public.objecttypes (metadata_name, metadata_value) VALUES(@metadataname, @metadatavalue) WHERE nodeset_id = @nodesetid");
                    var sqlCommand = new NpgsqlCommand(sqlInsert, connection);
                    sqlCommand.Parameters.AddWithValue("metadataname", MetaDataName);
                    sqlCommand.Parameters.AddWithValue("metadatavalue", MetaDataValue);
                    sqlCommand.Parameters.AddWithValue("nodesetid", NodesetId);
                    sqlCommand.ExecuteNonQuery();
                    return Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return Task.FromResult(false);
        }

        /// <summary>
        /// Find an existing nodeset based on keywords
        /// </summary>
        public Task<string> FindNodesetsAsync(string[] keywords, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    var mySqlCmd = new NpgsqlCommand();
                    mySqlCmd.Connection = connection;

                    //Search for matching metadata fields
                    //  Add keywords as parameters
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
                    //  Build parameterized query string
                    var sqlQuery = String.Format(@"
                        SELECT public.nodesets.nodeset_filename
                        FROM public.nodesets
                        NATURAL JOIN public.metadata
                        WHERE {0}", sqlParams);

                    // Search for matching objecttype fields
                    //     TODO: This can be done in a loop with the other tables (variabletypes, referencetypes) since their naming convention is similar
                    //  Re-use existing parameters
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
                    //  Update parameterized query string
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
                        return Task.FromResult(result.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return Task.FromResult(string.Empty);
        }
    }
}
