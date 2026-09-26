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
                            { "ServicePassword", "testpw" },
                            { "ConnectionStrings:CloudLibraryPostgreSQL", "Server=localhost;Username=testuser;Database=cloudlib_test;Port=5432;Password=password;SSLMode=Prefer;Include Error Detail=true" },
                            { "OAuth2ClientId", "Test" },
                            { "OAuth2ClientSecret", "TestSecret" },

                            // Required wherever a stable ESDC signing key is resolved: the issuer of an
                            // ESDC is otherwise taken from the DPP's own content, so the server refuses
                            // to start without an authoritative operator id rather than sign credentials
                            // asserting whatever operator a hosted passport names. The test host runs the
                            // real Startup, so it has to configure this exactly as a deployment does.
                            { "Dpp:Esdc:EconomicOperatorId", "urn:uacl:test-economic-operator" }
                        })
                );
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
