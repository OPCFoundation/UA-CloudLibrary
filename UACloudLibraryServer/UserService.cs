
namespace UACloudLibrary
{
    using UACloudLibrary.Interfaces;
    using System;

    /// <summary>
    /// User credentials validation class
    /// </summary>
    public class UserService : IUserService
    {
        /// <summary>
        /// Validates user credentials based on username and password
        /// </summary>
        public bool ValidateCredentials(string username, string password)
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
                // TODO: lookup authenticated users DB
                return false;
            }
        }
    }
}
