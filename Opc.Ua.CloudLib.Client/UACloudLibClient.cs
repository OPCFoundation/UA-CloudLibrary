using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua.CloudLib.Client.Models;

namespace Opc.Ua.CloudLib.Client
{
    /// <summary>
    /// This class handles the quering and conversion of the response
    /// </summary>
    public partial class UACloudLibClient : IDisposable
    {
        private GraphQLHttpClient m_client;
        private GraphQLRequest request = new GraphQLRequest();
        /// <summary>Gets or sets the endpoint.</summary>
        /// <value>The endpoint.</value>
        public Uri Endpoint
        {
            get { return BaseEndpoint; }
            set { BaseEndpoint = value; }
        }

        private Uri BaseEndpoint { get; set; }

        private string m_strUsername = "";
        private string m_strPassword = "";

        /// <summary>Gets or sets the username.</summary>
        /// <value>The username.</value>
        public string Username { get; set; }

        /// <summary>Sets the password.</summary>
        /// <value>The password.</value>
        public string Password
        {
            set { m_strPassword = value; }
        }

        /// <summary>Initializes a new instance of the <see cref="UACloudLibClient" /> class.</summary>
        /// <param name="strEndpoint">The string endpoint.</param>
        /// <param name="strUsername">The string username.</param>
        /// <param name="strPassword">The string password.</param>
        public UACloudLibClient(string strEndpoint, string strUsername, string strPassword)
        {
            BaseEndpoint = new Uri(strEndpoint);
            m_client = new GraphQLHttpClient(new Uri(strEndpoint + "/graphql"), new NewtonsoftJsonSerializer());
            string temp = Convert.ToBase64String(Encoding.UTF8.GetBytes(strUsername + ":" + strPassword));
            m_client.HttpClient.DefaultRequestHeaders.Add("Authorization", "basic " + temp);
            m_strUsername = strUsername;
            m_strPassword = strPassword;
        }

        //Sends the query and converts it
        /// <summary>Sends the GraphQL query and converts it to JSON</summary>
        /// <typeparam name="T">the JSON target tyoe</typeparam>
        /// <param name="request">The request.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        /// <exception cref="System.Exception"></exception>
        private async Task<T> SendAndConvert<T>(GraphQLRequest request)
        {
            GraphQLResponse<JObject> response = await m_client.SendQueryAsync<JObject>(request);
            string dataJson = response.Data.First?.First?.ToString();
            return JsonConvert.DeserializeObject<T>(dataJson);
        }

        /// <summary>
        /// Retrieves a list of ObjectTypes
        /// </summary>
        /// <returns></returns>
        public async Task<List<ObjectResult>> GetObjectTypes()
        {
            request.Query = PrebuiltQueries.ObjectQuery;
            return await SendAndConvert<List<ObjectResult>>(request);
        }
        /// <summary>
        /// Retrieves a list of metadata
        /// </summary>
        /// <returns></returns>
        public async Task<List<MetadataResult>> GetMetadata()
        {
            request.Query = PrebuiltQueries.MetadataQuery;
            return await SendAndConvert<List<MetadataResult>>(request);            
        }
        /// <summary>
        /// Retrieves a list of variabletypes
        /// </summary>
        /// <returns></returns>
        public async Task<List<VariableResult>> GetVariables()
        {
            request.Query = PrebuiltQueries.VariableQuery;
            return await SendAndConvert<List<VariableResult>>(request);
        }
        /// <summary>
        /// Retrieves a list of referencetype
        /// </summary>
        /// <returns></returns>
        public async Task<List<ReferenceResult>> GetReferencetype()
        {
            request.Query = PrebuiltQueries.ReferenceQuery;
            return await SendAndConvert<List<ReferenceResult>>(request);
        }
        /// <summary>
        /// Retrieves a list of datatype
        /// </summary>
        /// <returns></returns>
        public async Task<List<DatatypeResult>> GetDatatype()
        {
            request.Query = PrebuiltQueries.DatatypeQuery;
            return await SendAndConvert<List<DatatypeResult>>(request);
        }
        /// <summary>
        /// Retrieves a list of metadata and converts it to a list of addressspaces
        /// </summary>
        /// <returns></returns>
        public async Task<List<AddressSpace>> GetConvertedResult()
        {
            var result = await GetMetadata();

            return ConvertMetadataToAddressspace.Convert(result);
        }

        /// <summary>
        /// Download chosen Nodeset with a https call
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public async Task<AddressSpace> DownloadNodeset(string identifier)
        {
            HttpClient webClient = new HttpClient
            {
                BaseAddress = new Uri(Endpoint.ToString())
            };

            webClient.DefaultRequestHeaders.Add("Authorization", "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(m_strUsername + ":" + m_strPassword)));

            var address = webClient.BaseAddress.ToString() + "infomodel/download/" + Uri.EscapeDataString(identifier);
            var response = await webClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, address)).ConfigureAwait(false);
            var converted = JsonConvert.DeserializeObject<AddressSpace>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            return converted;
        }


        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool _isDisposed;
        // Protected implementation of Dispose pattern.
        /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    m_client?.Dispose();
                    m_client = null;
                }

                _isDisposed = true;
            }
        }
    }
}
