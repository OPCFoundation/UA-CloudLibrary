
namespace UA_CloudLibrary.Interfaces
{
    /// <summary>
    /// User credentials validation interface
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Validates credentials based and username and password
        /// </summary>
        bool ValidateCredentials(string username, string password);
    }
}

