using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Opc.Ua.Cloud.Library.Client;

namespace CloudLibClient.Tests
{
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        public class IntegrationTestConfig
        {
            public bool IgnoreUploadConflict { get; set; }
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
                            { "CloudLibrary:ApprovalRequired", "false" },
                            { "OAuth2ClientId", "Test" },
                            { "OAuth2ClientSecret", "TestSecret" }

                        }))
                        ;
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services => {
            });
        }

        internal UACloudLibClient CreateCloudLibClient()
        {
            var httpClient = CreateAuthorizedClient();
            var client = new UACloudLibClient(httpClient);
            // Ensure all test cases hit GraphQL. Set to true in the test case if explicitly testing fallbacks
            client._allowRestFallback = false;
            return client;
        }
        internal HttpClient CreateAuthorizedClient()
        {
            var httpClient = CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { });

            string temp = Convert.ToBase64String(Encoding.UTF8.GetBytes("admin" + ":" + "testpw"));
            httpClient.DefaultRequestHeaders.Add("Authorization", "basic " + temp);

            return httpClient;
        }

    }
}
