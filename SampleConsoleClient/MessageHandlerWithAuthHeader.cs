/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

namespace SampleConsoleClient
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    class MessageHandlerWithAuthHeader : DelegatingHandler
    {
        private string _username = string.Empty;
        private string _password = string.Empty;

        public MessageHandlerWithAuthHeader(string username, string password)
        {
            _username = username;
            _password = password;

            InnerHandler = new HttpClientHandler();
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("Authorization", "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(_username + ":" + _password)));

            return base.SendAsync(request, cancellationToken);
        }
    }
}
