using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;

namespace Opc.Ua.Cloud.Library.Authentication
{
    /// <summary>
    /// Supplies the display name for the account identifier field, so that both its label and its
    /// validation messages ("The {0} field is required.") come from a single source. Any property
    /// annotated with <see cref="EmailAddressWhenVerificationEnabledAttribute"/> is, by definition, the
    /// field that accepts an e-mail address or - when e-mail verification is disabled - a plain user
    /// name, so it is labelled accordingly.
    /// </summary>
    /// <remarks>
    /// <see cref="DisplayMetadata.DisplayName"/> is a delegate, so the caption is resolved per render
    /// rather than baked into the cached metadata. This provider must be appended after the built-in
    /// data-annotations provider so that it overrides any <c>[Display]</c> name.
    /// </remarks>
    public sealed class UserIdentifierDisplayMetadataProvider : IDisplayMetadataProvider
    {
        private readonly IOptions<IdentityOptions> _identityOptions;

        public UserIdentifierDisplayMetadataProvider(IOptions<IdentityOptions> identityOptions)
        {
            _identityOptions = identityOptions;
        }

        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            if (context is null || !context.Attributes.OfType<EmailAddressWhenVerificationEnabledAttribute>().Any())
            {
                return;
            }

            context.DisplayMetadata.DisplayName = () =>
                _identityOptions.Value.SignIn.RequireConfirmedAccount ? "Email" : "Username";
        }
    }
}
