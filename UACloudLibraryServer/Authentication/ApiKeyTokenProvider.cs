/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Opc.Ua.Cloud.Library.Authentication
{
    public class ApiKeyTokenProvider : IUserTwoFactorTokenProvider<IdentityUser>
    {
        public ApiKeyTokenProvider(AppDbContext appDbContext, ILogger<ApiKeyTokenProvider> logger)
        {
            _appDbContext = appDbContext;
            _logger = logger;
        }
        public const string ApiKeyProviderName = nameof(ApiKeyTokenProvider);

        private readonly AppDbContext _appDbContext;
        private readonly ILogger<ApiKeyTokenProvider> _logger;

        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<IdentityUser> manager, IdentityUser user)
        {
            return Task.FromResult(true);
        }

        public Task<string> GenerateAsync(string purpose, UserManager<IdentityUser> manager, IdentityUser user)
        {
            if (!string.IsNullOrEmpty(purpose))
            {
                var secretBytes = RandomNumberGenerator.GetBytes(32);

                // Make it Base64URL
                var apiKey = Convert.ToBase64String(secretBytes).Replace("+", "-", StringComparison.Ordinal).Replace("/", "_", StringComparison.Ordinal);
                return Task.FromResult(apiKey);
            }
            throw new ArgumentException($"Unknown purpose {purpose}.");
        }

        public async Task<bool> ValidateAsync(string purpose, string token, UserManager<IdentityUser> manager, IdentityUser user)
        {
            if (!string.IsNullOrEmpty(purpose))
            {
                var authTokenHash = await manager.GetAuthenticationTokenAsync(user, ApiKeyProviderName, purpose).ConfigureAwait(false);
                if (authTokenHash == null || authTokenHash.Length < 4)
                {
                    return false;
                }
                var result = manager.PasswordHasher.VerifyHashedPassword(user, authTokenHash.Substring(4), token);
                if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    return true;
                }
            }
            return false;
        }

        static Dictionary<string, (string UserId, string apiKeyName)> _apiKeyToUserMap = new();

        public async Task<(string UserId, string ApiKeyName)> FindUserForApiKey(string apiKey, UserManager<IdentityUser> manager)
        {
            if (apiKey.Length < 4)
            {
                throw new ArgumentException($"Invalid API key format");
            }
            // Don't keep the full API key in memory
            var partialApiKey = apiKey.Substring(0, apiKey.Length - 16);
            if (_apiKeyToUserMap.TryGetValue(partialApiKey, out var cachedUserAndKeyName) && cachedUserAndKeyName.UserId != "collision")
            {
                return cachedUserAndKeyName;
            }
            var prefix = apiKey.Substring(0, 4);
            var candidateTokens = await _appDbContext.UserTokens.Where(t => t.LoginProvider == ApiKeyTokenProvider.ApiKeyProviderName && t.Value.StartsWith(prefix)).ToListAsync().ConfigureAwait(false);

            foreach (var candidateToken in candidateTokens)
            {
                var user = await manager.FindByIdAsync(candidateToken.UserId).ConfigureAwait(false);
                var result = manager.PasswordHasher.VerifyHashedPassword(user, candidateToken.Value.Substring(4), apiKey);
                if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    var newUserAndKeyName = (user.Id, candidateToken.Name);

                    if (cachedUserAndKeyName.UserId != "collision" && !_apiKeyToUserMap.TryAdd(partialApiKey, newUserAndKeyName))
                    {
                        // Key prefix collision: stop using the cache for this key
                        _logger.LogWarning("APIKey cache collision detected: disabled caching for the colliding keys.");
                        _apiKeyToUserMap[partialApiKey] = ("collision", null);
                    }
                    return newUserAndKeyName;
                }
            }
            throw new ArgumentException($"Key not found");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="userManager">UserManager to use to generate and set the key.</param>
        /// <param name="user">User for who to generate the key for.</param>
        /// <param name="newApiKeyName">Name under which the key will be stored.</param>
        /// <returns>The generated api key,or null if a key with the name already exists.</returns>
        /// <exception cref="ApiKeyGenerationException"></exception>
        public static async Task<string> GenerateAndSetAuthenticationTokenAsync(UserManager<IdentityUser> userManager, IdentityUser user, string newApiKeyName)
        {
            var existingToken = await userManager.GetAuthenticationTokenAsync(user, ApiKeyProviderName, newApiKeyName).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(existingToken))
            {
                return null;
            }

            var newApiKey = await userManager.GenerateUserTokenAsync(user, ApiKeyProviderName, newApiKeyName).ConfigureAwait(false);
            // Store the first 4 bytes of the unhashed key for more efficient key lookup
            var newApiKeyHash = $"{newApiKey.Substring(0, 4)}{userManager.PasswordHasher.HashPassword(user, newApiKey)}";

            var setTokenResult = await userManager.SetAuthenticationTokenAsync(user, ApiKeyProviderName, newApiKeyName, newApiKeyHash).ConfigureAwait(false);
            if (!setTokenResult.Succeeded)
            {
                throw new ApiKeyGenerationException("Error saving key.", setTokenResult.Errors.Select(e => e.Description).ToList());
            }
            return newApiKey;
        }

        public async Task<List<(string KeyName, string KeyPrefix)>> GetUserApiKeysAsync(IdentityUser user)
        {
            var tokens = await _appDbContext.UserTokens.Where(t => t.UserId == user.Id && t.LoginProvider == ApiKeyTokenProvider.ApiKeyProviderName).ToListAsync().ConfigureAwait(false);
            return tokens.Select(t => (t.Name, t.Value.Substring(0, 4))).ToList();
        }
    }

    [Serializable]
    internal class ApiKeyGenerationException : Exception
    {
        public IEnumerable<string> Errors { get; private set; }

        public ApiKeyGenerationException() { }

        public ApiKeyGenerationException(string message, IEnumerable<string> enumerable) : base(message)
        {
            Errors = enumerable;
        }

        public ApiKeyGenerationException(string message) : base(message) { }

        public ApiKeyGenerationException(string message, Exception innerException) : base(message, innerException) { }

        protected ApiKeyGenerationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
