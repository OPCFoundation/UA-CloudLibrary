// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Authentication;

namespace Opc.Ua.Cloud.Library.Areas.Identity.Pages.Account.Manage
{
    public class ManageApiKeysModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApiKeyTokenProvider _apiKeyTokenProvider;

        public ManageApiKeysModel(
            UserManager<IdentityUser> userManager,
            ApiKeyTokenProvider apiKeyTokenProvider)
        {
            _userManager = userManager;
            _apiKeyTokenProvider = apiKeyTokenProvider;
        }

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
        [TempData]
        public string StatusMessage { get; set; }
        public List<(string KeyName, string KeyPrefix)> ApiKeysAndNames { get; private set; }

        [TempData]
        public string GeneratedApiKeyName { get; set; }
        [TempData]
        public string GeneratedApiKey { get; set; }

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
            [DataType(DataType.Text)]
            [Display(Name = "API Key Name")]
            public string NewApiKeyName { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            ApiKeysAndNames = await _apiKeyTokenProvider.GetUserApiKeysAsync(user).ConfigureAwait(false);
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteApiKeyAsync(string apiKeyToDelete)
        {
            var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var removeResult = await _userManager.RemoveAuthenticationTokenAsync(user, ApiKeyTokenProvider.ApiKeyProviderName, apiKeyToDelete).ConfigureAwait(false);
            if (!removeResult.Succeeded)
            {
                foreach (var error in removeResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }
            StatusMessage = $"Deleted API key {apiKeyToDelete}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
                if (user == null)
                {
                    return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
                }

                var newApiKeyName = Input.NewApiKeyName;

                try
                {
                    var newApiKey = await ApiKeyTokenProvider.GenerateAndSetAuthenticationTokenAsync(_userManager, user, newApiKeyName).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(newApiKey))
                    {
                        ModelState.AddModelError(string.Empty, "A key with this name already exists.");
                        ApiKeysAndNames = await _apiKeyTokenProvider.GetUserApiKeysAsync(user).ConfigureAwait(false);
                        return Page();
                    }
                    GeneratedApiKeyName = newApiKeyName;
                    GeneratedApiKey = newApiKey;
                    StatusMessage = $"Be sure to save the API key before you leave this page. You will not be able to retrieve it later.";
                }
                catch (ApiKeyGenerationException ex)
                {
                    foreach (var error in ex.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                    return Page();
                }

            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Error generating key.");
            }

            return RedirectToPage();
        }
    }
}
