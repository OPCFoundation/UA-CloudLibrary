
namespace UACloudLibrary
{
    using UACloudLibrary.Interfaces;
    using System;
    using Microsoft.AspNetCore.Identity;
    using System.Threading.Tasks;

    /// <summary>
    /// User credentials validation class
    /// </summary>
    public class UserService : IUserService
    {
        private UserManager<IdentityUser> _userManager;

        public UserService(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Validates user credentials based on username and password
        /// </summary>
        public async Task<bool> ValidateCredentials(string username, string password)
        {
            // check for admin
            if (username == "admin")
            {
                string passwordFromEnvironment = Environment.GetEnvironmentVariable("ServicePassword");
                if (string.IsNullOrEmpty(passwordFromEnvironment))
                {
                    Console.WriteLine("ServicePassword env variable not set, please set it before trying to log in with admin credentials!");
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
                        var result = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
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
