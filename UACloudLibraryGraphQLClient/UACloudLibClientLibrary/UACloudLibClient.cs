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
        public async Task<PageInfo<ObjectResult>> GetObjectTypes()
        {
            request.Query = QueryMethods.QueryObjectType();
            return await SendAndConvert<PageInfo<ObjectResult>>(request);
        }
        /// <summary>
        /// Retrieves a list of metadata
        /// </summary>
        /// <returns></returns>
        public async Task<PageInfo<MetadataResult>> GetMetadata()
        {
            request.Query = QueryMethods.QueryMetadata(); ;
            return await SendAndConvert<PageInfo<MetadataResult>>(request);            
        }
        /// <summary>
        /// Retrieves a list of variabletypes
        /// </summary>
        /// <returns></returns>
        public async Task<PageInfo<VariableResult>> GetVariables()
        {
            request.Query = QueryMethods.QueryVariables();
            return await SendAndConvert<PageInfo<VariableResult>>(request);
        }

        /// <summary>
        /// Retrieves a list of referencetype
        /// </summary>
        /// <returns></returns>
        public async Task<PageInfo<ReferenceResult>> GetReferencetype()
        {
            request.Query = QueryMethods.QueryReferences();
            return await SendAndConvert<PageInfo<ReferenceResult>>(request);
        }

        /// <summary>
        /// Retrieves a list of datatype
        /// </summary>
        /// <returns></returns>
        public async Task<PageInfo<DatatypeResult>> GetDatatype()
        {
            request.Query = QueryMethods.QueryDatatypes();
            return await SendAndConvert<PageInfo<DatatypeResult>>(request);
        }

        public async Task<List<AddressSpace>> GetConvertedMetadata()
        {
            request.Query = QueryMethods.QueryMetadata();
            PageInfo<MetadataResult> result = await SendAndConvert<PageInfo<MetadataResult>>(request);
            return ConvertMetadataToAddressspace.Convert(result);
        }

        /// <summary>
        /// Queries the organisations with the given filters.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="after"></param>
        /// <param name="filter"></param>
        /// <returns>The converted JSON result</returns>
        public async Task<PageInfo<Organisation>> GetOrganisations(int pageSize = 10, string after = "-1", IEnumerable<OrganisationWhereExpression> filter = null)
        {
            request.Query = QueryMethods.QueryOrganisations(pageSize, after, filter);
            return await SendAndConvert<PageInfo<Organisation>>(request);
        }

        /// <summary>
        /// Queries the addressspaces with the given filters and converts the result
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="after"></param>
        /// <param name="filter"></param>
        /// <param name="groupedExpression"></param>
        /// <returns>The converted JSON result</returns>
        public async Task<PageInfo<AddressSpace>> GetAddressSpaces(int pageSize = 10, string after = "-1", IEnumerable<AddressSpaceWhereExpression> filter = null)
        {
            request.Query = QueryMethods.AddressSpacesQuery(after, pageSize, filter);
            return await SendAndConvert<PageInfo<AddressSpace>>(request);
        }

        /// <summary>
        /// Queries the categories with the given filters
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="after"></param>
        /// <param name="filter"></param>
        /// <returns>The converted JSON result</returns>
        public async Task<PageInfo<AddressSpaceCategory>> GetAddressSpaceCategories(int pageSize = 10, string after = "-1", IEnumerable<CategoryWhereExpression> filter = null)
        {
            request.Query = QueryMethods.QueryCategories(pageSize, after, filter);
            return await SendAndConvert<PageInfo<AddressSpaceCategory>>(request);
        }

        /// <summary>
        /// Download chosen Nodeset with a https call
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public AddressSpace DownloadNodeset(string identifier)
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
