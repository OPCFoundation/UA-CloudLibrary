using System;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

using Opc.Ua.Cloud.Library.Authentication;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="EmailAddressWhenVerificationEnabledAttribute"/>: e-mail syntax is only
    /// enforced while e-mail verification is enabled, so that deployments without an e-mail sender can
    /// register plain user names.
    /// </summary>
    public class EmailAddressWhenVerificationEnabledAttributeTests
    {
        // Minimal service provider exposing just the IdentityOptions the attribute resolves.
        private sealed class IdentityOptionsProvider : IServiceProvider
        {
            private readonly IOptions<IdentityOptions> _options;

            public IdentityOptionsProvider(bool requireConfirmedAccount)
            {
                var identityOptions = new IdentityOptions();
                identityOptions.SignIn.RequireConfirmedAccount = requireConfirmedAccount;
                _options = Options.Create(identityOptions);
            }

            public object GetService(Type serviceType) =>
                serviceType == typeof(IOptions<IdentityOptions>) ? _options : null;
        }

        private static ValidationResult Validate(string value, bool verificationEnabled, bool resolvableOptions = true)
        {
            var attribute = new EmailAddressWhenVerificationEnabledAttribute();
            IServiceProvider provider = resolvableOptions ? new IdentityOptionsProvider(verificationEnabled) : null;
            var context = new ValidationContext(new object(), provider, null) {
                DisplayName = "Email",
                MemberName = "Email"
            };

            return attribute.GetValidationResult(value, context);
        }

        [Theory]
        [InlineData("plainusername")]
        [InlineData("alice")]
        [InlineData("not-an-email")]
        public void PlainUserName_IsAccepted_WhenVerificationDisabled(string value)
        {
            Assert.Null(Validate(value, verificationEnabled: false));
        }

        [Theory]
        [InlineData("plainusername")]
        [InlineData("not-an-email")]
        public void PlainUserName_IsRejected_WhenVerificationEnabled(string value)
        {
            ValidationResult result = Validate(value, verificationEnabled: true);

            Assert.NotNull(result);
            Assert.Contains("Email", result.ErrorMessage, StringComparison.Ordinal);
        }

        [Fact]
        public void ValidEmail_IsAccepted_RegardlessOfVerificationSetting()
        {
            Assert.Null(Validate("user@example.com", verificationEnabled: true));
            Assert.Null(Validate("user@example.com", verificationEnabled: false));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void EmptyValue_IsLeftTo_RequiredAttribute(string value)
        {
            Assert.Null(Validate(value, verificationEnabled: true));
        }

        [Fact]
        public void UnresolvableOptions_FallBackToStrictValidation()
        {
            // Fail safe: without IdentityOptions the stricter e-mail syntax check must still apply.
            Assert.NotNull(Validate("plainusername", verificationEnabled: false, resolvableOptions: false));
            Assert.Null(Validate("user@example.com", verificationEnabled: false, resolvableOptions: false));
        }
    }
}
