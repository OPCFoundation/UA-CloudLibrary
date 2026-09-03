using System.Reflection;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;

using Opc.Ua.Cloud.Library.Authentication;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="UserIdentifierDisplayMetadataProvider"/>: the account identifier field
    /// is captioned "Username" while e-mail verification is disabled and "Email" otherwise, so labels
    /// and validation messages agree. Properties without the marker attribute are left alone.
    /// </summary>
    public class UserIdentifierDisplayMetadataProviderTests
    {
        private sealed class Holder
        {
            [EmailAddressWhenVerificationEnabled]
            public string Email { get; set; }

            public string Unrelated { get; set; }
        }

        private static DisplayMetadata BuildMetadata(string propertyName, bool requireConfirmedAccount)
        {
            var identityOptions = new IdentityOptions();
            identityOptions.SignIn.RequireConfirmedAccount = requireConfirmedAccount;
            var provider = new UserIdentifierDisplayMetadataProvider(Options.Create(identityOptions));

            PropertyInfo property = typeof(Holder).GetProperty(propertyName);
            ModelMetadataIdentity key = ModelMetadataIdentity.ForProperty(property, typeof(string), typeof(Holder));
            ModelAttributes attributes = ModelAttributes.GetAttributesForProperty(typeof(Holder), property);
            var context = new DisplayMetadataProviderContext(key, attributes);

            provider.CreateDisplayMetadata(context);
            return context.DisplayMetadata;
        }

        [Fact]
        public void DisplayName_IsUsername_WhenVerificationDisabled()
        {
            DisplayMetadata metadata = BuildMetadata(nameof(Holder.Email), requireConfirmedAccount: false);

            Assert.Equal("Username", metadata.DisplayName());
        }

        [Fact]
        public void DisplayName_IsEmail_WhenVerificationEnabled()
        {
            DisplayMetadata metadata = BuildMetadata(nameof(Holder.Email), requireConfirmedAccount: true);

            Assert.Equal("Email", metadata.DisplayName());
        }

        [Fact]
        public void UnrelatedProperty_IsNotGivenADisplayName()
        {
            DisplayMetadata metadata = BuildMetadata(nameof(Holder.Unrelated), requireConfirmedAccount: false);

            Assert.Null(metadata.DisplayName);
        }
    }
}
