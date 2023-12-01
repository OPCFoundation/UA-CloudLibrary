// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Opc.Ua.Cloud.Library.Authentication;

namespace Opc.Ua.Cloud.Library.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly Interfaces.ICaptchaValidation _captchaValidation;
        private readonly CaptchaSettings _captchaSettings;

        public EmailModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IEmailSender emailSender,
            IConfiguration configuration,
            Interfaces.ICaptchaValidation captchaValidation)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _captchaValidation = captchaValidation;

            _captchaSettings = new CaptchaSettings();
            configuration.GetSection("CaptchaSettings").Bind(_captchaSettings);
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool IsEmailConfirmed { get; set; }

        /// <summary>
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
        [TempData]
        public string StatusMessage { get; set; }

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
            [Display(Name = "New email")]
            public string NewEmail { get; set; }
        }

        private async Task LoadAsync(IdentityUser user)
        {
            var email = await _userManager.GetEmailAsync(user).ConfigureAwait(false);
            Email = email;

            Input = new InputModel {
                NewEmail = email,
            };

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user).ConfigureAwait(false);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var captchaResult = await _captchaValidation.ValidateCaptcha(CaptchaResponseToken);
            if (!string.IsNullOrEmpty(captchaResult)) ModelState.AddModelError("CaptchaResponseToken", captchaResult);

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var email = await _userManager.GetEmailAsync(user).ConfigureAwait(false);
            if (Input.NewEmail != email)
            {
                var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail).ConfigureAwait(false);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmailChange",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, email = Input.NewEmail, code = code },
                    protocol: Request.Scheme);

                await EmailManager.SendConfirmEmailChange(
                    _emailSender,
                    Input.NewEmail,
                    callbackUrl
                ).ConfigureAwait(false);

                StatusMessage = "Confirmation link to change email sent. Please check your email.";
                return RedirectToPage();
            }

            StatusMessage = "Your email is unchanged.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            //Captcha validate
            var captchaResult = await _captchaValidation.ValidateCaptcha(CaptchaResponseToken);
            if (!string.IsNullOrEmpty(captchaResult)) ModelState.AddModelError("CaptchaResponseToken", captchaResult);

            if (!ModelState.IsValid)
            {
                await LoadAsync(user).ConfigureAwait(false);
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
            var email = await _userManager.GetEmailAsync(user).ConfigureAwait(false);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = code },
                protocol: Request.Scheme);

            await EmailManager.SendReconfirmEmail(
                _emailSender,
                Input.NewEmail,
                callbackUrl
            ).ConfigureAwait(false);

            StatusMessage = "Verification email sent. Please check your email.";
            return RedirectToPage();
        }
    }
}
