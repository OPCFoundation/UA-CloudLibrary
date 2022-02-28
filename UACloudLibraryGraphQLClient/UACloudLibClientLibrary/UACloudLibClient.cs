
namespace UACloudLibClientLibrary
{
    using GraphQL;
    using GraphQL.Client.Http;
    using GraphQL.Client.Serializer.Newtonsoft;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// This class handles the quering and conversion of the response
    /// </summary>
    public partial class UACloudLibClient : IDisposable
    {
        public static Uri StandardEndpoint = new Uri("https://uacloudlibrary.opcfoundation.org");

        private GraphQLHttpClient m_client = null;
        private GraphQLRequest request = new GraphQLRequest();
        public List<string> errors = new List<string>();
        public Uri Endpoint
        {
            get { return BaseEndpoint; }
            set { BaseEndpoint = value; }
        }

        private RestClient restClient;

        private Uri BaseEndpoint { get; set; }

        private string m_strUsername = "";
        private string m_strPassword = "";

        public string Username { get; set; }

        public string Password
        {
            set { m_strPassword = value; }
        }

        /// <summary>
        /// This Constructor uses the standard endpoint with no authorization
        /// </summary>
        public UACloudLibClient()
        {
            BaseEndpoint = StandardEndpoint;
            m_client = new GraphQLHttpClient(new Uri(BaseEndpoint + "/graphql"), new NewtonsoftJsonSerializer());
        }

        /// <summary>
        /// This constructor uses the standard endpoint with authorization
        /// </summary>
        /// <param name="strUsername"></param>
        /// <param name="strPassword"></param>
        public UACloudLibClient(string strUsername, string strPassword)
        {
            BaseEndpoint = StandardEndpoint;
            m_client = new GraphQLHttpClient(new Uri(BaseEndpoint + "/graphql"), new NewtonsoftJsonSerializer());
            string temp = Convert.ToBase64String(Encoding.UTF8.GetBytes(strUsername + ":" + strPassword));
            m_client.HttpClient.DefaultRequestHeaders.Add("Authorization", "basic " + temp);
        }

        public UACloudLibClient(string strEndpoint, string strUsername, string strPassword)
        {
            restClient = new RestClient(strEndpoint, strUsername, strPassword);
            BaseEndpoint = new Uri(strEndpoint);
            m_client = new GraphQLHttpClient(new Uri(strEndpoint + "/graphql"), new NewtonsoftJsonSerializer());
            string temp = Convert.ToBase64String(Encoding.UTF8.GetBytes(strUsername + ":" + strPassword));
            m_client.HttpClient.DefaultRequestHeaders.Add("Authorization", "basic " + temp);
            m_strUsername = strUsername;
            m_strPassword = strPassword;
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
            List<AddressSpace> convertedResult = null;
            request.Query = QueryMethods.QueryMetadata();
            PageInfo<MetadataResult> result = await SendAndConvert<PageInfo<MetadataResult>>(request);
            if (result != null)
            {
                convertedResult = ConvertMetadataToAddressspace.Convert(result);
            }
            else
            {
                convertedResult = await restClient.GetBasicAddressSpaces();
            }

            return convertedResult;
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
            PageInfo<AddressSpace> result = await SendAndConvert<PageInfo<AddressSpace>>(request);
            if (result == null)
            {
                List<AddressSpace> temp = await restClient.GetBasicAddressSpaces();
                result.TotalCount = temp.Count;

            }
            else
            {
                result = ConvertWithPaging(await restClient.GetBasicAddressSpaces(filter.Select(e => e.Value)), pageSize, Convert.ToInt32(after));
            }

            return result;
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
        public async Task<AddressSpace> DownloadNodeset(string identifier) => await restClient.DownloadNodeset(identifier);

        /// <summary>
        /// Use this method if the CloudLib instance doesn't provide the GraphQL API
        /// </summary>
        /// <returns></returns>
        public async Task<List<AddressSpace>> GetBasicAddressSpaces(IEnumerable<string> keywords = null) => await restClient.GetBasicAddressSpaces(keywords);


        public void Dispose()
        {
            m_client.Dispose();
            restClient.Dispose();
        }

        /// <summary>
        /// Fakes paging support so the UI dev doesn't have to deal with it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="addressSpaces"></param>
        /// <param name="pageSize"></param>
        /// <param name="after"></param>
        /// <returns></returns>
        private static PageInfo<T> ConvertWithPaging<T>(List<T> addressSpaces, int pageSize = 0, int after = -1) where T : class
        {
            PageInfo<T> result = new PageInfo<T>();
            result.TotalCount = addressSpaces.Count;

            after++;
            if (pageSize == 0)
            {
                result.Page.hasNext = false;
                result.Page.hasPrev = false;

                for (int i = after; i < addressSpaces.Count; i++)
                {
                    PageItem<T> item = new PageItem<T>();
                    item.Item = addressSpaces[i];
                    item.Cursor = i.ToString();
                    result.Items.Add(item);
                }
            }
            else if (pageSize > 0)
            {
                if (after >= 0)
                {
                    result.Page.hasPrev = after > 0;
                    result.Page.hasNext = after + pageSize < addressSpaces.Count;
                    for (int i = after; i < addressSpaces.Count && i - after < pageSize; i++)
                    {
                        PageItem<T> item = new PageItem<T>();
                        item.Item = addressSpaces[i];
                        item.Cursor = i.ToString();
                        result.Items.Add(item);
                    }
                }
            }

            return result; 
        }
    }
}
