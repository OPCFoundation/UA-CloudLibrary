using System.Collections.Generic;
using System.Text;
using Opc.Ua.Cloud.Library.Wot;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// <see cref="IWotThingModelGenerator"/> that records the nodesets it is asked to convert and returns
    /// a small fixed Thing Model, or a configured failure.
    /// </summary>
    internal sealed class FakeWotThingModelGenerator : IWotThingModelGenerator
    {
        public const string Document = "{\"@context\":[\"https://www.w3.org/2022/wot/td/v1.1\"],\"@type\":\"tm:ThingModel\",\"title\":\"Fake\"}";

        public List<string> Requests { get; } = new();

        public List<string> ContentTypeRequests { get; } = new();

        public string ContentType { get; set; } = WotMediaTypes.ThingModel;

        public string FailWith { get; set; }

        public WotThingModelResult Generate(string nodesetXml)
        {
            Requests.Add(nodesetXml);
            return FailWith != null
                ? WotThingModelResult.Failure(FailWith)
                : WotThingModelResult.Success(Encoding.UTF8.GetBytes(Document), ContentType, null);
        }

        public string GetContentType(string nodesetXml)
        {
            ContentTypeRequests.Add(nodesetXml);
            return ContentType;
        }
    }
}
