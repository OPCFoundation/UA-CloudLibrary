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

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using PostmarkDotNet;

namespace Opc.Ua.Cloud.Library
{
    public class PostmarkEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            string apiKey = Environment.GetEnvironmentVariable("EmailSenderAPIKey");
            if (!string.IsNullOrEmpty(apiKey))
            {
                PostmarkClient client = new(apiKey);

                string emailFrom = Environment.GetEnvironmentVariable("RegistrationEmailFrom");
                if (string.IsNullOrEmpty(emailFrom)) emailFrom = "office@opcfoundation.org";

                // Send an email asynchronously:
                PostmarkMessage msg = new() {
                    To = email,
                    From = emailFrom,
                    TrackOpens = true,
                    Subject = subject,
                    TextBody = htmlMessage,
                    HtmlBody = htmlMessage,
                    MessageStream = "outbound",
                    Tag = "UA Cloud Library"
                };

                return client.SendMessageAsync(msg);
            }
            else
            {
                Console.WriteLine($"Mail sending is disabled due to missing API-Key for Postmark (email: ${email}, subject: ${subject})");
                return Task.CompletedTask;
            }
        }
    }
}
