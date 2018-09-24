using System;
using System.Runtime.Serialization;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Newtonsoft.Json;

namespace Hacker1ProductsFuncApp
{
    public static class PersistFromEventHubs
    {
        [FunctionName("PersistFromEventHubs")]
        public static void Run([EventHubTrigger("hacker1ehub", Connection = "EventHubsConnectionString")]string myEventHubMessage,
                               [DocumentDB(
                                    databaseName: "OpenHack",
                                    collectionName: "SalesEvents",
                                    ConnectionStringSetting = "CosmosDBConnection")]
                                out dynamic document,
                                TraceWriter log)
        {
            document = JsonConvert.DeserializeObject<SalesEvent>(myEventHubMessage);
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