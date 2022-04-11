/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

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
    using Opc.Ua.CloudLib.Client;
    using Opc.Ua.CloudLib.Client.Models;
    using System.Threading.Tasks;

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

            await TestGraphQLInterface(args).ConfigureAwait(false);

            TestRESTInterface(args);

            await TestClientLibrary(args).ConfigureAwait(false);

            Console.WriteLine();
            Console.WriteLine("Done!");
        }

        private static async Task TestGraphQLInterface(string[] args)
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
            var response = await graphQLClient.SendQueryAsync<UACloudLibGraphQLObjecttypeQueryResponse>(request).ConfigureAwait(false);
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
            var response2 = await graphQLClient.SendQueryAsync<UACloudLibGraphQLMetadataQueryResponse>(request).ConfigureAwait(false);
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

        private static async Task TestClientLibrary(string[] args)
        {
            Console.WriteLine("\n\nTesting the client library");

            UACloudLibClient client = new UACloudLibClient(args[0], args[1], args[2]);

            Console.WriteLine("\nTesting object query");
            List<ObjectResult> test = await client.GetObjectTypes().ConfigureAwait(false);
            foreach (ObjectResult result in test)
            {
                Console.WriteLine($"{result.ID}, {result.Namespace}, {result.Browsename}, {result.Value}");
            }

            Console.WriteLine("\nTesting metadata query");
            List<MetadataResult> metadatas = await client.GetMetadata().ConfigureAwait(false);
            foreach (MetadataResult metadata in metadatas)
            {
                Console.WriteLine($"{metadata.ID}, {metadata.Name}, {metadata.Value}");
            }

            Console.WriteLine("\nTesting query and convertion of metadata");
            List<AddressSpace> finalResult = await client.GetConvertedResult().ConfigureAwait(false);
            foreach (AddressSpace result in finalResult)
            {
                Console.WriteLine($"{result.Title} by {result.Contributor.Name} last update on {result.LastModificationTime}");
            }

            if (finalResult.Count > 0)
            {
                Console.WriteLine("Testing download of nodeset");
                AddressSpace result = await client.DownloadNodeset(finalResult[0].MetadataID).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(result.Nodeset.NodesetXml))
                {
                    Console.WriteLine("Nodeset Downloaded");
                }
            }
        }
    }
}
