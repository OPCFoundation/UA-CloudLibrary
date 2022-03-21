using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace UACloudLibClientLibrary
{
    /// <summary>
    /// For use when the provider doesn't have a GraphQL interface and the downloading of nodesets
    /// </summary>
    internal class RestClient : IDisposable
    {
        private HttpClient client;
        public AuthenticationHeaderValue Authentication { set => client.DefaultRequestHeaders.Authorization = value; get => client.DefaultRequestHeaders.Authorization; }

        public RestClient(Uri address)
        {
            client = new HttpClient();
            client.BaseAddress = address;
        }

        public RestClient(string address, AuthenticationHeaderValue authentication)
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(address);
            client.DefaultRequestHeaders.Authorization = authentication;        }

        public void Dispose()
        {
            client.Dispose();
        }

        public async Task<List<AddressSpace>> GetBasicAddressSpaces(IEnumerable<string> keywords = null)
        {
            string address = Path.Combine(client.BaseAddress.ToString(), "infomodel/find");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, address);
            if (keywords == null)
            {
                request.Content = new StringContent("[\"*\"]");
            }
            else
            {
                request.Content = new StringContent(string.Format("[{0}]", PrepareArgumentsString(keywords)));
            }

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Authorization = client.DefaultRequestHeaders.Authorization;
            HttpResponseMessage response = await client.SendAsync(request);
            List<BasicNodesetInformation> info = null;
            
            if(response.StatusCode == HttpStatusCode.OK)
            {
                info = JsonConvert.DeserializeObject<List<BasicNodesetInformation>>(await response.Content.ReadAsStringAsync());
            }
            return ConvertToAddressSpace(info);
        }

        public async Task<AddressSpace> DownloadNodeset(string identifier)
        {
            string address = Path.Combine(client.BaseAddress.ToString(), "infomodel/download/", Uri.EscapeDataString(identifier));
            HttpResponseMessage response = await client.GetAsync(address);
            AddressSpace resultType = null;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                resultType = JsonConvert.DeserializeObject<AddressSpace>(await response.Content.ReadAsStringAsync());
            }
            return resultType;
        }

        private List<AddressSpace> ConvertToAddressSpace(List<BasicNodesetInformation> Info)
        {
            List<AddressSpace> result = new List<AddressSpace>();
            if (Info != null)
            {
                foreach (BasicNodesetInformation basicNodesetInformation in Info)
                {
                    AddressSpace address = new AddressSpace();
                    address.Title = basicNodesetInformation.Title;
                    address.Version = basicNodesetInformation.Version;
                    address.Contributor.Name = basicNodesetInformation.Organisation;
                    address.License = basicNodesetInformation.License;
                    address.Nodeset.NodesetPublication = basicNodesetInformation.CreationTime;
                    address.MetadataID = basicNodesetInformation.ID.ToString();
                    result.Add(address);
                }
            }
            return result;
        }

        private static string PrepareArgumentsString(IEnumerable<string> arguments)
        {
            List<string> argumentsList = new List<string>();

            foreach (string argument in arguments)
            {
                argumentsList.Add(string.Format("\"{0}\"", argument));
            }

            StringBuilder stringBuilder = new StringBuilder();

            for(int i = 0; i < argumentsList.Count; i++)
            {
                if(i != 0)
                {
                    stringBuilder.Append(",");
                }
                stringBuilder.Append(argumentsList[i]);
            }

            return stringBuilder.ToString();
        }
    }
}
