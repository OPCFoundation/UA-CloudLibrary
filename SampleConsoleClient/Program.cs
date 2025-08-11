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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Client;
using Opc.Ua.Cloud.Client.Models;

[assembly: CLSCompliant(false)]
namespace SampleConsoleClient
{
    public class Program
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
            await TestClientLibraryAsync(args).ConfigureAwait(false);

            Console.WriteLine();
            Console.WriteLine("Done!");
        }

        private static async Task TestRESTInterfaceAsync(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("Testing REST interface...");

            HttpClient webClient = new HttpClient {
                BaseAddress = new Uri(args[0])
            };

            webClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(args[1] + ":" + args[2])));

            Console.WriteLine();
            Console.WriteLine("Testing /infomodel/find?keywords");

            // return everything (keywords=*, other keywords are simply appended with "&keywords=UriEscapedKeyword2&keywords=UriEscapedKeyword3", etc.)
            Uri address = new Uri(webClient.BaseAddress, "infomodel/find?keywords=" + Uri.EscapeDataString("*"));
            HttpResponseMessage response = webClient.Send(new HttpRequestMessage(HttpMethod.Get, address));
            Console.WriteLine("Response: " + response.StatusCode.ToString());
            string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            UANameSpace[] nodesets = null;
            if (!string.IsNullOrEmpty(responseString))
            {
                nodesets = JsonConvert.DeserializeObject<UANameSpace[]>(responseString);
                for (int i = 0; i < nodesets.Length; i++)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(nodesets[i], Formatting.Indented));
                }
            }
            Console.WriteLine();
            Console.WriteLine("Testing /infomodel/download/{identifier}");

            if (nodesets != null)
            {
                // pick the first identifier returned previously
                string identifier = nodesets[0].Nodeset.Identifier.ToString(CultureInfo.InvariantCulture);
                address = new Uri(webClient.BaseAddress, "infomodel/download/" + Uri.EscapeDataString(identifier));
                response = webClient.Send(new HttpRequestMessage(HttpMethod.Get, address));

                Console.WriteLine("Response: " + response.StatusCode.ToString());
                string responseStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Console.WriteLine(responseStr);
            }
            else
            {
                Console.WriteLine("Skipped download test because of failure in previous test.");
            }

            webClient.Dispose();
        }

        private static async Task TestClientLibraryAsync(string[] args)
        {
            Console.WriteLine("\n\nTesting the client library");

            UACloudLibClient client = new UACloudLibClient(args[0], args[1], args[2]);

            List<UANameSpace> restResult = await client.GetBasicNodesetInformationAsync(0, 10).ConfigureAwait(false);
            if (restResult?.Count > 0)
            {
                Console.WriteLine("Passed.");
                Console.WriteLine("\nUsing the rest API");
                Console.WriteLine("Testing download of nodeset");

                UANameSpace result = await client.DownloadNodesetAsync(restResult[0].Nodeset.Identifier.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);

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
