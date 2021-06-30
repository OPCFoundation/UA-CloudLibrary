
namespace UA_CloudLibrary
{
    using Npgsql;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// PostgresSQL storage class
    /// </summary>
    public class PostgresSQLDB
    {
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
            string connectionString = string.Format(
                "Server={0};Username={1};Database={2};Port={3};Password={4};SSLMode=Prefer",
                Host,
                User,
                DBname,
                Port,
                Password);

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS nodesets(Id serial PRIMARY KEY, Nodeset XML)", connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Connection to PostgreSQL failed!");
            }
        }

        /// <summary>
        /// Find a nodeset using keywords within PostgreSQL
        /// </summary>
        public Task<string[]> FindNodesetsAsync(string keywords, CancellationToken cancellationToken = default)
        {
            try
            {
                List<string> results = new List<string>();

                // TODO!

                return Task.FromResult(new string[1]);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Find files failed!");
                return Task.FromResult(new string[1]);
            }
        }

        /// <summary>
        /// Upload a nodeset to PostgreSQL
        /// </summary>
        public Task<bool> UploadNodesetAsync(string name, string content, CancellationToken cancellationToken = default)
        {
            try
            {
                // TODO!

               return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Upload files failed!");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Download a nodeset from PostgreSQL
        /// </summary>
        public Task<string> DownloadNodesetAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                // TODO!

                return Task.FromResult(string.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "Download files failed!");
                return Task.FromResult(string.Empty);
            }
        }
    }
}
