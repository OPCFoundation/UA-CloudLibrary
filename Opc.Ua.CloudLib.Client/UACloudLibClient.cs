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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua.Cloud.Client.Models;

[assembly: CLSCompliant(false)]
namespace Opc.Ua.Cloud.Client
{
    /// <summary>
    /// This class handles the quering and conversion of the response
    /// </summary>
    public partial class UACloudLibClient : IDisposable
    {
        /// <summary>The standard endpoint</summary>
#pragma warning disable S1075 // URIs should not be hardcoded
        // Stable URI of the official UA Cloud Library
        private static Uri _standardEndpoint = new Uri("https://uacloudlibrary.opcfoundation.org");
#pragma warning restore S1075 // URIs should not be hardcoded

        private readonly RestClient _restClient;

        /// <summary>Gets the Base endpoint used to access the api.</summary>
        /// <value>The url of the endpoint</value>
        public Uri BaseEndpoint { get; private set; }

        /// <summary>
        /// Options to use in IOptions patterns
        /// </summary>
        public class Options
        {
            /// <summary>
            /// Endpoint of the cloud library. Defaults to the OPC Foundation Cloud Library.
            /// </summary>
            public string EndPoint { get; set; }

            /// <summary>
            /// Username to use for authenticating with the cloud library
            /// </summary>
            public string Username { get; set; }

            /// <summary>
            /// Password to use for authenticating with the cloud library
            /// </summary>
            public string Password { get; set; }
        }

        /// <summary>Initializes a new instance of the <see cref="UACloudLibClient" /> class.</summary>
        /// <param name="strEndpoint">The string endpoint.</param>
        /// <param name="strUsername">The string username.</param>
        /// <param name="strPassword">The string password.</param>
        public UACloudLibClient(string strEndpoint, string strUsername, string strPassword)
        {
            if (string.IsNullOrEmpty(strEndpoint))
            {
                strEndpoint = _standardEndpoint.ToString();
            }

            BaseEndpoint = new Uri(strEndpoint);

            string temp = Convert.ToBase64String(Encoding.UTF8.GetBytes(strUsername + ":" + strPassword));
            _restClient = new RestClient(strEndpoint, new AuthenticationHeaderValue("Basic", temp));
        }

        /// <summary>
        /// Initializes a new instance using an existing HttpClient
        /// </summary>
        /// <param name="httpClient"></param>
        public UACloudLibClient(HttpClient httpClient)
        {
            _restClient = new RestClient(httpClient);
        }

        /// <summary>
        /// Download chosen Nodeset with a REST call
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="metadataOnly"></param>
        public async Task<UANameSpace> DownloadNodesetAsync(string identifier, bool metadataOnly = false) => await _restClient.DownloadNodesetAsync(identifier, metadataOnly).ConfigureAwait(false);

        /// <summary>
        /// Download chosen Nodeset with a REST call
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="metadataOnly"></param>
        public async Task<UANameSpace> DownloadNodesetAsync(uint identifier, bool metadataOnly = false) => await _restClient.DownloadNodesetAsync(identifier.ToString(CultureInfo.InvariantCulture), metadataOnly).ConfigureAwait(false);

        /// <summary>
        /// Use this method if the CloudLib instance doesn't provide the GraphQL API
        /// </summary>
        public async Task<List<UANameSpace>> GetBasicNodesetInformationAsync(int offset, int limit, List<string> keywords = null) => await _restClient.GetBasicNodesetInformationAsync(offset, limit, keywords).ConfigureAwait(false);

        /// <summary>
        /// Gets all available namespaces and the corresponding node set identifier
        /// </summary>
        /// <returns></returns>
        public Task<(string NamespaceUri, string Identifier)[]> GetNamespaceIdsAsync() => _restClient.GetNamespaceIdsAsync();

        /// <summary>
        /// Gets all available namespaces and the corresponding node set identifier, version and publication date
        /// </summary>
        /// <returns></returns>
        public Task<(string NamespaceUri, string Identifier, string Version, string PublicationDate)[]> GetNamespaceIdsExAsync() => _restClient.GetNamespaceIdsExAsync();

        /// <summary>
        /// Upload a nodeset to the cloud library
        /// </summary>
        /// <param name="nameSpace"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        public Task<(HttpStatusCode Status, string Message)> UploadNodeSetAsync(UANameSpace nameSpace, bool overwrite = false) => _restClient.UploadNamespaceAsync(nameSpace, overwrite);

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            _restClient.Dispose();
        }
    }
}
