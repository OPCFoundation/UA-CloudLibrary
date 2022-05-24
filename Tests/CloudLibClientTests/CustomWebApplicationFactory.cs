using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Opc.Ua.Cloud.Library.Client;

namespace CloudLibClient.Tests
{
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override IHostBuilder CreateHostBuilder()
        {
            return base.CreateHostBuilder()
                .ConfigureHostConfiguration(
                    config => config.AddEnvironmentVariables("ASPNETCORE")
                        .AddInMemoryCollection(new Dictionary<string, string>
                        {
                            { "ServicePassword", "testpw" },
                            { "ConnectionStrings:CloudLibraryPostgreSQL", "Server=localhost;Username=testuser;Database=cloudlib_test;Port=5432;Password=password;SSLMode=Prefer;Include Error Detail=true" },
                        }))
                        ;
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
            });
        }

        internal UACloudLibClient CreateCloudLibClient()
        {
            var httpClient = CreateAuthorizedClient();
            var client = new UACloudLibClient(httpClient);
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
