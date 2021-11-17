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
        private GraphQLRequest _request = new GraphQLRequest();

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
        public async Task<Tuple<string, PageInfo<Organisation>>> GetOrganisations(int pageSize = 10, string after = "-1", List<OrganisationWhereExpression> filter = null)
        {
            try
            {
                _request.Query = QueryMethods.QueryOrganisations(pageSize, after, filter);
                GraphQLResponse<JObject> response = await m_client.SendQueryAsync<JObject>(_request).ConfigureAwait(false);
                if ((response.Errors != null) && (response.Errors.Length > 0))
                {
                    return new Tuple<string, PageInfo<Organisation>>("GraphQL error: " + response.Errors[0].Message, null);
                }
                else
                {
                    string dataJson = response.Data?.First?.First?.ToString();
                    return new Tuple<string, PageInfo<Organisation>>(null, JsonConvert.DeserializeObject<PageInfo<Organisation>>(dataJson));
                }
            }
            catch (Exception ex)
            {
                return new Tuple<string, PageInfo<Organisation>>("GraphQL exception: " + ex.Message, null);
            }
        }

        /// <summary>
        /// Queries the addressspaces with the given filters and converts the result
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="after"></param>
        /// <param name="filter"></param>
        /// <param name="groupedExpression"></param>
        /// <returns>The converted JSON result</returns>
        public async Task<Tuple<string, PageInfo<AddressSpace>>> GetAddressSpaces(int pageSize = 10, string after = "-1", List<AddressSpaceWhereExpression> filter = null, GroupedOrExpression<AddressSpaceSearchField> groupedExpression = null)
        {
            try
            {
                _request.Query = QueryMethods.AddressSpacesQuery(after, pageSize, filter, groupedExpression);
                GraphQLResponse<JObject> response = await m_client.SendQueryAsync<JObject>(_request).ConfigureAwait(false);
                if ((response.Errors != null) && (response.Errors.Length > 0))
                {
                    return new Tuple<string, PageInfo<AddressSpace>>("GraphQL error: " + response.Errors[0].Message, null);
                }
                else
                {
                    string dataJson = response.Data?.First?.First?.ToString();
                    return new Tuple<string, PageInfo<AddressSpace>>(null, JsonConvert.DeserializeObject<PageInfo<AddressSpace>>(dataJson));
                }
            }
            catch (Exception ex)
            {
                return new Tuple<string, PageInfo<AddressSpace>>("GraphQL exception: " + ex.Message, null);
            }

        }

        /// <summary>
        /// Queries the addressspaces that match the searchtext
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="after"></param>
        /// <param name="searchtext"></param>
        /// <returns></returns>
        public async Task<Tuple<string, PageInfo<AddressSpace>>> GetAddressSpaces(string searchtext, int pageSize = 10, string after = "-1")
        {
            try
            {
                _request.Query = QueryMethods.AddressSpaceQuery(after, pageSize, searchtext);
                GraphQLResponse<JObject> response = await m_client.SendQueryAsync<JObject>(_request).ConfigureAwait(false);
                if ((response.Errors != null) && (response.Errors.Length > 0))
                {
                    return new Tuple<string, PageInfo<AddressSpace>>("GraphQL error: " + response.Errors[0].Message, null);
                }
                else
                {
                    string dataJson = response.Data?.First?.First?.ToString();
                    return new Tuple<string, PageInfo<AddressSpace>>(null, JsonConvert.DeserializeObject<PageInfo<AddressSpace>>(dataJson));
                }
            }
            catch (Exception ex)
            {
                return new Tuple<string, PageInfo<AddressSpace>>("GraphQL exception: " + ex.Message, null);
            }
        }

        /// <summary>
        /// Queries the categories with the given filters
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="after"></param>
        /// <param name="filter"></param>
        /// <returns>The converted JSON result</returns>
        public async Task<Tuple<string, PageInfo<AddressSpaceCategory>>> GetAddressSpaceCategories(int pageSize = 10, string after = "-1", List<CategoryWhereExpression> filter = null)
        {
            try
            {
                _request.Query = QueryMethods.QueryCategories(pageSize, after, filter);
                GraphQLResponse<JObject> response = await m_client.SendQueryAsync<JObject>(_request).ConfigureAwait(false);
                if ((response.Errors != null) && (response.Errors.Length > 0))
                {
                    return new Tuple<string, PageInfo<AddressSpaceCategory>>("GraphQL error: " + response.Errors[0].Message, null);
                }
                else
                {
                    string dataJson = response.Data?.First?.First?.ToString();
                    return new Tuple<string, PageInfo<AddressSpaceCategory>>(null, JsonConvert.DeserializeObject<PageInfo<AddressSpaceCategory>>(dataJson));
                }
            }
            catch (Exception ex)
            {
                return new Tuple<string, PageInfo<AddressSpaceCategory>>("GraphQL exception: " + ex.Message, null);
            }
        }

        /// <summary>
        /// Searches for and downloads the specified nodeset
        /// </summary>
        /// <param name="NodesetID"></param>
        /// <returns>The converted JSON result</returns>
        public async Task<Tuple<string, AddressSpaceNodeset2>> GetNodeset(string NodesetID)
        {
            try
            {
                _request.Query = QueryMethods.NodesetQuery(NodesetID);
                GraphQLResponse<JObject> response = await m_client.SendQueryAsync<JObject>(_request).ConfigureAwait(false);
                if ((response.Errors != null) && (response.Errors.Length > 0))
                {
                    return new Tuple<string, AddressSpaceNodeset2>("GraphQL error: " + response.Errors[0].Message, null);
                }
                else
                {
                    string dataJson = response.Data?.First?.First?.ToString();
                    return new Tuple<string, AddressSpaceNodeset2>(null, JsonConvert.DeserializeObject<AddressSpaceNodeset2>(dataJson));
                }
            }
            catch (Exception ex)
            {
                return new Tuple<string, AddressSpaceNodeset2>("GraphQL exception: " + ex.Message, null);
            }
        }
    }
}
