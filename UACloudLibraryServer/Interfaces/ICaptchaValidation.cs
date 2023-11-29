namespace Opc.Ua.Cloud.Library.Interfaces
{
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;

    public interface ICaptchaValidation
    {
        Task<string> ValidateCaptcha(string responseToken);
    }
}

