using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Opc.Ua.Cloud.Library.Interfaces
{
    public interface ICaptchaValidation
    {
        Task<string> ValidateCaptcha(string responseToken);
    }
}

