
namespace UACloudLibrary
{
    using Npgsql;
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using UACloudLibrary.Models;

    /// <summary>
    /// PostgresSQL storage class
    /// </summary>
    public class PostgresSQLDB
    {
        private string _connectionString;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PostgresSQLDB()
        {
            // Obtain connection string information from the environment
            string Host = Environment.GetEnvironmentVariable("PostgresSQLEndpoint");
            string User = Environment.GetEnvironmentVariable("PostgresSQLUsername");
            string Password = Environment.GetEnvironmentVariable("PostgresSQLPassword");

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
                Debug.WriteLine(ex, "Connection to PostgreSQL failed!");
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
                    var sqlInsert = String.Format("INSERT INTO public.nodesets (nodeset_filename) VALUES('{0}')", filename);
                    var sqlCommand = new NpgsqlCommand(sqlInsert, connection);
                    sqlCommand.ExecuteNonQuery();

                    // query for the id of the record
                    var sqlQuery = String.Format("SELECT nodeset_id from public.nodesets where nodeset_filename = '{0}'", filename);
                    sqlCommand = new NpgsqlCommand(sqlQuery, connection);
                    var result = sqlCommand.ExecuteScalar();
                    if (int.TryParse(result.ToString(), out retVal))
                    {
                        return Task.FromResult(retVal);
                    }
                    else
                    {
                        Debug.WriteLine("Record could be inserted or found!");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Connection to PostgreSQL failed!");
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
                    var sqlInsert = String.Format("INSERT INTO public.{0}s ({0}_browsename, {0}_displayname, {0}_namespace) VALUES('{1}, {2}, {3}') WHERE nodeset_id = {4}", UAType, UATypeBrowseName, UATypeDisplayName, UATypeNamespace, NodesetId);
                    var sqlCommand = new NpgsqlCommand(sqlInsert, connection);
                    sqlCommand.ExecuteNonQuery();
                    return Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Could not insert to PostgreSQL!");
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
                    var sqlInsert = String.Format("INSERT INTO public.objecttypes (metadata_name, metadata_value) VALUES('{0}, {1}') WHERE nodeset_id = {2}", MetaDataName, MetaDataValue, NodesetId);
                    var sqlCommand = new NpgsqlCommand(sqlInsert, connection);
                    sqlCommand.ExecuteNonQuery();
                    return Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Could not insert to PostgreSQL!");
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
                    string sqlParams = string.Empty;
                    int i = 0;

                    foreach (string keyword in keywords)
                    {
                        string paramName = string.Format("keyword{0}", i);
                        mySqlCmd.Parameters.AddWithValue(paramName, "%" + keyword + "%");
                        sqlParams += string.Format(" LOWER(public.metadata.metadata_value) LIKE LOWER(@{0}) or", paramName);
                        i++;
                    }
                    sqlParams = sqlParams.Substring(0, sqlParams.Length - 2);

                    //TODO: Expand this query to search more tables with "JOIN"
                    var sqlQuery = String.Format(@"SELECT public.nodesets.nodeset_filename
                        FROM public.metadata
                        INNER JOIN public.nodesets
                        ON public.metadata.nodeset_id = public.nodesets.nodeset_id
                        WHERE {0}", sqlParams);

                    //TODO: Remove debugging of parameters
                    #if DEBUG
                    mySqlCmd.CommandText = sqlQuery;
                    Debug.WriteLine(mySqlCmd.CommandText);
                    string debugSQL = mySqlCmd.CommandText;
                    foreach (NpgsqlParameter param in mySqlCmd.Parameters)
                    {
                        debugSQL = debugSQL.Replace(("@" + param.ParameterName), param.Value.ToString());
                    }
                    Debug.WriteLine(debugSQL);
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
                Debug.WriteLine(ex, "Connection to PostgreSQL failed!");
            }
            return Task.FromResult(string.Empty);
        }
    }
}
