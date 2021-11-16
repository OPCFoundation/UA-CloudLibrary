using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UACloudLibClientLibrary.WhereExpressions;


namespace UACloudLibClientLibrary
{
    /// <summary>
    /// This class handles the quering and conversion of the response
    /// </summary>
    public partial class UACloudLibClient
    {
        private GraphQLHttpClient m_client = null;
        private GraphQLRequest request = new GraphQLRequest();

        public Uri Endpoint
        {
            get { return m_client.Options.EndPoint; }
            set { m_client.Options.EndPoint = value; }
        }

        public UACloudLibClient(string strEndpoint, string strUsername, string strPassword)
        {
            m_client = new GraphQLHttpClient(strEndpoint, new NewtonsoftJsonSerializer());
            string temp = Convert.ToBase64String(Encoding.UTF8.GetBytes(strUsername + ":" + strPassword));
            m_client.HttpClient.DefaultRequestHeaders.Add("Authorization", "basic " + temp);
        }

        /// <summary>
        /// Queries the organisations with the given filters.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="after"></param>
        /// <param name="filter"></param>
        /// <returns>The converted JSON result</returns>
        public async Task<PageInfo<Organisation>> GetOrganisations(int pageSize = 10, string after = "-1", List<OrganisationWhereExpression> filter = null)
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
        public async Task<PageInfo<AddressSpace>> GetAddressSpaces(int pageSize = 10, string after = "-1", List<AddressSpaceWhereExpression> filter = null, GroupedOrExpression<AddressSpaceSearchField> groupedExpression = null)
        {
            request.Query = QueryMethods.AddressSpacesQuery(after, pageSize, filter, groupedExpression);
            return await SendAndConvert<PageInfo<AddressSpace>>(request);
        }

        /// <summary>
        /// Queries the addressspaces that match the searchtext
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="after"></param>
        /// <param name="searchtext"></param>
        /// <returns></returns>
        public async Task<PageInfo<AddressSpace>> GetAddressSpaces(string searchtext, int pageSize = 10, string after = "-1")
        {
            request.Query = QueryMethods.AddressSpaceQuery(after, pageSize, searchtext);
            return await SendAndConvert<PageInfo<AddressSpace>>(request);
        }

        /// <summary>
        /// Queries the categories with the given filters
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="after"></param>
        /// <param name="filter"></param>
        /// <returns>The converted JSON result</returns>
        public async Task<PageInfo<AddressSpaceCategory>> GetAddressSpaceCategories(int pageSize = 10, string after = "-1", List<CategoryWhereExpression> filter = null)
        {
            request.Query = QueryMethods.QueryCategories(pageSize, after, filter);
            return await SendAndConvert<PageInfo<AddressSpaceCategory>>(request);
        }

        /// <summary>
        /// Searches for and downloads the specified nodeset
        /// </summary>
        /// <param name="NodesetID"></param>
        /// <returns>The converted JSON result</returns>
        public async Task<AddressSpaceNodeset2> GetNodeset(string NodesetID)
        {
            request.Query = QueryMethods.NodesetQuery(NodesetID);
            return await SendAndConvert<AddressSpaceNodeset2>(request);
        }

        // Sends the query and converts it
        private async Task<T> SendAndConvert<T>(GraphQLRequest request)
        {
            GraphQLResponse<JObject> response = await m_client.SendQueryAsync<JObject>(request);

            string dataJson = response.Data.First?.First?.ToString();

            return JsonConvert.DeserializeObject<T>(dataJson);
        }
    }
}
