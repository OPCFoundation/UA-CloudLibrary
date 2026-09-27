using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="EsdcSigningKeyProvider"/>'s resolution rules. Covers the paths that
    /// complete before any database access: a configured key is returned as-is, and the
    /// database-generated fallback is refused outside Development unless explicitly permitted.
    /// </summary>
    public class EsdcSigningKeyProviderTests
    {
        private sealed class StubEnvironment : IWebHostEnvironment
        {
            public string EnvironmentName { get; set; }
            public string ApplicationName { get; set; } = "Tests";
            public string WebRootPath { get; set; }
            public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; }
            public string ContentRootPath { get; set; }
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
        }

        private static EsdcSigningKeyProvider CreateProvider(string environmentName, Dictionary<string, string> settings)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            // The DbContext is deliberately null: every case under test must return or throw before
            // reaching the database, which is itself part of what is being asserted.
            return new EsdcSigningKeyProvider(
                null,
                configuration,
                new StubEnvironment { EnvironmentName = environmentName },
                NullLoggerFactory.Instance);
        }

        [Theory]
        [InlineData("Production")]
        [InlineData("Staging")]
        public async Task ConfiguredKey_IsUsedWithoutTouchingTheDatabase(string environmentName)
        {
            const string pem = "-----BEGIN PRIVATE KEY-----\nMIIB\n-----END PRIVATE KEY-----";

            EsdcSigningKeyProvider provider = CreateProvider(environmentName, new Dictionary<string, string> {
                [EsdcSigningKeyProvider.PrivateKeyConfigurationPath] = pem
            });

            Assert.Equal(pem, await provider.GetOrCreatePrivateKeyPemAsync().ConfigureAwait(true));
        }

        [Theory]
        [InlineData("Production")]
        [InlineData("Staging")]
        public async Task MissingKey_OutsideDevelopment_IsRefused(string environmentName)
        {
            EsdcSigningKeyProvider provider = CreateProvider(environmentName, new Dictionary<string, string>());

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.GetOrCreatePrivateKeyPemAsync()).ConfigureAwait(true);

            // The message has to tell an operator how to resolve it, both properly and as an override.
            Assert.Contains(EsdcSigningKeyProvider.PrivateKeyConfigurationPath, ex.Message, StringComparison.Ordinal);
            Assert.Contains(EsdcSigningKeyProvider.AllowGeneratedKeyConfigurationPath, ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task MissingKey_OutsideDevelopment_IsAllowedWhenOptedIn()
        {
            EsdcSigningKeyProvider provider = CreateProvider("Production", new Dictionary<string, string> {
                [EsdcSigningKeyProvider.AllowGeneratedKeyConfigurationPath] = "true"
            });

            // Opting in must get past the refusal and reach the database (null here, hence the throw).
            // Asserting on the message distinguishes "went to the database" from "was refused", which a
            // bare exception-type assertion could not.
            Exception ex = await Record.ExceptionAsync(
                () => provider.GetOrCreatePrivateKeyPemAsync()).ConfigureAwait(true);

            Assert.NotNull(ex);
            Assert.DoesNotContain(
                EsdcSigningKeyProvider.AllowGeneratedKeyConfigurationPath,
                ex.Message,
                StringComparison.Ordinal);
        }

        [Fact]
        public async Task MissingKey_InDevelopment_IsNotRefused()
        {
            EsdcSigningKeyProvider provider = CreateProvider("Development", new Dictionary<string, string>());

            // Development keeps working with no configuration at all, so the guard must not fire.
            Exception ex = await Record.ExceptionAsync(
                () => provider.GetOrCreatePrivateKeyPemAsync()).ConfigureAwait(true);

            Assert.NotNull(ex);
            Assert.DoesNotContain(
                EsdcSigningKeyProvider.AllowGeneratedKeyConfigurationPath,
                ex.Message,
                StringComparison.Ordinal);
        }
    }
}
