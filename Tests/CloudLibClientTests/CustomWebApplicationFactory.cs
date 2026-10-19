using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Opc.Ua.Cloud.Client;

namespace CloudLibClient.Tests
{
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        /// <summary>
        /// Environment variable (process or repository <c>.env</c> file) with a complete connection
        /// string that overrides the database used by the integration tests. The generic
        /// <c>ConnectionStrings__CloudLibraryPostgreSQL</c> variable is deliberately ignored
        /// because docker-compose sets it to the Docker-internal host name "db", which does
        /// not resolve on the host and breaks local test runs ("No such host is known").
        /// </summary>
        public const string TestConnectionStringVariable = "CLOUDLIB_TEST_CONNECTIONSTRING";

        /// <summary>
        /// Optional host port of the docker-compose Postgres container (process or <c>.env</c>).
        /// Defaults to the port docker-compose.yml publishes (5433).
        /// </summary>
        public const string TestDbPortVariable = "CLOUDLIB_TEST_DB_PORT";

        // Matches the database the GitHub Actions workflow provisions on the runner.
        private const string DefaultTestConnectionString =
            "Server=localhost;Username=testuser;Database=cloudlib_test;Port=5432;Password=password;SSLMode=Prefer;Include Error Detail=true";

        // Resolution order:
        //  1. CLOUDLIB_TEST_CONNECTIONSTRING (process environment, then .env)
        //  2. the docker-compose POSTGRESQL_* settings from .env, reached via localhost and the
        //     published host port (the compose-internal host name "db" is not usable from the host)
        //  3. the database provisioned by the GitHub Actions workflow
        private static string ResolveTestConnectionString()
        {
            string explicitValue = DotEnvFile.Get(TestConnectionStringVariable);
            if (!string.IsNullOrWhiteSpace(explicitValue))
            {
                return explicitValue;
            }

            string username = DotEnvFile.Get("POSTGRESQL_USERNAME");
            string password = DotEnvFile.Get("POSTGRESQL_PASSWORD");
            string database = DotEnvFile.Get("POSTGRESQL_DATABASE");

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(database))
            {
                string port = DotEnvFile.Get(TestDbPortVariable) ?? "5433";
                return $"Server=localhost;Port={port};Username={username};Password={password};Database={database};SSLMode=Prefer;Include Error Detail=true";
            }

            return DefaultTestConnectionString;
        }
        public class IntegrationTestConfig
        {
            public bool IgnoreUploadConflict { get; set; } = true;

            public bool DeleteCloudLibDBAndStore { get; set; }
        }

        private IntegrationTestConfig _testConfig;

        public IntegrationTestConfig TestConfig
        {
            get
            {
                if (_testConfig == null)
                {
                    IntegrationTestConfig testConfig = new();
                    Services.GetService<IConfiguration>()?.Bind("IntegrationTest", testConfig);
                    _testConfig = testConfig;
                }
                return _testConfig;
            }
        }

        protected override IHostBuilder CreateHostBuilder()
        {
            return base.CreateHostBuilder()
                .ConfigureHostConfiguration(
                    config => config.AddEnvironmentVariables("ASPNETCORE")
                        .AddInMemoryCollection(new Dictionary<string, string>
                        {
                            { "ServiceUsername", "admin" },
                            { "ServicePassword", "testpw" },
                            { "OAuth2ClientId", "Test" },
                            { "OAuth2ClientSecret", "TestSecret" },

                            // Required wherever a stable ESDC signing key is resolved: the issuer of an
                            // ESDC is otherwise taken from the DPP's own content, so the server refuses
                            // to start without an authoritative operator id rather than sign credentials
                            // asserting whatever operator a hosted passport names. The test host runs the
                            // real Startup, so it has to configure this exactly as a deployment does.
                            { "Dpp:Esdc:EconomicOperatorId", "urn:uacl:test-economic-operator" }
                        })
                )
                // Host.CreateDefaultBuilder layers appsettings.json and process environment variables
                // on top of the host configuration above, so a connection string placed there can be
                // silently replaced by whatever ConnectionStrings__CloudLibraryPostgreSQL happens to be
                // set in the developer's environment. Registering it here, after the default sources,
                // makes the test database authoritative for the test host.
                .ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "ConnectionStrings:CloudLibraryPostgreSQL", ResolveTestConnectionString() }
                    }));
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services => {
            });
        }

        internal UACloudLibClient CreateCloudLibClient()
        {
            HttpClient httpClient = CreateAuthorizedClient();
            var client = new UACloudLibClient(httpClient);
            return client;
        }
        internal HttpClient CreateAuthorizedClient()
        {
            HttpClient httpClient = CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { });

            string temp = Convert.ToBase64String(Encoding.UTF8.GetBytes("admin" + ":" + "testpw"));
            httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + temp);

            return httpClient;
        }

    }
}
