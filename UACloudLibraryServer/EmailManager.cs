/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Opc.Ua.Cloud.Library
{
    public class EmailManager
    {
        const string EmailTemplate = @"
            <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
            <p><img decoding=""async"" class=""alignnone wp-image-1095"" style=""margin: 0px; border: 0px none;"" src=""https://opcfoundation.org/wp-content/uploads/2013/09/OPC_Logo_500x72-300x110.jpg"" alt=""OPC Foundation Logo"" width=""240"" height=""95""></p>
            <p><b>The Industrial Interoperability Standard ™</b></p>
            <p>&nbsp;</p>
            <p><p>{0} Please <a href=""{1}"">click here</a> to continue.</p>
            <p>If the above link does not work, please copy and paste the link below into your browser’s address bar and press enter:</p>
            <p>{1}</p>
            <p>If you experience difficulty with this site, please reply to this email for help.</p>
            </p>
            <p><strong>OPC Foundation</strong><br>
            16101 North 82nd Street, Suite 3B<br>
            Scottsdale, Arizona 85260-1868 US<br>
            +1 (480) 483-6644<br>
            <p style=""text-align: center""><a href=""mailto:unsubscribe@opcfoundation.org?subject=Unsubscribe%20from%20UA%20Cloud%20Library%20Emails&body="">Click here to unsubscribe.</a></p></p>
            ";

        public static async Task Send(IEmailSender emailSender, string email, string subject, string action, string url)
        {
            var body = string.Format(
                CultureInfo.InvariantCulture,
                EmailTemplate,
                action,
                url);

            await emailSender.SendEmailAsync(
                email,
                subject,
                body).ConfigureAwait(false);
        }
    }
}
