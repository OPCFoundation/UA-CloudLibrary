
namespace UACloudLibrary
{
    using Microsoft.AspNetCore.Identity;
    using System;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using UACloudLibrary.Interfaces;

    public class UserService : IUserService
    {
        private UserManager<IdentityUser> _userManager;

        private SignInManager<IdentityUser> _signInManager;

        public UserService(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public Task<bool> ValidateCookieAsync(string cookie)
        {
            //TODO: properly validate cookie
            return Task.FromResult(true);

            //IdentityUser currentUser = await _userManager.GetUserAsync(ClaimsPrincipal.Current);
            //string token = await _userManager.GetAuthenticationTokenAsync(currentUser, "Microsoft", "access_token").ConfigureAwait(false);
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string password)
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
