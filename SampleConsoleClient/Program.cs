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
    using System.Linq;
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

            var objectTypeQuery = @"query {
                            objectType {
                                browseName
                                value
                                nameSpace
                                nodesetId
                            }
                        }";
            try
            {
                Console.WriteLine();
                Console.WriteLine("Testing graphql objectType via plain HTTPClient");

                HttpClient webClient = new HttpClient {
                    BaseAddress = new Uri(args[0])
                };

                webClient.DefaultRequestHeaders.Add("Authorization", "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(args[1] + ":" + args[2])));
                var queryBodyJson = JsonConvert.SerializeObject(new JObject { { "query", objectTypeQuery } });
                Uri address = new Uri(webClient.BaseAddress, "graphql");
                var response2 = webClient.Send(new HttpRequestMessage(HttpMethod.Post, address) {
                    Content = new StringContent(queryBodyJson, null, "application/json"),
                });
                Console.WriteLine("Response: " + response2.StatusCode.ToString());
                var responseString = response2.Content.ReadAsStringAsync().Result;
                if (!response2.IsSuccessStatusCode)
                {
                    Console.WriteLine($"HTTP status: {response2.StatusCode} {responseString}");
                }
                if (!string.IsNullOrEmpty(responseString))
                {
                    var parsedJson = JsonConvert.DeserializeObject<JObject>(responseString);
                    var responseJson = parsedJson["data"];
                    Console.WriteLine(JsonConvert.SerializeObject(responseJson, Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            GraphQLHttpClientOptions options = new GraphQLHttpClientOptions {
                EndPoint = new Uri(new Uri(args[0]), "/graphql"),
                HttpMessageHandler = new MessageHandlerWithAuthHeader(args[1], args[2])
            };

            GraphQLHttpClient graphQLClient = new GraphQLHttpClient(options, new NewtonsoftJsonSerializer());
            try
            {
                Console.WriteLine();
                Console.WriteLine("Testing objectType query (the other OPC UA types are very similar!) via GraphQLHttpClient client");
                GraphQLRequest request = new GraphQLRequest {
                    Query = objectTypeQuery
                };
                GraphQLResponse<JObject> response = await graphQLClient.SendQueryAsync<JObject>(request).ConfigureAwait(false);
                Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            try
            {
                Console.WriteLine();
                Console.WriteLine("Testing metadata query");
                var request = new GraphQLRequest {
                    Query = @"query {
                            metadata {
                                name
                                value
                                nodesetId
                            }
                        }"
                };

                var response = await graphQLClient.SendQueryAsync<JObject>(request).ConfigureAwait(false);
                Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            try
            {
                Console.WriteLine();
                Console.WriteLine("Testing NameSpace query");
                var request = new GraphQLRequest {
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

                var response = await graphQLClient.SendQueryAsync<JObject>(request).ConfigureAwait(false);
                Console.WriteLine(JsonConvert.SerializeObject(response.Data, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
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
            Uri address = new Uri(webClient.BaseAddress, "infomodel/find?keywords=" + Uri.EscapeDataString("*"));
            var response = webClient.Send(new HttpRequestMessage(HttpMethod.Get, address));
            Console.WriteLine("Response: " + response.StatusCode.ToString());
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            UANodesetResult[] identifiers = null;
            if (!string.IsNullOrEmpty(responseString))
            {
                identifiers = JsonConvert.DeserializeObject<UANodesetResult[]>(responseString);
                for (var i = 0; i < identifiers.Length; i++)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(identifiers[i], Formatting.Indented));
                }
            }
            Console.WriteLine();
            Console.WriteLine("Testing /infomodel/download/{identifier}");

            if (identifiers != null)
            {
                // pick the first identifier returned previously
                string identifier = identifiers[0].Id.ToString(CultureInfo.InvariantCulture);
                address = new Uri(webClient.BaseAddress, "infomodel/download/" + Uri.EscapeDataString(identifier));
                response = webClient.Send(new HttpRequestMessage(HttpMethod.Get, address));

                Console.WriteLine("Response: " + response.StatusCode.ToString());
                var responseStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Console.WriteLine(responseStr);
            }
            else
            {
                Console.WriteLine("Skipped download test because of failure in previous test.");
            }
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
#pragma warning disable CS0618 // Type or member is obsolete
                List<UANameSpace> nameSpaces = await client.GetNameSpacesAsync(10, 0, filter).ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete
                if (nameSpaces?.Count > 0)
                {
                    Console.WriteLine("Title: {0}", nameSpaces[0].Title);
                    Console.WriteLine("Total number of address spaces: {0}", nameSpaces.Count);
                }
                else
                {
                    Console.WriteLine("Error: no namespaces returned.");
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
                List<UANameSpace> finalResult = await client.GetConvertedMetadataAsync(0, 100).ConfigureAwait(false);
                foreach (UANameSpace result in finalResult)
                {
                    Console.WriteLine($"{result.Title} by {result.Contributor.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            List<UANodesetResult> restResult = await client.GetBasicNodesetInformationAsync(0, 100).ConfigureAwait(false);
            if (restResult?.Count > 0)
            {

                Console.WriteLine();
                Console.WriteLine("Testing nodeset dependency query");
                var identifier = restResult[0].Id.ToString(CultureInfo.InvariantCulture);
                var nodeSets = await client.GetNodeSetDependencies(identifier: identifier).ConfigureAwait(false);
                if (nodeSets?.Count > 0)
                {
                    foreach (var nodeSet in nodeSets)
                    {
                        Console.WriteLine($"Dependencies for {nodeSet.Identifier} {nodeSet.NamespaceUri} {nodeSet.PublicationDate} ({nodeSet.Version}):");
                        foreach (var requiredNodeSet in nodeSet.RequiredModels)
                        {
                            Console.WriteLine($"Required: {requiredNodeSet.NamespaceUri} {requiredNodeSet.PublicationDate} ({requiredNodeSet.Version}). Available in Cloud Library: {requiredNodeSet.AvailableModel?.Identifier} {requiredNodeSet.AvailableModel?.PublicationDate} ({requiredNodeSet.AvailableModel?.Version})");
                        }
                    }
                }
                Console.WriteLine();
                Console.WriteLine("Testing nodeset dependency query by namespace and publication date");
                var namespaceUri = restResult[0].NameSpaceUri;
                var publicationDate = restResult[0].PublicationDate.HasValue && restResult[0].PublicationDate.Value.Kind == DateTimeKind.Unspecified ?
                    DateTime.SpecifyKind(restResult[0].PublicationDate.Value, DateTimeKind.Utc)
                    : restResult[0].PublicationDate;
                var nodeSetsByNamespace = await client.GetNodeSetDependencies(modelUri: namespaceUri, publicationDate: publicationDate).ConfigureAwait(false);
                var dependenciesByNamespace = nodeSetsByNamespace
                    .SelectMany(n => n.RequiredModels).Where(r => r != null)
                    .Select(r => (r.AvailableModel?.Identifier, r.NamespaceUri, r.PublicationDate))
                    .OrderBy(m => m.Identifier).ThenBy(m => m.NamespaceUri).Distinct()
                    .ToList();
                var dependenciesByIdentifier = nodeSets
                    .SelectMany(n => n.RequiredModels).Where(r => r != null)
                    .Select(r => (r.AvailableModel?.Identifier, r.NamespaceUri, r.PublicationDate))
                    .OrderBy(m => m.Identifier).ThenBy(m => m.NamespaceUri).Distinct()
                    .ToList();
                if (!dependenciesByIdentifier.SequenceEqual(dependenciesByNamespace))
                {
                    Console.WriteLine($"FAIL: returned dependencies are different.");
                    Console.WriteLine($"For identifier {identifier}: {string.Join(" ", dependenciesByIdentifier)}.");
                    Console.WriteLine($"For namespace {namespaceUri} / {publicationDate}: {string.Join(" ", dependenciesByNamespace)}");
                }
                else
                {
                    Console.WriteLine("Passed.");
                }

                Console.WriteLine("\nUsing the rest API");
                Console.WriteLine("Testing download of nodeset");
                UANameSpace result = await client.DownloadNodesetAsync(restResult[0].Id.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(result.Nodeset.NodesetXml))
                {
                    Console.WriteLine("Nodeset Downloaded");
                    Console.WriteLine(result.Nodeset.NodesetXml);
                }
            }
            else
            {
                Console.WriteLine("Skipped download test because of failure in previous test.");
            }
        }
    }
}
