/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;

namespace UACloudLibrary
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var apiKey = Environment.GetEnvironmentVariable("SendGridAPIKey");
            if (!string.IsNullOrEmpty(apiKey))
            {
                SendGridClient client = new SendGridClient(apiKey);
                SendGridMessage msg = new SendGridMessage()
                {
                    From = new EmailAddress("stefan.hoppe@opcfoundation.org"),
                    ReplyTo = new EmailAddress("no-reply@opcfoundation.org"),
                    Subject = subject,
                    PlainTextContent = htmlMessage,
                    HtmlContent = htmlMessage
                };
                msg.AddTo(new EmailAddress(email));

                // Disable click tracking.
                // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
                msg.SetClickTracking(false, false);

                return client.SendEmailAsync(msg);
            }
            else
            {
                Console.WriteLine($"Mail sending is disabled due to missing API-Key for sendgrid (email: ${email}, subject: ${subject})");
                return Task.CompletedTask;
            }
        }
    }
}
