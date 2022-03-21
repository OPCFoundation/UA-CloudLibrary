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

namespace UACloudLibClientLibrary
{
    using GraphQL;
    using GraphQL.Client.Http;
    using GraphQL.Client.Serializer.Newtonsoft;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using UACloudLibClientLibrary.Models;

    /// <summary>
    /// This class handles the quering and conversion of the response
    /// </summary>
    public partial class UACloudLibClient : IDisposable
    {
        public static Uri StandardEndpoint = new Uri("https://uacloudlibrary.opcfoundation.org");

        private GraphQLHttpClient m_client = null;
        private GraphQLRequest request = new GraphQLRequest();
        private AuthenticationHeaderValue authentication 
        { 
            set => m_client.HttpClient.DefaultRequestHeaders.Authorization = value; 
            get => m_client.HttpClient.DefaultRequestHeaders.Authorization; 
        }

        public Uri Endpoint
        {
            get { return BaseEndpoint; }
            set { BaseEndpoint = value; }
        }

        private RestClient restClient;

        private Uri BaseEndpoint { get; set; }

        private string m_strUsername = "";
        private string m_strPassword = "";

        public string Username { get { return m_strUsername; } set { m_strUsername = value; UserDataChanged(); } }

        public string Password
        {
            set { m_strPassword = value; UserDataChanged(); }
        }

        /// <summary>
        /// This Constructor uses the standard endpoint with no authorization
        /// </summary>
        public UACloudLibClient()
        {
            BaseEndpoint = StandardEndpoint;
            m_client = new GraphQLHttpClient(new Uri(BaseEndpoint + "/graphql"), new NewtonsoftJsonSerializer());
            restClient = new RestClient(StandardEndpoint);
        }

        /// <summary>
        /// This constructor uses the standard endpoint with authorization
        /// </summary>
        /// <param name="strUsername"></param>
        /// <param name="strPassword"></param>
        public UACloudLibClient(string strUsername, string strPassword)
        {
            restClient = new RestClient(StandardEndpoint.ToString(), authentication);
            BaseEndpoint = StandardEndpoint;
            m_client = new GraphQLHttpClient(new Uri(BaseEndpoint + "/graphql"), new NewtonsoftJsonSerializer());
            string temp = Convert.ToBase64String(Encoding.UTF8.GetBytes(strUsername + ":" + strPassword));
            m_client.HttpClient.DefaultRequestHeaders.Add("Authorization", "basic " + temp);
        }

        public UACloudLibClient(string strEndpoint, string strUsername, string strPassword)
        {
            BaseEndpoint = new Uri(strEndpoint);
            m_client = new GraphQLHttpClient(new Uri(strEndpoint + "/graphql"), new NewtonsoftJsonSerializer());
            string temp = Convert.ToBase64String(Encoding.UTF8.GetBytes(strUsername + ":" + strPassword));
            m_client.HttpClient.DefaultRequestHeaders.Add("Authorization", "basic " + temp);
            m_strUsername = strUsername;
            m_strPassword = strPassword;
            restClient = new RestClient(strEndpoint, authentication);
        }

        //Sends the query and converts it
        private async Task<T> SendAndConvert<T>(GraphQLRequest request)
        {
            GraphQLResponse<JObject> response = await m_client.SendQueryAsync<JObject>(request);

            if (response?.Errors.Count() > 0)
            {
                throw new Exception(response.Errors[0].Message);
            }

            string dataJson = response.Data?.First?.First?.ToString();
            return JsonConvert.DeserializeObject<T>(dataJson);
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
            try
            {
                convertedResult = ConvertMetadataToAddressspace.Convert(result);
            }
            catch (Exception)
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
        public async Task<PageInfo<Organisation>> GetOrganisations(int limit = 10, int offset = 1, IEnumerable<OrganisationWhereExpression> filter = null)
        {
            request.Query = QueryMethods.QueryOrganisations(limit, offset, filter);
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
        public async Task<PageInfo<AddressSpace>> GetAddressSpaces(int limit = 10, int offset = 1, IEnumerable<AddressSpaceWhereExpression> filter = null)
        {
            request.Query = QueryMethods.AddressSpacesQuery(limit, offset, filter);
            PageInfo<AddressSpace> result = new PageInfo<AddressSpace>();
            try
            {
                result = await SendAndConvert<PageInfo<AddressSpace>>(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message + " Falling back to REST interface...");
                result = ConvertWithPaging(await restClient.GetBasicAddressSpaces((List<string>)(filter?.Select(e => e.Value))), limit, Convert.ToInt32(offset));
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
        public async Task<PageInfo<AddressSpaceCategory>> GetAddressSpaceCategories(int limit = 10, int offset = 1, IEnumerable<CategoryWhereExpression> filter = null)
        {
            request.Query = QueryMethods.QueryCategories(limit, offset, filter);
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
        public async Task<List<AddressSpace>> GetBasicAddressSpaces(List<string> keywords = null) => await restClient.GetBasicAddressSpaces(keywords);


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

        private void UserDataChanged()
        {
            authentication = new AuthenticationHeaderValue("basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(m_strUsername + ":" + m_strPassword)));
            m_client.HttpClient.DefaultRequestHeaders.Authorization = authentication;
        }
    }
}
