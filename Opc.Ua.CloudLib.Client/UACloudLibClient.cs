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

namespace Opc.Ua.CloudLib.Client
{
    using global::Opc.Ua.CloudLib.Client.Models;
    using GraphQL;
    using GraphQL.Client.Http;
    using GraphQL.Client.Serializer.Newtonsoft;
    using GraphQL.Query.Builder;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;


    /// <summary>
    /// This class handles the quering and conversion of the response
    /// </summary>
    public partial class UACloudLibClient : IDisposable
    {
        /// <summary>The standard endpoint</summary>
        public static Uri StandardEndpoint = new Uri("https://uacloudlibrary.opcfoundation.org");

        private GraphQLHttpClient m_client = null;
        private GraphQLRequest request = new GraphQLRequest();

        private AuthenticationHeaderValue authentication
        {
            set => m_client.HttpClient.DefaultRequestHeaders.Authorization = value;
            get => m_client.HttpClient.DefaultRequestHeaders.Authorization;
        }

        /// <summary>Gets or sets the endpoint.</summary>
        /// <value>The endpoint.</value>
        public Uri Endpoint
        {
            get { return BaseEndpoint; }
            set { BaseEndpoint = value; }
        }

        private RestClient restClient;

        private Uri BaseEndpoint { get; set; }

        private string m_strUsername = "";
        private string m_strPassword = "";

        /// <summary>Gets or sets the username.</summary>
        /// <value>The username.</value>
        public string Username { get { return m_strUsername; } set { m_strUsername = value; UserDataChanged(); } }

        /// <summary>Sets the password.</summary>
        /// <value>The password.</value>
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
        public UACloudLibClient(string strUsername, string strPassword) : this(StandardEndpoint.ToString(), strUsername, strPassword)
        {
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
            restClient = new RestClient(strEndpoint, authentication);
        }

        /// <summary>Sends the GraphQL query and converts it to JSON</summary>
        /// <typeparam name="T">the JSON target tyoe</typeparam>
        /// <param name="request">The request.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        /// <exception cref="System.Exception"></exception>
        private async Task<T> SendAndConvertAsync<T>(GraphQLRequest request)
        {
            GraphQLResponse<JObject> response = await m_client.SendQueryAsync<JObject>(request).ConfigureAwait(false);

            if (response?.Errors?.Count() > 0)
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
        public async Task<List<ObjectResult>> GetObjectTypesAsync()
        {
            IQuery<ObjectResult> objectQuery = new Query<ObjectResult>("objectType")
                .AddField(f => f.ID)
                .AddField(f => f.NodesetID)
                .AddField(f => f.Namespace)
                .AddField(f => f.Browsename)
                .AddField(f => f.Value);

            request.Query = "query{" + objectQuery.Build() + "}";

            return await SendAndConvertAsync<List<ObjectResult>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a list of metadata
        /// </summary>
        public async Task<List<MetadataResult>> GetMetadataAsync()
        {
            IQuery<MetadataResult> metadataQuery = new Query<MetadataResult>("metadata")
                .AddField(f => f.ID)
                .AddField(f => f.NodesetID)
                .AddField(f => f.Name)
                .AddField(f => f.Value);

            request.Query = "query{" + metadataQuery.Build() + "}";
            
            return await SendAndConvertAsync<List<MetadataResult>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a list of variabletypes
        /// </summary>
        /// <returns></returns>
        public async Task<List<VariableResult>> GetVariablesAsync()
        {
            IQuery<VariableResult> variableQuery = new Query<VariableResult>("variabletype")
            .AddField(f => f.ID)
            .AddField(f => f.NodesetID)
            .AddField(f => f.Namespace)
            .AddField(f => f.Browsename)
            .AddField(f => f.Value);

            request.Query = "query{" + variableQuery.Build() + "}";
            
            return await SendAndConvertAsync<List<VariableResult>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a list of referencetype
        /// </summary>
        /// <returns></returns>
        public async Task<List<ReferenceResult>> GetReferencetypeAsync()
        {
            IQuery<ReferenceResult> referenceQuery = new Query<ReferenceResult>("referencetype")
                .AddField(f => f.ID)
                .AddField(f => f.NodesetID)
                .AddField(f => f.Namespace)
                .AddField(f => f.Browsename)
                .AddField(f => f.Value);

            request.Query = "query{" + referenceQuery.Build() + "}";
            
            return await SendAndConvertAsync<List<ReferenceResult>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a list of datatype
        /// </summary>
        public async Task<List<DataResult>> GetDatatypeAsync()
        {
            IQuery<DataResult> dataQuery = new Query<DataResult>("datatype")
               .AddField(f => f.ID)
               .AddField(f => f.NodesetID)
               .AddField(f => f.Namespace)
               .AddField(f => f.Browsename)
               .AddField(f => f.Value);

            request.Query = "query{" + dataQuery.Build() + "}";
            
            return await SendAndConvertAsync<List<DataResult>>(request).ConfigureAwait(false);
        }

        /// <summary>Gets the converted metadata.</summary>
        /// <returns>List of AddressSpace</returns>
        public async Task<List<AddressSpace>> GetConvertedMetadataAsync()
        {
            List<AddressSpace> convertedResult = null;

            IQuery<MetadataResult> metadataQuery = new Query<MetadataResult>("metadata")
                .AddField(f => f.ID)
                .AddField(f => f.NodesetID)
                .AddField(f => f.Name)
                .AddField(f => f.Value);

            request.Query = "query{" + metadataQuery.Build() + "}";
            List<MetadataResult> result = await SendAndConvertAsync<List<MetadataResult>>(request).ConfigureAwait(false);
            try
            {
                convertedResult = MetadataConverter.Convert(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message + " Falling back to REST interface...");
                List<UANodesetResult> infos = await restClient.GetBasicNodesetInformation().ConfigureAwait(false);
                convertedResult.AddRange(MetadataConverter.Convert(infos));
            }

            return convertedResult;
        }

        /// <summary>
        /// Queries the organisations with the given filters.
        /// </summary>
        public async Task<List<Organisation>> GetOrganisationsAsync(int limit = 10, int offset = 0, IEnumerable<WhereExpression> filter = null)
        {
            IQuery<Organisation> organisationQuery = new Query<Organisation>("organisation")
                .AddField(f => f.Name)
                .AddField(f => f.Website)
                .AddField(f => f.ContactEmail)
                .AddField(f => f.Description)
                .AddField(f => f.LogoUrl);

            organisationQuery.AddArgument("limit", limit);
            organisationQuery.AddArgument("offset", offset);

            if (filter != null)
            {
                organisationQuery.AddArgument("where", WhereExpression.Build(filter));
            }

            request.Query = "query{" + organisationQuery.Build() + "}";

            return await SendAndConvertAsync<List<Organisation>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Queries the address spaces with the given filters and converts the result
        /// </summary>
        public async Task<List<AddressSpace>> GetAddressSpacesAsync(int limit = 10, int offset = 0, IEnumerable<WhereExpression> filter = null)
        {
            IQuery<AddressSpace> addressSpaceQuery = new Query<AddressSpace>("addressSpace")
                .AddField(h => h.Title)
                .AddField(
                    h => h.Contributor,
                    sq => sq.AddField(h => h.Name)
                            .AddField(h => h.ContactEmail)
                            .AddField(h => h.Website)
                            .AddField(h => h.LogoUrl)
                            .AddField(h => h.Description)
                    )
                .AddField(h => h.License)
                .AddField(
                    h => h.Category,
                    sq => sq.AddField(h => h.Name)
                            .AddField(h => h.Description)
                            .AddField(h => h.IconUrl)
                    )
                .AddField(h => h.Description)
                .AddField(h => h.DocumentationUrl)
                .AddField(h => h.PurchasingInformationUrl)
                .AddField(h => h.ReleaseNotesUrl)
                .AddField(h => h.Keywords)
                .AddField(h => h.SupportedLocales);

            addressSpaceQuery.AddArgument("limit", limit);
            addressSpaceQuery.AddArgument("offset", offset);

            if (filter != null)
            {
                addressSpaceQuery.AddArgument("where", WhereExpression.Build(filter));
            }

            request.Query = "query{" + addressSpaceQuery.Build() + "}";

            List<AddressSpace> result = new List<AddressSpace>();
            try
            {
                result = await SendAndConvertAsync<List<AddressSpace>>(request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message + " Falling back to REST interface...");
                List<UANodesetResult> infos = await restClient.GetBasicNodesetInformation((List<string>)(filter?.Select(e => e.Value))).ConfigureAwait(false);
                result = MetadataConverter.ConvertWithPaging(infos, limit, offset);
            }

            return result;
        }

        /// <summary>
        /// Queries the categories with the given filters
        /// </summary>
        public async Task<List<Category>> GetAddressSpaceCategoriesAsync(int limit = 10, int offset = 0, IEnumerable<WhereExpression> filter = null)
        {
            IQuery<Category> categoryQuery = new Query<Category>("category")
                .AddField(f => f.Name)
                .AddField(f => f.Description)
                .AddField(f => f.IconUrl);

            categoryQuery.AddArgument("limit", limit);
            categoryQuery.AddArgument("offset", offset);

            if (filter != null)
            {
                categoryQuery.AddArgument("where", WhereExpression.Build(filter));
            }

            request.Query = "query{" + categoryQuery.Build() + "}";

            return await SendAndConvertAsync<List<Category>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Download chosen Nodeset with a REST call
        /// </summary>
        /// <param name="identifier"></param>
        public async Task<AddressSpace> DownloadNodesetAsync(string identifier) => await restClient.DownloadNodeset(identifier).ConfigureAwait(false);

        /// <summary>
        /// Use this method if the CloudLib instance doesn't provide the GraphQL API
        /// </summary>
        public async Task<List<UANodesetResult>> GetBasicNodesetInformationAsync(List<string> keywords = null) => await restClient.GetBasicNodesetInformation(keywords).ConfigureAwait(false);

        /// <summary>
        /// Gets all available namespaces and the corresponding node set identifier
        /// </summary>
        /// <returns></returns>
        public Task<(string namespaceUri, string identifier)[]> GetNamespacesAsync() => restClient.GetNamespacesAsync();

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            m_client.Dispose();
            restClient.Dispose();
        }

        private void UserDataChanged()
        {
            authentication = new AuthenticationHeaderValue("basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(m_strUsername + ":" + m_strPassword)));
            m_client.HttpClient.DefaultRequestHeaders.Authorization = authentication;
        }
    }
}