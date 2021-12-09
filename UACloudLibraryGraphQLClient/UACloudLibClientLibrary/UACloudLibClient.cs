using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client;
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
        private GraphQLHttpClient m_client = null;
        private GraphQLRequest request = new GraphQLRequest();
        public List<string> errors = new List<string>();
        public Uri Endpoint
        {
            get { return BaseEndpoint; }
            set { BaseEndpoint = value; }
        }

        private Uri BaseEndpoint { get; set; }

        private string m_strUsername = "";
        private string m_strPassword = "";

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
        }

        //Sends the query and converts it
        private async Task<T> SendAndConvert<T>(GraphQLRequest request)
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
        public async Task<List<ObjectType>> GetObjectTypes()
        {
            request.Query = PrebuiltQueries.ObjectQuery;
            return await SendAndConvert<List<ObjectType>>(request);
        }
        /// <summary>
        /// Retrieves a list of metadata
        /// </summary>
        /// <returns></returns>
        public async Task<List<MetadataType>> GetMetadata()
        {
            request.Query = PrebuiltQueries.MetadataQuery;
            return await SendAndConvert<List<MetadataType>>(request);            
        }
        /// <summary>
        /// Retrieves a list of variabletypes
        /// </summary>
        /// <returns></returns>
        public async Task<List<VariableType>> GetVariables()
        {
            request.Query = PrebuiltQueries.VariableQuery;
            return await SendAndConvert<List<VariableType>>(request);
        }
        /// <summary>
        /// Retrieves a list of referencetype
        /// </summary>
        /// <returns></returns>
        public async Task<List<ReferenceType>> GetReferencetype()
        {
            request.Query = PrebuiltQueries.ReferenceQuery;
            return await SendAndConvert<List<ReferenceType>>(request);
        }
        /// <summary>
        /// Retrieves a list of datatype
        /// </summary>
        /// <returns></returns>
        public async Task<List<DatatypeType>> GetDatatype()
        {
            request.Query = PrebuiltQueries.DatatypeQuery;
            return await SendAndConvert<List<DatatypeType>>(request);
        }
        /// <summary>
        /// Retrieves a list of metadata and converts it to a list of addressspaces
        /// </summary>
        /// <returns></returns>
        public async Task<List<CombinatedTypes>> GetCombinedResult()
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
            WebClient webClient = new WebClient
            {
                BaseAddress = Endpoint.ToString()
            };

            webClient.Headers.Add("Authorization", "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(m_strUsername + ":" + m_strPassword)));

            var address = webClient.BaseAddress + "infomodel/download/" + Uri.EscapeDataString(identifier);
            var response = webClient.DownloadString(address);
            var converted = JsonConvert.DeserializeObject<AddressSpace>(response);
            return converted;
        }
    }
}
