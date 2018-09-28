using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Hacker1ProductsFuncApp
{
    public static class PersistFromEventHubs
    {
        [FunctionName("PersistFromEventHubs")]
        public static async Task Run([EventHubTrigger("hacker1ehub", Connection = "EventHubsConnectionString",ConsumerGroup = "Listen")]string[] messages,
                                     [DocumentDB(
                                        databaseName: "OpenHack",
                                        collectionName: "SalesEvents",
                                        ConnectionStringSetting = "CosmosDBConnection")]
                                     IAsyncCollector<SalesEvent> salesEvents,
                                     TraceWriter log)
        {
            var events = messages.Select(JsonConvert.DeserializeObject<SalesEvent>).ToList();
            foreach (var e in events)
                await salesEvents.AddAsync(e);
        }
    }

    [DataContract()]
    public class SalesEvent
    {
        [DataMember()]
        public string id { get; set; } = Guid.NewGuid().ToString();
        [DataMember()]
        public Header header { get; set; }
        [DataMember()]
        public Detail[] details { get; set; }
    }

    [DataContract()]
    public class Header
    {
        [DataMember()]
        public string salesNumber { get; set; }
        [DataMember()]
        public DateTime dateTime { get; set; }
        [DataMember()]
        public string locationId { get; set; }
        [DataMember()]
        public string locationName { get; set; }
        [DataMember()]
        public string locationAddress { get; set; }
        [DataMember()]
        public string locationPostcode { get; set; }
        [DataMember()]
        public string totalCost { get; set; }
        [DataMember()]
        public string totalTax { get; set; }
    }

    [DataContract]
    public class Detail
    {
        [DataMember()]
        public string productId { get; set; }
        [DataMember()]
        public string quantity { get; set; }
        [DataMember()]
        public string unitCost { get; set; }
        [DataMember()]
        public string totalCost { get; set; }
        [DataMember()]
        public string totalTax { get; set; }
        [DataMember()]
        public string productName { get; set; }
        [DataMember()]
        public string productDescription { get; set; }
    }

}
