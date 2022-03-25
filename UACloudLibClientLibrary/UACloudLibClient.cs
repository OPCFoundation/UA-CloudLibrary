using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UACloudLibClientLibrary.Models;

namespace UACloudLibClientLibrary
{
    /// <summary>
    /// This class handles the quering and conversion of the response
    /// </summary>
    public partial class UACloudLibClient
    {
        GraphQLHttpClient m_client = null;
        GraphQLRequest request = new GraphQLRequest();
        public List<string> errors = new List<string>();
        public Uri Endpoint
        {
            get { return BaseEndpoint; }
            set { BaseEndpoint = value; }
        }

        Uri BaseEndpoint { get; set; }

        string m_strUsername = "";
        string m_strPassword = "";

        public string Username { get; set; }

        public string Password
        {
            set { m_strPassword = value; }
        }

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
        async Task<T> SendAndConvert<T>(GraphQLRequest request)
        {
            try
            {
                GraphQLResponse<JObject> response = await m_client.SendQueryAsync<JObject>(request);
                string dataJson = response.Data.First?.First?.ToString();
                return JsonConvert.DeserializeObject<T>(dataJson);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
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
        public AddressSpace DownloadNodeset(string identifier)
        {
            HttpClient webClient = new HttpClient
            {
                BaseAddress = new Uri(Endpoint.ToString())
            };

            webClient.DefaultRequestHeaders.Add("Authorization", "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(m_strUsername + ":" + m_strPassword)));

            var address = webClient.BaseAddress.ToString() + "infomodel/download/" + Uri.EscapeDataString(identifier);
            var response = webClient.Send(new HttpRequestMessage(HttpMethod.Get, address));
            var converted = JsonConvert.DeserializeObject<AddressSpace>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            return converted;
        }
    }
}
