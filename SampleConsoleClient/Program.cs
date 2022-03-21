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
    using GraphQL;
    using GraphQL.Client.Http;
    using GraphQL.Client.Serializer.Newtonsoft;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using UACloudLibClientLibrary;

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine();
                Console.WriteLine("Usage: SampleConsoleClient <UA Cloud Library instance URL> <username> <password>");
                return;
            }

            Console.WriteLine("OPC Foundation UA Cloud Library Console Client Application");

            TestGraphQLInterface(args);

            TestRESTInterface(args);

            TestClientLibrary(args);

            Console.WriteLine();
            Console.WriteLine("Done!");
        }

        private static void TestGraphQLInterface(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("Testing GraphQL interface (see https://graphql.org/learn/ for details)...");

            GraphQLHttpClientOptions options = new GraphQLHttpClientOptions
            {
                EndPoint = new Uri(args[0] + "/graphql"),
                HttpMessageHandler = new MessageHandlerWithAuthHeader(args[1], args[2])
            };

            GraphQLHttpClient graphQLClient = new GraphQLHttpClient(options, new NewtonsoftJsonSerializer());

            Console.WriteLine();
            Console.WriteLine("Testing objecttype query (the other OPC UA types are very similar!)");
            GraphQLRequest request = new GraphQLRequest
            {
                Query = @"
                query {
                    objecttype
                    {
                        objecttype_browsename objecttype_value objecttype_namespace nodeset_id
                    }
                }"

            };
            var response = graphQLClient.SendQueryAsync<UACloudLibGraphQLObjecttypeQueryResponse>(request).GetAwaiter().GetResult();
            Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));

            Console.WriteLine();
            Console.WriteLine("Testing metadata query");
            request = new GraphQLRequest
            {
                Query = @"
                query {
                    metadata
                    {
                        metadata_name metadata_value nodeset_id
                    }
                }"

            };
            var response2 = graphQLClient.SendQueryAsync<UACloudLibGraphQLMetadataQueryResponse>(request).GetAwaiter().GetResult();
            Console.WriteLine(JsonConvert.SerializeObject(response2.Data, Formatting.Indented));

            graphQLClient.Dispose();
        }

        private static void TestRESTInterface(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("Testing REST interface...");

            HttpClient webClient = new HttpClient
            {
                BaseAddress = new Uri(args[0])
            };

            webClient.DefaultRequestHeaders.Add("Authorization", "basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(args[1] + ":" + args[2])));

            Console.WriteLine();
            Console.WriteLine("Testing /infomodel/find?keywords");
            string keywords = "*"; // return everything (other keywords are simply appended with "&keywords=UriEscapedKeyword2&keywords=UriEscapedKeyword3", etc.)
            string address = webClient.BaseAddress.ToString() + "infomodel/find?keywords=" + Uri.EscapeDataString(keywords);
            HttpContent content = new StringContent(JsonConvert.SerializeObject(keywords), Encoding.UTF8, "application/json");
            var response = webClient.Send(new HttpRequestMessage(HttpMethod.Get, address));
            Console.WriteLine("Response: " + response.StatusCode.ToString());
            UANodesetResult[] identifiers = JsonConvert.DeserializeObject<UANodesetResult[]>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            for (var i = 0; i < identifiers.Length; i++)
            {
                Console.WriteLine(JsonConvert.SerializeObject(identifiers[i], Formatting.Indented));
            }

            Console.WriteLine();
            Console.WriteLine("Testing /infomodel/download/{identifier}");
            string identifier = identifiers[0].Id.ToString(); // pick the first identifier returned previously
            address = webClient.BaseAddress.ToString() + "infomodel/download/" + Uri.EscapeDataString(identifier);
            response = webClient.Send(new HttpRequestMessage(HttpMethod.Get, address));
            Console.WriteLine("Response: " + response.StatusCode.ToString());
            Console.WriteLine(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            Console.WriteLine();
            Console.WriteLine("For sample code to test /infomodel/upload, see https://github.com/digitaltwinconsortium/UANodesetWebViewer/blob/main/Applications/Controllers/UACL.cs");

            webClient.Dispose();
        }

        private static void TestClientLibrary(string[] args)
        {
            Console.WriteLine("\n\nTesting the client library");

            UACloudLibClient client = new UACloudLibClient(args[0], args[1], args[2]);
            try
            {
                Console.WriteLine("\nTesting the GraphQL api");

                Console.WriteLine("\nTesting the addressspace query, this should also work if graphql is not available");

                List<AddressSpaceWhereExpression> expression = new List<AddressSpaceWhereExpression>();
                PageInfo<AddressSpace> anothertest = client.GetAddressSpaces(10).GetAwaiter().GetResult();
                if(anothertest.Items.Count > 0)
                {
                    Console.WriteLine("Title: {0}", anothertest.Items[0].Item.Title);
                    Console.WriteLine("Total amount of adressspaces: {0}", anothertest.TotalCount);
                }

                Console.WriteLine("\nTesting object query");
                
                PageInfo<ObjectResult> test = client.GetObjectTypes().GetAwaiter().GetResult();
                foreach (PageItem<ObjectResult> result in test.Items)
                {
                    Console.WriteLine($"{result.Item.ID}, {result.Item.Namespace}, {result.Item.Browsename}, {result.Item.Value}");
                }

                Console.WriteLine("\nTesting metadata query");
                PageInfo<MetadataResult> metadatas = client.GetMetadata().GetAwaiter().GetResult();
                foreach (PageItem<MetadataResult> metadata in metadatas.Items)
                {
                    Console.WriteLine($"{metadata.Item.ID}, {metadata.Item.Name}, {metadata.Item.Value}");
                }

                Console.WriteLine("\nTesting query and convertion of metadata");
                List<AddressSpace> finalResult = client.GetConvertedMetadata().GetAwaiter().GetResult();
                foreach (AddressSpace result in finalResult)
                {
                    Console.WriteLine($"{result.Title} by {result.Contributor.Name}");
                }
            }catch
            {
                Console.WriteLine("\nThis instance doesn't provide GraphQL");
            }

            Console.WriteLine("\nUsing the rest api");

            List<AddressSpace> restResult = client.GetBasicAddressSpaces().GetAwaiter().GetResult();
            if (restResult.Count > 0)
            {
                Console.WriteLine("Testing download of nodeset");
                AddressSpace result = client.DownloadNodeset(restResult[0].MetadataID).GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(result.Nodeset.NodesetXml))
                {
                    Console.WriteLine("Nodeset Downloaded");
                    Console.WriteLine(result.Nodeset.NodesetXml);
                }
            }
        }
    }
}
