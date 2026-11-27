using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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

        private readonly object _lock = new();
        private readonly List<string> _requests = new();
        private readonly List<string> _contentTypeRequests = new();

        /// <summary>The nodesets converted so far (a snapshot; recorded thread-safely).</summary>
        public IReadOnlyList<string> Requests
        {
            get { lock (_lock) { return _requests.ToArray(); } }
        }

        /// <summary>The nodesets inspected for their document kind so far (a snapshot).</summary>
        public IReadOnlyList<string> ContentTypeRequests
        {
            get { lock (_lock) { return _contentTypeRequests.ToArray(); } }
        }

        public string ContentType { get; set; } = WotMediaTypes.ThingModel;

        public string FailWith { get; set; }

        public bool Busy { get; set; }

        public Task<WotThingModelResult> GenerateAsync(string nodesetXml)
        {
            lock (_lock)
            {
                _requests.Add(nodesetXml);
            }

            if (Busy)
            {
                return Task.FromResult(WotThingModelResult.Busy());
            }

            return Task.FromResult(FailWith != null
                ? WotThingModelResult.Failure(FailWith)
                : WotThingModelResult.Success(Encoding.UTF8.GetBytes(Document), ContentType, null));
        }

        public string GetContentType(string nodesetXml)
        {
            lock (_lock)
            {
                _contentTypeRequests.Add(nodesetXml);
            }

            return ContentType;
        }
    }
}
