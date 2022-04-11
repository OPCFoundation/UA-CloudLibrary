/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

namespace UACloudLibrary
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading.Tasks;
    using UACloudLibrary.Interfaces;

    public class UserService : IUserService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger _logger;

        public UserService(UserManager<IdentityUser> userManager, ILoggerFactory logger)
        {
            _userManager = userManager;
            _logger = logger.CreateLogger("UserService");
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            // check for admin
            if (username == "admin")
            {
                string passwordFromEnvironment = Environment.GetEnvironmentVariable("ServicePassword");
                if (string.IsNullOrEmpty(passwordFromEnvironment))
                {
                    _logger.LogError("ServicePassword env variable not set, please set it before trying to log in with admin credentials!");
                    return false;
                }
                else
                {
                    return username.Equals("admin") && password.Equals(passwordFromEnvironment);
                }
            }
            else
            {
                if (password != null)
                {
                    IdentityUser user = await _userManager.FindByNameAsync(username).ConfigureAwait(false);
                    if (user == null)
                    {
                        user = await _userManager.FindByEmailAsync(username).ConfigureAwait(false);
                    }

                    if (user != null)
                    {
                        PasswordVerificationResult result = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
                        if (result == PasswordVerificationResult.Success)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}
