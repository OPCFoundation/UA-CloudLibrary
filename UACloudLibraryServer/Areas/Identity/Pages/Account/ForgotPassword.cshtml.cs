// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Opc.Ua.Cloud.Library.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly CaptchaValidation _captchaValidation;
        private readonly CaptchaSettings _captchaSettings;

        public ForgotPasswordModel(
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            IConfiguration configuration,
            CaptchaValidation captchaValidation)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _captchaValidation = captchaValidation;

            _captchaSettings = new CaptchaSettings();
            configuration.GetSection("CaptchaSettings").Bind(_captchaSettings);
        }

        /// Populate values for cshtml to use
        /// </summary>
        public CaptchaSettings CaptchaSettings { get { return _captchaSettings; } }

        /// <summary>
        /// Populate a token returned from client side call to Google Captcha
        /// </summary>
        [BindProperty]
        public string CaptchaResponseToken { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            //Captcha validate
            string captchaResult = await _captchaValidation.ValidateCaptcha(CaptchaResponseToken);
            if (!string.IsNullOrEmpty(captchaResult)) ModelState.AddModelError("CaptchaResponseToken", captchaResult);

            if (ModelState.IsValid)
            {
                IdentityUser user = await _userManager.FindByEmailAsync(Input.Email).ConfigureAwait(false);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                string code = await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                string callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                await EmailManager.Send(
                    _emailSender,
                    Input.Email,
                    "UA Cloud Library - Reset Password",
                    "We received a request to reset your password.",
                    callbackUrl
                ).ConfigureAwait(false);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
