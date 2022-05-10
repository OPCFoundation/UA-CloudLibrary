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

namespace SampleConsoleClient
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using GraphQL;
    using GraphQL.Client.Http;
    using GraphQL.Client.Serializer.Newtonsoft;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Opc.Ua.Cloud.Library.Client;
    using Opc.Ua.Cloud.Library.Client.Models;

    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine();
                Console.WriteLine("Usage: SampleConsoleClient <UA Cloud Library instance URL> <username> <password>");
                return;
            }

            Console.WriteLine("OPC Foundation UA Cloud Library Console Client Application");

            await TestRESTInterfaceAsync(args).ConfigureAwait(false);
            await TestGraphQLInterfaceAsync(args).ConfigureAwait(false);
            await TestClientLibraryAsync(args).ConfigureAwait(false);

            Console.WriteLine();
            Console.WriteLine("Done!");
        }

        private static async Task TestGraphQLInterfaceAsync(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("Testing GraphQL interface (see https://graphql.org/learn/ for details)...");

            GraphQLHttpClientOptions options = new GraphQLHttpClientOptions {
                EndPoint = new Uri(args[0] + "/graphql"),
                HttpMessageHandler = new MessageHandlerWithAuthHeader(args[1], args[2])
            };

            GraphQLHttpClient graphQLClient = new GraphQLHttpClient(options, new NewtonsoftJsonSerializer());

            Console.WriteLine();
            Console.WriteLine("Testing objectType query (the other OPC UA types are very similar!)");
            GraphQLRequest request = new GraphQLRequest {
                Query = @"query {
                            objectType {
                                browseName
                                value
                                nameSpace
                                nodesetId
                            }
                        }"
            };

            GraphQLResponse<JObject> response = await graphQLClient.SendQueryAsync<JObject>(request).ConfigureAwait(false);
            Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));

            Console.WriteLine();
            Console.WriteLine("Testing metadata query");
            request = new GraphQLRequest {
                Query = @"query {
                            metadata {
                                name
                                value
                                nodesetId
                            }
                        }"
            };

            response = await graphQLClient.SendQueryAsync<JObject>(request).ConfigureAwait(false);
            Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));

            Console.WriteLine();
            Console.WriteLine("Testing NameSpace query");
            request = new GraphQLRequest {
                Query = @"query {
                            nameSpace(
                                limit: 10
                                offset: 0
                                where: ""[{ 'orgname': { 'like': 'microsoft' }}]""
                                orderBy: ""title""
                            ) {
                                title
                                contributor {
                                    name
                                    contactEmail
                                    website
                                    logoUrl
                                    description
                                }
                                license
                                category {
                                    name
                                    description
                                    iconUrl
                                }
                                description
                                documentationUrl
                                purchasingInformationUrl
                                releaseNotesUrl
                                keywords
                                supportedLocales
                                nodeset
                                {
                                    version
                                    identifier
                                    namespaceUri
                                    publicationDate
                                    lastModifiedDate
                                }
                            }
                        }"
            };

            response = await graphQLClient.SendQueryAsync<JObject>(request).ConfigureAwait(false);
            Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));

            graphQLClient.Dispose();
        }

        private static async Task TestRESTInterfaceAsync(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("Testing REST interface...");

            HttpClient webClient = new HttpClient {
                BaseAddress = new Uri(args[0])
            };

            webClient.DefaultRequestHeaders.Add("Authorization", "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(args[1] + ":" + args[2])));

            Console.WriteLine();
            Console.WriteLine("Testing /infomodel/find?keywords");

            // return everything (keywords=*, other keywords are simply appended with "&keywords=UriEscapedKeyword2&keywords=UriEscapedKeyword3", etc.)
            string address = webClient.BaseAddress.ToString() + "infomodel/find?keywords=" + Uri.EscapeDataString("*");
            var response = webClient.Send(new HttpRequestMessage(HttpMethod.Get, address));
            Console.WriteLine("Response: " + response.StatusCode.ToString());
            UANodesetResult[] identifiers = JsonConvert.DeserializeObject<UANodesetResult[]>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            for (var i = 0; i < identifiers.Length; i++)
            {
                Console.WriteLine(JsonConvert.SerializeObject(identifiers[i], Formatting.Indented));
            }

            Console.WriteLine();
            Console.WriteLine("Testing /infomodel/download/{identifier}");

            // pick the first identifier returned previously
            string identifier = identifiers[0].Id.ToString(CultureInfo.InvariantCulture);
            address = webClient.BaseAddress.ToString() + "infomodel/download/" + Uri.EscapeDataString(identifier);
            response = webClient.Send(new HttpRequestMessage(HttpMethod.Get, address));

            Console.WriteLine("Response: " + response.StatusCode.ToString());
            Console.WriteLine(response.Content.ReadAsStringAsync().ConfigureAwait(false));

            Console.WriteLine();
            Console.WriteLine("For sample code to test /infomodel/upload, see https://github.com/digitaltwinconsortium/UANodesetWebViewer/blob/main/Applications/Controllers/UACL.cs");

            webClient.Dispose();
        }

        private static async Task TestClientLibraryAsync(string[] args)
        {
            Console.WriteLine("\n\nTesting the client library");

            UACloudLibClient client = new UACloudLibClient(args[0], args[1], args[2]);
            try
            {
                Console.WriteLine("\nTesting the GraphQL API");

                Console.WriteLine("\nTesting the namespace query, this will fall back to the REST interface if GraphQL is not available.");
                List<WhereExpression> filter = new List<WhereExpression>();
                filter.Add(new WhereExpression(SearchField.orgname, "microsoft", ComparisonType.like));
                List<UANameSpace> nameSpaces = await client.GetNameSpacesAsync(10, 0, filter).ConfigureAwait(false);
                if (nameSpaces.Count > 0)
                {
                    Console.WriteLine("Title: {0}", nameSpaces[0].Title);
                    Console.WriteLine("Total number of address spaces: {0}", nameSpaces.Count);
                }

                Console.WriteLine("\nTesting object query");
                List<ObjectResult> objects = await client.GetObjectTypesAsync().ConfigureAwait(false);
                foreach (ObjectResult result in objects)
                {
                    Console.WriteLine($"{result.ID}, {result.Namespace}, {result.Browsename}, {result.Value}");
                }

                Console.WriteLine("\nTesting metadata query");
                List<MetadataResult> metadata = await client.GetMetadataAsync().ConfigureAwait(false);
                foreach (MetadataResult entry in metadata)
                {
                    Console.WriteLine($"{entry.ID}, {entry.Name}, {entry.Value}");
                }

                Console.WriteLine("\nTesting query and convertion of metadata");
                List<UANameSpace> finalResult = await client.GetConvertedMetadataAsync().ConfigureAwait(false);
                foreach (UANameSpace result in finalResult)
                {
                    Console.WriteLine($"{result.Title} by {result.Contributor.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("\nUsing the rest API");
            List<UANodesetResult> restResult = await client.GetBasicNodesetInformationAsync().ConfigureAwait(false);
            if (restResult.Count > 0)
            {
                Console.WriteLine("Testing download of nodeset");
                UANameSpace result = await client.DownloadNodesetAsync(restResult[0].Id.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(result.Nodeset.NodesetXml))
                {
                    Console.WriteLine("Nodeset Downloaded");
                    Console.WriteLine(result.Nodeset.NodesetXml);
                }
            }
        }
    }
}
