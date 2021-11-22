
using System.Threading.Tasks;

namespace UACloudLibrary.Interfaces
{
    public interface IUserService
    {
        Task<bool> ValidateCredentials(string username, string password);
    }
}

