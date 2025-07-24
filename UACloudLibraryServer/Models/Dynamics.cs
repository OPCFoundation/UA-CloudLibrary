using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AdminShell
{
    public class DynamicsBearerTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonProperty("expires_in")]
        public int ExpiresInSeconds { get; set; } = 0;
    }

    public class DynamicsQuery
    {
        public string tracingDirection { get; set; }

        public string trackingId { get; set; }

        public string company { get; set; }

        public string itemNumber { get; set; }

        public string serialNumber { get; set; }

        public string batchNumber { get; set; }

        public bool shouldIncludeEvents { get; set; }
    }


    public class DynamicsQueryResponse
    {
        public string tracingDirection { get; set; }

        public ErpNode root { get; set; }
    }

    public class ErpNode
    {
        public string trackingId { get; set; }

        public List<ErpNode> next { get; set; }

        public List<ErpEvent> events { get; set; }
    }

    public class ErpEvent
    {
        public string eventId { get; set; }

        public string companyCode { get; set; }

        public string @operator { get; set; }

        public string description { get; set; }

        public string activityType { get; set; }

        public string activityCode { get; set; }

        public string datetime { get; set; }

        public List<ErpTransaction> consumptionTransactions { get; set; }

        public List<ErpTransaction> productTransactions { get; set; }
    }

    public class ErpTransaction
    {
        public string transactionId { get; set; }

        public string itemId { get; set; }

        public string trackingId { get; set; }

        public string eventId { get; set; }

        public float quantity { get; set; }

        public string unitOfMeasure { get; set; }

        public string transactionType { get; set; }

        public string batchId { get; set; }

        public string serialId { get; set; }

        public JObject details { get; set; }
    }
}
