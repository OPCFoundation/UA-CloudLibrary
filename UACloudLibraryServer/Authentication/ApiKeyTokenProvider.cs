/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
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
                byte[] secretBytes = RandomNumberGenerator.GetBytes(32);

                // Make it Base64URL
                string apiKey = Convert.ToBase64String(secretBytes).Replace("+", "-", StringComparison.Ordinal).Replace("/", "_", StringComparison.Ordinal);
                return Task.FromResult(apiKey);
            }
            throw new ArgumentException($"Unknown purpose {purpose}.");
        }

        public async Task<bool> ValidateAsync(string purpose, string token, UserManager<IdentityUser> manager, IdentityUser user)
        {
            // Delay validation by 150ms to mitigate DOS/brute-force attacks
            await Task.Delay(150).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(purpose))
            {
                string authTokenHash = await manager.GetAuthenticationTokenAsync(user, ApiKeyProviderName, purpose).ConfigureAwait(false);
                if (authTokenHash == null || authTokenHash.Length < 4)
                {
                    return false;
                }

                // Extract the hash part (everything before the metadata separator '|')
                int metadataSeparatorIndex = authTokenHash.IndexOf('|');
                string hashPart = metadataSeparatorIndex > 0 
                    ? authTokenHash.Substring(0, metadataSeparatorIndex) 
                    : authTokenHash;

                // Verify the hash
                PasswordVerificationResult result = manager.PasswordHasher.VerifyHashedPassword(user, hashPart.Substring(4), token);
                if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    // Check if the key has expired
                    if (metadataSeparatorIndex > 0)
                    {
                        string metadata = authTokenHash.Substring(metadataSeparatorIndex);
                        if (IsApiKeyExpired(metadata))
                        {
                            _logger.LogWarning($"API key '{purpose}' for user '{user.UserName}' has expired.");
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if an API key has expired based on its metadata.
        /// </summary>
        /// <param name="metadata">The metadata string containing expiration information.</param>
        /// <returns>True if the key has expired, false otherwise.</returns>
        private bool IsApiKeyExpired(string metadata)
        {
            // Extract ExpiresAt value from metadata
            // Format: |Type:Read-Write|Expiration:30 Days|ExpiresAt:2025-02-15T10:30:00.0000000Z
            int expiresAtIndex = metadata.IndexOf("|ExpiresAt:");
            if (expiresAtIndex < 0)
            {
                // No expiration date means unlimited
                return false;
            }

            string expiresAtPart = metadata.Substring(expiresAtIndex + "|ExpiresAt:".Length);
            int nextSeparator = expiresAtPart.IndexOf('|');
            string expiresAtString = nextSeparator > 0 
                ? expiresAtPart.Substring(0, nextSeparator) 
                : expiresAtPart;

            if (DateTime.TryParse(expiresAtString, out DateTime expirationDate))
            {
                return DateTime.UtcNow > expirationDate;
            }

            // If we can't parse the date, assume not expired for safety
            return false;
        }

        static Dictionary<string, (string UserId, string apiKeyName)> _apiKeyToUserMap = new();

        public async Task<(string UserId, string ApiKeyName)> FindUserForApiKey(string apiKey, UserManager<IdentityUser> manager)
        {
            // Delay validation by 150ms to mitigate DOS/brute-force attacks
            await Task.Delay(150).ConfigureAwait(false);

            if (apiKey.Length < 4)
            {
                throw new ArgumentException($"Invalid API key format");
            }
            // Don't keep the full API key in memory
            string partialApiKey = apiKey.Substring(0, apiKey.Length - 16);
            if (_apiKeyToUserMap.TryGetValue(partialApiKey, out (string UserId, string apiKeyName) cachedUserAndKeyName) && cachedUserAndKeyName.UserId != "collision")
            {
                return cachedUserAndKeyName;
            }
            string prefix = apiKey.Substring(0, 4);
            List<IdentityUserToken<string>> candidateTokens = await _appDbContext.UserTokens.Where(t => t.LoginProvider == ApiKeyTokenProvider.ApiKeyProviderName && t.Value.StartsWith(prefix)).ToListAsync().ConfigureAwait(false);

            foreach (IdentityUserToken<string> candidateToken in candidateTokens)
            {
                IdentityUser user = await manager.FindByIdAsync(candidateToken.UserId).ConfigureAwait(false);

                // Extract the hash part (everything before the metadata separator '|')
                string tokenValue = candidateToken.Value;
                int metadataSeparatorIndex = tokenValue.IndexOf('|');
                string hashPart = metadataSeparatorIndex > 0 
                    ? tokenValue.Substring(0, metadataSeparatorIndex) 
                    : tokenValue;

                PasswordVerificationResult result = manager.PasswordHasher.VerifyHashedPassword(user, hashPart.Substring(4), apiKey);
                if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    // Check if the key has expired
                    if (metadataSeparatorIndex > 0)
                    {
                        string metadata = tokenValue.Substring(metadataSeparatorIndex);
                        if (IsApiKeyExpired(metadata))
                        {
                            _logger.LogWarning($"API key '{candidateToken.Name}' for user '{user.UserName}' has expired.");
                            throw new ArgumentException($"API key has expired");
                        }
                    }

                    (string Id, string Name) newUserAndKeyName = (user.Id, candidateToken.Name);

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
        /// Generates and sets an authentication token (API key) for a user.
        /// </summary>
        /// <param name="userManager">UserManager to use to generate and set the key.</param>
        /// <param name="user">User for who to generate the key for.</param>
        /// <param name="newApiKeyName">Name under which the key will be stored.</param>
        /// <param name="apiKeyType">Type of the API key (Read-Only or Read-Write).</param>
        /// <param name="apiKeyExpiration">Expiration period for the API key (Unlimited, 1 Day, 30 Days, 6 Month, 1 Year).</param>
        /// <returns>The generated api key, or null if a key with the name already exists.</returns>
        /// <exception cref="ApiKeyGenerationException"></exception>
        public static async Task<string> GenerateAndSetAuthenticationTokenAsync(
            UserManager<IdentityUser> userManager, 
            IdentityUser user, 
            string newApiKeyName,
            string apiKeyType = "Read-Only",
            string apiKeyExpiration = "Unlimited")
        {
            string existingToken = await userManager.GetAuthenticationTokenAsync(user, ApiKeyProviderName, newApiKeyName).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(existingToken))
            {
                return null;
            }

            string newApiKey = await userManager.GenerateUserTokenAsync(user, ApiKeyProviderName, newApiKeyName).ConfigureAwait(false);

            // Calculate expiration date if applicable
            DateTime? expirationDate = CalculateExpirationDate(apiKeyExpiration);

            // Store metadata: prefix (4 bytes) + hash + metadata separator + type + expiration
            string metadata = $"|Type:{apiKeyType}|Expiration:{apiKeyExpiration}";
            if (expirationDate.HasValue)
            {
                metadata += $"|ExpiresAt:{expirationDate.Value:O}"; // ISO 8601 format
            }

            string newApiKeyHash = $"{newApiKey.Substring(0, 4)}{userManager.PasswordHasher.HashPassword(user, newApiKey)}{metadata}";

            IdentityResult setTokenResult = await userManager.SetAuthenticationTokenAsync(user, ApiKeyProviderName, newApiKeyName, newApiKeyHash).ConfigureAwait(false);
            if (!setTokenResult.Succeeded)
            {
                throw new ApiKeyGenerationException("Error saving key.", setTokenResult.Errors.Select(e => e.Description).ToList());
            }
            return newApiKey;
        }

        /// <summary>
        /// Calculates the expiration date based on the expiration period string.
        /// </summary>
        /// <param name="expirationPeriod">The expiration period (Unlimited, 1 Day, 30 Days, 6 Month, 1 Year).</param>
        /// <returns>The expiration DateTime, or null if unlimited.</returns>
        private static DateTime? CalculateExpirationDate(string expirationPeriod)
        {
            DateTime now = DateTime.UtcNow;

            return expirationPeriod switch
            {
                "1 Day" => now.AddDays(1),
                "30 Days" => now.AddDays(30),
                "6 Month" => now.AddMonths(6),
                "1 Year" => now.AddYears(1),
                "Unlimited" => null,
                _ => null // Default to unlimited if unknown value
            };
        }

        public async Task<List<(string KeyName, string KeyPrefix)>> GetUserApiKeysAsync(IdentityUser user)
        {
            List<IdentityUserToken<string>> tokens = await _appDbContext.UserTokens.Where(t => t.UserId == user.Id && t.LoginProvider == ApiKeyTokenProvider.ApiKeyProviderName).ToListAsync().ConfigureAwait(false);
            return tokens.Select(t => (t.Name, t.Value.Substring(0, 4))).ToList();
        }

        /// <summary>
        /// Gets the API key type (Read-Only or Read-Write) for a specific API key.
        /// </summary>
        /// <param name="user">The user who owns the API key.</param>
        /// <param name="apiKeyName">The name of the API key.</param>
        /// <returns>The API key type string, or null if not found or no metadata.</returns>
        public async Task<string> GetApiKeyTypeAsync(IdentityUser user, string apiKeyName)
        {
            IdentityUserToken<string> token = await _appDbContext.UserTokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id 
                    && t.LoginProvider == ApiKeyTokenProvider.ApiKeyProviderName 
                    && t.Name == apiKeyName)
                .ConfigureAwait(false);

            if (token == null || string.IsNullOrEmpty(token.Value))
            {
                return null;
            }

            // Extract API key type from metadata
            // Format: {prefix}{hash}|Type:{type}|Expiration:{period}|ExpiresAt:{date}
            int typeIndex = token.Value.IndexOf("|Type:");
            if (typeIndex < 0)
            {
                // No metadata, assume Read-Write for backward compatibility
                return "Read-Write";
            }

            string typePart = token.Value.Substring(typeIndex + "|Type:".Length);
            int nextSeparator = typePart.IndexOf('|');
            string apiKeyType = nextSeparator > 0 
                ? typePart.Substring(0, nextSeparator) 
                : typePart;

            return apiKeyType;
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
    }
}
