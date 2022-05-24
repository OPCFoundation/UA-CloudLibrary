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

namespace Opc.Ua.Cloud.Library.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using global::Opc.Ua.Cloud.Library.Client.Models;
    using GraphQL;
    using GraphQL.Client.Http;
    using GraphQL.Client.Serializer.Newtonsoft;
    using GraphQL.Query.Builder;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;


    /// <summary>
    /// This class handles the quering and conversion of the response
    /// </summary>
    public partial class UACloudLibClient : IDisposable
    {
        /// <summary>The standard endpoint</summary>
        private static Uri _standardEndpoint = new Uri("https://uacloudlibrary.opcfoundation.org");

        private GraphQLHttpClient _client = null;
        private RestClient _restClient;
        private string _username = "";
        private string _password = "";

        private AuthenticationHeaderValue Authentication
        {
            set => _client.HttpClient.DefaultRequestHeaders.Authorization = value;
            get => _client.HttpClient.DefaultRequestHeaders.Authorization;
        }

        /// <summary>Gets the Base endpoint used to access the api.</summary>
        /// <value>The url of the endpoint</value>
        public Uri BaseEndpoint { get; private set; }

        /// <summary>Gets or sets the username.</summary>
        /// <value>The username.</value>
        public string Username { get { return _username; } set { _username = value; UserDataChanged(); } }

        /// <summary>Sets the password.</summary>
        /// <value>The password.</value>
        public string Password
        {
            set { _password = value; UserDataChanged(); }
        }

        /// <summary>
        /// Options to use in IOptions patterns
        /// </summary>
        public class Options
        {
            /// <summary>
            /// URL of the cloud library. Defaults to the OPC Foundation Cloud Library.
            /// </summary>
            public string Url { get; set; }
            /// <summary>
            /// Username to use for authenticating with the cloud library
            /// </summary>
            public string Username { get; set; }
            /// <summary>
            /// Password to use for authenticating with the cloud library
            /// </summary>
            public string Password { get; set; }
        }

        /// <summary>
        /// This Constructor uses the standard endpoint with no authorization
        /// </summary>
        public UACloudLibClient()
        {
            BaseEndpoint = _standardEndpoint;
            _client = new GraphQLHttpClient(new Uri(BaseEndpoint + "/graphql"), new NewtonsoftJsonSerializer());
            _restClient = new RestClient(_standardEndpoint);
        }

        /// <summary>
        /// This constructor uses the standard endpoint with authorization
        /// </summary>
        public UACloudLibClient(string strUsername, string strPassword)
            : this(_standardEndpoint.ToString(), strUsername, strPassword)
        {
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
            _client = new GraphQLHttpClient(new Uri(new Uri(strEndpoint), "/graphql"), new NewtonsoftJsonSerializer());
            string temp = Convert.ToBase64String(Encoding.UTF8.GetBytes(strUsername + ":" + strPassword));
            _client.HttpClient.DefaultRequestHeaders.Add("Authorization", "basic " + temp);
            _username = strUsername;
            _password = strPassword;
            _restClient = new RestClient(strEndpoint, Authentication);
        }
        /// <summary>Initializes a new instance of the <see cref="UACloudLibClient" /> class.</summary>
        /// <param name="options">Credentials and URL</param>
        public UACloudLibClient(Options options)
            : this(options.Url, options.Username, options.Password)
        {
        }

        /// <summary>
        /// Initializes a new instance using an existing HttpClient
        /// </summary>
        /// <param name="httpClient"></param>
        public UACloudLibClient(HttpClient httpClient)
        {
            _client = new GraphQLHttpClient(new GraphQLHttpClientOptions { EndPoint = new Uri(httpClient.BaseAddress, "graphql"), }, new NewtonsoftJsonSerializer(), httpClient);
            _restClient = new RestClient(httpClient);
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
            GraphQLResponse<JObject> response = await _client.SendQueryAsync<JObject>(request).ConfigureAwait(false);

            if (response?.Errors?.Length > 0)
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

            var request = new GraphQLRequest();
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

            var request = new GraphQLRequest();
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

            var request = new GraphQLRequest();
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

            var request = new GraphQLRequest();
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

            var request = new GraphQLRequest();
            request.Query = "query{" + dataQuery.Build() + "}";

            return await SendAndConvertAsync<List<DataResult>>(request).ConfigureAwait(false);
        }

        /// <summary>Gets the converted metadata.</summary>
        /// <returns>List of NameSpace</returns>
        public async Task<List<UANameSpace>> GetConvertedMetadataAsync()
        {
            List<UANameSpace> convertedResult = null;

            IQuery<MetadataResult> metadataQuery = new Query<MetadataResult>("metadata")
                .AddField(f => f.ID)
                .AddField(f => f.NodesetID)
                .AddField(f => f.Name)
                .AddField(f => f.Value);

            var request = new GraphQLRequest();
            request.Query = "query{" + metadataQuery.Build() + "}";
            List<MetadataResult> result = await SendAndConvertAsync<List<MetadataResult>>(request).ConfigureAwait(false);
            try
            {
                convertedResult = MetadataConverter.Convert(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message + " Falling back to REST interface...");
                List<UANodesetResult> infos = await _restClient.GetBasicNodesetInformationAsync().ConfigureAwait(false);
                convertedResult.AddRange(MetadataConverter.Convert(infos));
            }

            return convertedResult;
        }

        /// <summary>
        /// Queries the organisations with the given filters.
        /// </summary>
        public async Task<List<Organisation>> GetOrganisationsAsync(int limit = 10, IEnumerable<WhereExpression> filter = null)
        {
            IQuery<Organisation> organisationQuery = new Query<Organisation>("organisation")
                .AddField(f => f.Name)
                .AddField(f => f.Website)
                .AddField(f => f.ContactEmail)
                .AddField(f => f.Description)
                .AddField(f => f.LogoUrl);

            organisationQuery.AddArgument("limit", limit);

            if (filter != null)
            {
                organisationQuery.AddArgument("where", WhereExpression.Build(filter));
            }

            var request = new GraphQLRequest();
            request.Query = "query{" + organisationQuery.Build() + "}";

            return await SendAndConvertAsync<List<Organisation>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Queries the address spaces with the given filters and converts the result
        /// </summary>
        public async Task<List<UANameSpace>> GetNameSpacesAsync(int limit = 10, int offset = 0, IEnumerable<WhereExpression> filter = null)
        {
            IQuery<UANameSpace> nameSpaceQuery = new Query<UANameSpace>("nameSpace")
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
                .AddField(h => h.SupportedLocales)
                .AddField(
                    h => h.Nodeset,
                    sq => sq.AddField(h => h.NamespaceUri)
                            .AddField(h => h.PublicationDate)
                            .AddField(h => h.Identifier)
                    )
                ;

            nameSpaceQuery.AddArgument("limit", limit);
            nameSpaceQuery.AddArgument("offset", offset);

            if (filter != null)
            {
                nameSpaceQuery.AddArgument("where", WhereExpression.Build(filter));
            }

            var request = new GraphQLRequest();
            request.Query = "query{" + nameSpaceQuery.Build() + "}";

            List<UANameSpace> result = new List<UANameSpace>();
            try
            {
                result = await SendAndConvertAsync<List<UANameSpace>>(request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message + " Falling back to REST interface...");
                List<UANodesetResult> infos = await _restClient.GetBasicNodesetInformationAsync(filter?.Select(e => e.Value).ToList()).ConfigureAwait(false);
                result = MetadataConverter.ConvertWithPaging(infos, limit, offset);
            }

            return result;
        }

        /// <summary>
        /// Queries one or more node sets and their dependencies
        /// </summary>
        public async Task<List<Nodeset>> GetNodeSetDependencies(string identifier = null, string namespaceUri = null, DateTime? publicationDate = null)
        {
            var request = new GraphQLRequest();
            request.Query = @"
query MyQuery ($identifier: String, $namespaceUri: String, $publicationDate: DateTime) {
  nodeSets(identifier: $identifier, nodeSetUrl: $namespaceUri, publicationDate: $publicationDate) {
    nodes {
      modelUri
      publicationDate
      version
      identifier
      validationStatus
      requiredModels {
        modelUri
        publicationDate
        version
        availableModel {
          modelUri
          publicationDate
          version
          identifier
          requiredModels {
            modelUri
            publicationDate
            version
            availableModel {
              modelUri
              publicationDate
              version
              identifier
              requiredModels {
                modelUri
                publicationDate
                version
                availableModel {
                  modelUri
                  publicationDate
                  version
                  identifier
                }
              }
            }
          }
        }
      }
    }
  }
}
";
            request.Variables = new {
                identifier = identifier,
                namespaceUri = namespaceUri,
                publicationDate = publicationDate,
            };
            GraphQLNodeResponse<GraphQLRequiredNodeSet> result = null;
            result = await SendAndConvertAsync<GraphQLNodeResponse<GraphQLRequiredNodeSet>>(request).ConfigureAwait(false);
            var nodeSets = result?.nodes.Select(n => n.ToNodeSet()).ToList();
            return nodeSets;
        }

        /// <summary>
        /// Helper class to parse GraphQL connections
        /// </summary>
        /// <typeparam name="T"></typeparam>
        class GraphQLNodeResponse<T>
        {
            public List<T> nodes { get; set; }
        }

        /// <summary>
        /// Queries the categories with the given filters
        /// </summary>
        public async Task<List<Category>> GetNameSpaceCategoriesAsync(int limit = 10, IEnumerable<WhereExpression> filter = null)
        {
            IQuery<Category> categoryQuery = new Query<Category>("category")
                .AddField(f => f.Name)
                .AddField(f => f.Description)
                .AddField(f => f.IconUrl);

            categoryQuery.AddArgument("limit", limit);

            if (filter != null)
            {
                categoryQuery.AddArgument("where", WhereExpression.Build(filter));
            }

            var request = new GraphQLRequest();
            request.Query = "query{" + categoryQuery.Build() + "}";

            return await SendAndConvertAsync<List<Category>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Download chosen Nodeset with a REST call
        /// </summary>
        /// <param name="identifier"></param>
        public async Task<UANameSpace> DownloadNodesetAsync(string identifier) => await _restClient.DownloadNodesetAsync(identifier).ConfigureAwait(false);

        /// <summary>
        /// Use this method if the CloudLib instance doesn't provide the GraphQL API
        /// </summary>
        public async Task<List<UANodesetResult>> GetBasicNodesetInformationAsync(List<string> keywords = null) => await _restClient.GetBasicNodesetInformationAsync(keywords).ConfigureAwait(false);

        /// <summary>
        /// Gets all available namespaces and the corresponding node set identifier
        /// </summary>
        /// <returns></returns>
        public Task<(string NamespaceUri, string Identifier)[]> GetNamespaceIdsAsync() => _restClient.GetNamespaceIdsAsync();

        /// <summary>
        /// Upload a nodeset to the cloud library
        /// </summary>
        /// <param name="nameSpace"></param>
        /// <returns></returns>
        public Task<(HttpStatusCode Status, string Message)> UploadNodeSetAsync(UANameSpace nameSpace) => _restClient.UploadNamespaceAsync(nameSpace);

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            _client.Dispose();
            _restClient.Dispose();
        }

        private void UserDataChanged()
        {
            Authentication = new AuthenticationHeaderValue("basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(_username + ":" + _password)));
            _client.HttpClient.DefaultRequestHeaders.Authorization = Authentication;
        }
    }
}
