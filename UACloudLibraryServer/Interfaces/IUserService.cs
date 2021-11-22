
using System.Threading.Tasks;

namespace UACloudLibrary.Interfaces
{
    public interface IUserService
    {
        Task<bool> ValidateCredentialsAsync(string username, string password);

        Task<bool> ValidateCookieAsync(string cookie);
    }
}

