using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Opc.Ua.Cloud.Library.Authentication
{
    /// <summary>
    /// Validates that a value is a syntactically valid e-mail address, but only while e-mail
    /// verification is enabled. This deployment ties e-mail verification to
    /// <see cref="SignInOptions.RequireConfirmedAccount"/>, which <c>Startup</c> sets from the presence
    /// of an <c>EmailSenderAPIKey</c>. When no key is configured no confirmation mail can be sent
    /// anyway, so accounts may be registered (and signed in to) with a plain user name instead of an
    /// e-mail address.
    /// </summary>
    /// <remarks>
    /// Deliberately not derived from <see cref="EmailAddressAttribute"/> (a <c>DataTypeAttribute</c>):
    /// deriving from it would make the input tag helper render <c>type="email"</c> and emit a
    /// <c>data-val-email</c> rule, so the browser would reject plain user names before the request ever
    /// reached the server, regardless of this attribute's decision.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class EmailAddressWhenVerificationEnabledAttribute : ValidationAttribute
    {
        private static readonly EmailAddressAttribute s_emailAddress = new();

        public EmailAddressWhenVerificationEnabledAttribute()
            : base("The {0} field is not a valid e-mail address.")
        {
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // Presence is the concern of [Required]; an empty value is not an e-mail syntax error.
            if (value is not string input || string.IsNullOrWhiteSpace(input))
            {
                return ValidationResult.Success;
            }

            if (!IsEmailVerificationEnabled(validationContext) || s_emailAddress.IsValid(input))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(
                FormatErrorMessage(validationContext?.DisplayName),
                validationContext?.MemberName is null ? null : new[] { validationContext.MemberName });
        }

        private static bool IsEmailVerificationEnabled(ValidationContext validationContext)
        {
            // Fail safe: when the options cannot be resolved, keep the stricter e-mail syntax check.
            var options = validationContext?.GetService(typeof(IOptions<IdentityOptions>)) as IOptions<IdentityOptions>;
            return options?.Value?.SignIn?.RequireConfirmedAccount ?? true;
        }
    }
}
