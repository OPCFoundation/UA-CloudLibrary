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
    using GraphQL.Query.Builder;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using UACloudLibClientLibrary.Models;
    using UACloudLibrary;

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
        public UACloudLibClient(string strUsername, string strPassword)
        {
            restClient = new RestClient(StandardEndpoint.ToString(), authentication);
            BaseEndpoint = StandardEndpoint;
            m_client = new GraphQLHttpClient(new Uri(BaseEndpoint + "/graphql"), new NewtonsoftJsonSerializer());
            string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(strUsername + ":" + strPassword));
            m_client.HttpClient.DefaultRequestHeaders.Add("Authorization", "basic " + auth);
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

        // Sends the query and converts it
        private async Task<T> SendAndConvert<T>(GraphQLRequest request)
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
        public async Task<List<ObjectResult>> GetObjectTypes()
        {
            IQuery<ObjectResult> objectQuery = new Query<ObjectResult>("objecttype")
                .AddField(f => f.ID)
                .AddField(f => f.NodesetID)
                .AddField(f => f.Namespace)
                .AddField(f => f.Browsename)
                .AddField(f => f.Value);
        
            request.Query = "query{" + objectQuery.Build() + "}";
            
            return await SendAndConvert<List<ObjectResult>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a list of metadata
        /// </summary>
        public async Task<List<MetadataResult>> GetMetadata()
        {
            IQuery<MetadataResult> metadataQuery = new Query<MetadataResult>("metadata")
                .AddField(f => f.ID)
                .AddField(f => f.NodesetID)
                .AddField(f => f.Name)
                .AddField(f => f.Value);

            request.Query = "query{" + metadataQuery.Build() + "}";
            
            return await SendAndConvert<List<MetadataResult>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a list of variabletypes
        /// </summary>
        public async Task<List<VariableResult>> GetVariables()
        {
            IQuery<VariableResult> variableQuery = new Query<VariableResult>("variabletype")
            .AddField(f => f.ID)
            .AddField(f => f.NodesetID)
            .AddField(f => f.Namespace)
            .AddField(f => f.Browsename)
            .AddField(f => f.Value);
        
            request.Query = "query{" + variableQuery.Build() + "}";
            
            return await SendAndConvert<List<VariableResult>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a list of referencetype
        /// </summary>
        public async Task<List<ReferenceResult>> GetReferencetype()
        {
            IQuery<ReferenceResult> referenceQuery = new Query<ReferenceResult>("referencetype")
                .AddField(f => f.ID)
                .AddField(f => f.NodesetID)
                .AddField(f => f.Namespace)
                .AddField(f => f.Browsename)
                .AddField(f => f.Value);
        
            request.Query = "query{" + referenceQuery.Build() + "}";
            
            return await SendAndConvert<List<ReferenceResult>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a list of datatype
        /// </summary>
        public async Task<List<DataResult>> GetDatatype()
        {
            IQuery<DataResult> dataQuery = new Query<DataResult>("datatype")
               .AddField(f => f.ID)
               .AddField(f => f.NodesetID)
               .AddField(f => f.Namespace)
               .AddField(f => f.Browsename)
               .AddField(f => f.Value);
        
            request.Query = "query{" + dataQuery.Build() + "}";
            
            return await SendAndConvert<List<DataResult>>(request).ConfigureAwait(false);
        }

        public async Task<List<AddressSpace>> GetConvertedMetadata()
        {
            List<AddressSpace> convertedResult = null;

            IQuery<MetadataResult> metadataQuery = new Query<MetadataResult>("metadata")
                .AddField(f => f.ID)
                .AddField(f => f.NodesetID)
                .AddField(f => f.Name)
                .AddField(f => f.Value);
        
            request.Query = "query{" + metadataQuery.Build() + "}";
            List<MetadataResult> result = await SendAndConvert<List<MetadataResult>>(request).ConfigureAwait(false);
            try
            {
                convertedResult = MetadataConverter.Convert(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message + " Falling back to REST interface...");
                List<BasicNodesetInformation> infos = await restClient.GetBasicNodesetInformation().ConfigureAwait(false);
                convertedResult.AddRange(MetadataConverter.Convert(infos));
            }

            return convertedResult;
        }

        /// <summary>
        /// Queries the organisations with the given filters.
        /// </summary>
        public async Task<List<Organisation>> GetOrganisations(int limit = 10, int offset = 0, IEnumerable<WhereExpression> filter = null)
        {
            IQuery<Organisation> organisationQuery = new Query<Organisation>("organisationtype")
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

            return await SendAndConvert<List<Organisation>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Queries the addressspaces with the given filters and converts the result
        /// </summary>
        public async Task<List<AddressSpace>> GetAddressSpaces(int limit = 10, int offset = 0, IEnumerable<WhereExpression> filter = null)
        {
            IQuery<AddressSpace> addressSpaceQuery = new Query<AddressSpace>("addressspacetype")
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
                .AddField(h => h.Version)
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
                result = await SendAndConvert<List<AddressSpace>>(request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message + " Falling back to REST interface...");
                List<BasicNodesetInformation> infos = await restClient.GetBasicNodesetInformation((List<string>)(filter?.Select(e => e.Value))).ConfigureAwait(false);
                result = MetadataConverter.ConvertWithPaging(infos, limit, offset);
            }

            return result;
        }

        /// <summary>
        /// Queries the categories with the given filters
        /// </summary>
        public async Task<List<AddressSpaceCategory>> GetAddressSpaceCategories(int limit = 10, int offset = 0, IEnumerable<WhereExpression> filter = null)
        {
            IQuery<AddressSpaceCategory> categoryQuery = new Query<AddressSpaceCategory>("categorytype")
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

            return await SendAndConvert<List<AddressSpaceCategory>>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Download chosen Nodeset with a REST call
        /// </summary>
        /// <param name="identifier"></param>
        public async Task<AddressSpace> DownloadNodeset(string identifier) => await restClient.DownloadNodeset(identifier).ConfigureAwait(false);

        /// <summary>
        /// Use this method if the CloudLib instance doesn't provide the GraphQL API
        /// </summary>
        public async Task<List<BasicNodesetInformation>> GetBasicNodesetInformation(List<string> keywords = null) => await restClient.GetBasicNodesetInformation(keywords).ConfigureAwait(false);


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
