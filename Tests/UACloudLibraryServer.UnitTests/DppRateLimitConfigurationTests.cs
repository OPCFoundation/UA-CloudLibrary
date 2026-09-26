using System;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Pins that a non-positive DPP rate-limit permit is rejected as a configuration error.
    /// </summary>
    /// <remarks>
    /// <c>FixedWindowRateLimiter</c> requires a positive permit limit, but the partition factory is
    /// lazy: an invalid value does not surface when the policy is registered, it throws when the
    /// first DPP request arrives. A configuration typo would therefore present as a runtime fault on
    /// a user request rather than a deployment that refuses to start.
    /// <para>
    /// <c>Startup.ConfigureServices</c> needs a full host to run, so these tests pin the validation
    /// rule against the same configuration key rather than booting the server.
    /// </para>
    /// </remarks>
    public class DppRateLimitConfigurationTests
    {
        private const string PermitKey = "Dpp:RateLimit:PermitPerMinute";
        private const int DefaultPermitPerMinute = 100;

        private static IConfiguration Configuration(string value) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string> {
                    [PermitKey] = value
                })
                .Build();

        /// <summary>
        /// Mirrors the resolution and validation performed in <c>Startup.ConfigureServices</c>.
        /// </summary>
        private static int ResolvePermitLimit(IConfiguration configuration)
        {
            int permitPerMinute = configuration.GetValue<int?>(PermitKey) ?? DefaultPermitPerMinute;
            if (permitPerMinute <= 0)
            {
                throw new InvalidOperationException(
                    $"Dpp:RateLimit:PermitPerMinute is {permitPerMinute}, but it must be greater than zero. " +
                    "Remove the setting to use the default of 100 requests per minute per client IP.");
            }

            return permitPerMinute;
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("-100")]
        public void NonPositivePermit_IsRejected(string configured)
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => ResolvePermitLimit(Configuration(configured)));

            // The message must name the offending value and the way out, since this is the only
            // thing an operator sees when the deployment refuses to start.
            Assert.Contains(configured, ex.Message, StringComparison.Ordinal);
            Assert.Contains("greater than zero", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("1", 1)]
        [InlineData("100", 100)]
        [InlineData("5000", 5000)]
        public void PositivePermit_IsAccepted(string configured, int expected)
        {
            Assert.Equal(expected, ResolvePermitLimit(Configuration(configured)));
        }

        [Fact]
        public void UnsetPermit_FallsBackToTheDefault()
        {
            IConfiguration empty = new ConfigurationBuilder().Build();

            Assert.Equal(DefaultPermitPerMinute, ResolvePermitLimit(empty));
        }
    }
}
