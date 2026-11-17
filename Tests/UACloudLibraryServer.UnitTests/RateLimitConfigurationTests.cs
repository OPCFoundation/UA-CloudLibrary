using System;
using System.Linq;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Pins how the DPP and REST API rate-limit permits are resolved from configuration.
    /// </summary>
    /// <remarks>
    /// <c>FixedWindowRateLimiter</c> requires a positive permit limit, but the partition factory is
    /// lazy: an invalid value does not surface when the policy is registered, it throws when the
    /// first request arrives. A configuration typo would therefore present as a runtime fault on
    /// a user request rather than a deployment that refuses to start.
    /// </remarks>
    public class RateLimitConfigurationTests
    {
        public static TheoryData<string> PermitKeys => new() {
            Opc.Ua.Cloud.Library.Startup.DppRateLimitPermitKey,
            Opc.Ua.Cloud.Library.Startup.ApiRateLimitPermitKey
        };

        private static IConfiguration Configuration(string key, string value) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string> {
                    [key] = value
                })
                .Build();

        private static int Resolve(IConfiguration configuration, string key) =>
            Opc.Ua.Cloud.Library.Startup.ResolveRateLimitPermit(configuration, key);

        [Fact]
        public void PermitKeys_AreTheDocumentedSettings()
        {
            Assert.Equal("Dpp:RateLimit:PermitPerMinute", Opc.Ua.Cloud.Library.Startup.DppRateLimitPermitKey);
            Assert.Equal("Api:RateLimit:PermitPerMinute", Opc.Ua.Cloud.Library.Startup.ApiRateLimitPermitKey);
            Assert.Equal(100, Opc.Ua.Cloud.Library.Startup.DefaultRateLimitPermitPerMinute);
        }

        [Theory]
        [MemberData(nameof(PermitKeys))]
        public void NonPositivePermit_IsRejected(string key)
        {
            foreach (string configured in new[] { "0", "-1", "-100" })
            {
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                    () => Resolve(Configuration(key, configured), key));

                // The message must name the setting, the offending value and the way out, since this
                // is the only thing an operator sees when the deployment refuses to start.
                Assert.Contains(key, ex.Message, StringComparison.Ordinal);
                Assert.Contains(configured, ex.Message, StringComparison.Ordinal);
                Assert.Contains("greater than zero", ex.Message, StringComparison.Ordinal);
            }
        }

        [Theory]
        [MemberData(nameof(PermitKeys))]
        public void PositivePermit_IsAccepted(string key)
        {
            Assert.Equal(1, Resolve(Configuration(key, "1"), key));
            Assert.Equal(100, Resolve(Configuration(key, "100"), key));
            Assert.Equal(5000, Resolve(Configuration(key, "5000"), key));
        }

        [Theory]
        [MemberData(nameof(PermitKeys))]
        public void UnsetPermit_FallsBackToTheDefault(string key)
        {
            IConfiguration empty = new ConfigurationBuilder().Build();

            Assert.Equal(100, Resolve(empty, key));
        }

        [Fact]
        public void Permits_AreResolvedIndependently()
        {
            IConfiguration configuration = Configuration(Opc.Ua.Cloud.Library.Startup.ApiRateLimitPermitKey, "250");

            Assert.Equal(250, Resolve(configuration, Opc.Ua.Cloud.Library.Startup.ApiRateLimitPermitKey));
            Assert.Equal(100, Resolve(configuration, Opc.Ua.Cloud.Library.Startup.DppRateLimitPermitKey));
        }

        [Fact]
        public void EveryApiController_IsRateLimited()
        {
            System.Type[] apiControllers = typeof(Opc.Ua.Cloud.Library.Startup).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && t.IsDefined(typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute), inherit: true))
                .ToArray();

            Assert.NotEmpty(apiControllers);
            foreach (System.Type controller in apiControllers)
            {
                Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute attribute = controller
                    .GetCustomAttributes(typeof(Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute), inherit: true)
                    .Cast<Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute>()
                    .SingleOrDefault();

                Assert.True(attribute != null, $"{controller.Name} has no [EnableRateLimiting] policy.");
                Assert.Contains(attribute.PolicyName, new[] { Opc.Ua.Cloud.Library.Startup.DppRateLimitPolicy, Opc.Ua.Cloud.Library.Startup.ApiRateLimitPolicy });
            }
        }
    }
}
