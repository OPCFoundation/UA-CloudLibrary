using System.Globalization;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Opc.Ua.Cloud.Library.Authentication
{
    public class EmailManager
    {
        const string EmailTemplate = @"
            <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
            <p><img decoding=""async"" class=""alignnone wp-image-1095"" style=""margin: 0px; border: 0px none;"" src=""https://opcfoundation.org/wp-content/uploads/2013/09/OPC_Logo_500x72-300x110.jpg"" alt=""OPC Foundation Logo"" width=""240"" height=""95""></p>
            <p>The Industrial Interoperability Standard <img src=""https://s.w.org/images/core/emoji/14.0.0/72x72/2122.png"" alt=""™"" class=""wp-smiley"" style=""height: 1em; max-height: 1em;""></p>
            <p>&nbsp;</p>
            <p><p>{0}. Please <a href=""{1}"">click here</a> to continue.</p>
            <p>If the above link does not work, please copy and paste the link below into your browser’s address bar and press enter:</p>
            <p>{1}</p>
            <p>If you experience difficulty in logging in to the web site, please reply to this email for help.</p>
            </p>
            <p><strong>OPC Foundation</strong><br>
            16101 North 82nd Street, Suite 3B<br>
            Scottsdale, Arizona 85260-1868 US<br>
            +1 (480) 483-6644<br>
            <p style=""text-align: center""><a href=""mailto:unsubscribe@opcfoundation.org?subject=Unsubscribe%20to%20UA%20Cloud%20Library%20Emails&body="">Click here to unsubscribe.</a></p></p>
            ";

        public static async Task Send(IEmailSender emailSender, string email, string subject, string action, string url)
        {
            var body = string.Format(
                CultureInfo.InvariantCulture,
                EmailTemplate,
                action,
                HtmlEncoder.Default.Encode(url));

            await emailSender.SendEmailAsync(
                email,
                subject,
                body).ConfigureAwait(false);
        }
    }
}
