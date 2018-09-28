using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;

namespace Hacker1ProductsFuncApp
{
    public static class OrderChangeFeed
    {
        [FunctionName("OrderChangeFeed")]
        public static void Run([CosmosDBTrigger(
                databaseName: "OpenHack",
                collectionName: "Orders",
                ConnectionStringSetting = "CosmosDBConnection",
                LeaseCollectionName = "leases")]
            IReadOnlyList<Document> documents,
            [EventHub("ChangeFeed", Connection = "ChangeFeedEventHubConnection")]
            ICollector<string> events,
            TraceWriter log)
        {
            if (documents == null) return;
            foreach (var order in documents)
            {
                foreach (var i in order.GetPropertyValue<List<LineItem>>("lineItems"))
                    events.Add(JsonConvert.SerializeObject(new
                    {
                        type = "order",
                        totalCost = i.TotalCost,
                        Id = i.Product.Id
                    }));
            }
        }
    }
    public static class RatingChangeFeed
    {
        [FunctionName("RatingChangeFeed")]
        public static void Run([CosmosDBTrigger(
            databaseName: "OpenHack",
            collectionName: "Ratings",
            ConnectionStringSetting = "CosmosDBConnection",
            LeaseCollectionName = "leases")]IReadOnlyList<Document> documents,
            [EventHub("ChangeFeed", Connection = "ChangeFeedEventHubConnection")] ICollector<string> events,
            TraceWriter log)
        {
            if (documents == null) return;
            foreach (var r in documents)
            {
                events.Add(JsonConvert.SerializeObject(new
                {
                    type = "rating",
                    sentiment = r.GetPropertyValue<double>("sentimentScore"),
                    rating = r.GetPropertyValue<int>("rating"),
                    productId = r.GetPropertyValue<string>("productId")
                }));
            }
        }
    }
}
