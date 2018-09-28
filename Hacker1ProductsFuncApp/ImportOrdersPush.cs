// This is the default URL for triggering event grid function in the local environment.
// http://localhost:7071/admin/extensions/EventGridExtensionConfig?functionName={functionname} 

using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Hacker1ProductsFuncApp
{
    public static class StartImportMissedPurchaseOrders
    {
        [FunctionName("StartImportMissedPurchaseOrders")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "import")]
                                                           HttpRequestMessage req,
                                                           [OrchestrationClient] DurableOrchestrationClient client,
                                                           TraceWriter log)
        {
            await client.StartNewAsync("ImportMissedPurchaseOrders",null);
            return req.CreateResponse(HttpStatusCode.Accepted);
        }
    }

    public static class ImportMissedPurchaseOrders
    {
        [FunctionName("ImportMissedPurchaseOrders")]
        public static async Task Run([OrchestrationTrigger] DurableOrchestrationContext context,
                                     [OrchestrationClient] DurableOrchestrationClient client,
                                     TraceWriter log)
        {
            var account = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StorageConnectionString"));
            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("challengesixblob");

            var resultSegment = await container.ListBlobsSegmentedAsync(null);

            do
            {
                foreach (var r in resultSegment.Results)
                {
                    if (!(r is CloudBlockBlob blob))
                        continue;

                    blob.FetchAttributes();
                    if (blob.Metadata.ContainsKey("imported"))
                        continue;

                    var id = blob.Name.Substring(0, blob.Name.IndexOf("-", StringComparison.Ordinal));

                    var status = await client.GetStatusAsync(id);
                    if (status == null)
                        await client.StartNewAsync("NewOrder", id, null);

                    if (blob.Name.EndsWith("OrderHeaderDetails.csv"))
                    {
                        await client.RaiseEventAsync(id, "OrderHeaderDetails.csv", blob.Name);
                    }
                    else if (blob.Name.EndsWith("ProductInformation.csv"))
                    {
                        await client.RaiseEventAsync(id, "ProductInformation.csv", blob.Name);
                    }
                    else if (blob.Name.EndsWith("OrderLineItems.csv"))
                    {
                        await client.RaiseEventAsync(id, "OrderLineItems.csv", blob.Name);
                    }
                }

                resultSegment = await container.ListBlobsSegmentedAsync(resultSegment.ContinuationToken);
            } while (resultSegment.ContinuationToken != null);
        }
    }

    public static class OnBlobCreated
    {
        [FunctionName("OnBlobCreated")]
        public static async Task Run([EventGridTrigger]JObject eventGridEvent,
            [OrchestrationClient] DurableOrchestrationClient client,
            TraceWriter log)
        {
            log.Info(eventGridEvent.ToString(Formatting.Indented));

            var eventType = eventGridEvent.SelectToken(@"eventType").Value<string>();

            if (eventType != "Microsoft.Storage.BlobCreated") return;

            var blobUrl = eventGridEvent.SelectToken(@"data.url").Value<string>();

            var blob = new CloudBlockBlob(new Uri(blobUrl));

            var id = blob.Name.Substring(0, blob.Name.IndexOf("-", StringComparison.Ordinal));

            var status = await client.GetStatusAsync(id);
            if (status == null)
                await client.StartNewAsync("NewOrder", id, null);

            if (blob.Name.EndsWith("OrderHeaderDetails.csv"))
            {
                await client.RaiseEventAsync(id, "OrderHeaderDetails.csv", blob.Name);
            }
            else if (blob.Name.EndsWith("ProductInformation.csv"))
            {
                await client.RaiseEventAsync(id, "ProductInformation.csv", blob.Name);
            }
            else if (blob.Name.EndsWith("OrderLineItems.csv"))
            {
                await client.RaiseEventAsync(id, "OrderLineItems.csv", blob.Name);
            }
        }
    }

    public static class NewOrder
    {
        [FunctionName("NewOrder")]
        public static async Task Run([OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var header = context.WaitForExternalEvent<string>("OrderHeaderDetails.csv");
            var info = context.WaitForExternalEvent<string>("ProductInformation.csv");
            var lineItems = context.WaitForExternalEvent<string>("OrderLineItems.csv");

            // all three documents are present, build the order documents
            await Task.WhenAll(header, info, lineItems);

            var account = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StorageConnectionString"));
            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("challengesixblob");

            var headerBlob = container.GetBlockBlobReference(header.Result);
            var headerText = headerBlob.DownloadText();
            var csv = new CsvReader(new StringReader(headerText), new Configuration { HasHeaderRecord = true });

            var orderDictionary = csv.GetRecords<dynamic>()
                .Select(r => new Order
                {
                    Id = r.ponumber,
                    Datetime = Convert.ToDateTime(r.datetime),
                    TotalTax = Convert.ToDouble(r.totaltax),
                    TotalCost = Convert.ToDouble(r.totalcost),
                    Location = new Location
                    {
                        Id = r.locationid,
                        Name = r.locationname,
                        Address = r.locationaddress,
                        PostCode = r.locationpostcode
                    }
                }).ToDictionary(o => o.Id, o => o);

            var infoBlob = container.GetBlockBlobReference(info.Result);
            var infoText = infoBlob.DownloadText();
            csv = new CsvReader(new StringReader(infoText), new Configuration { HasHeaderRecord = true });
            var productDictionary = csv.GetRecords<dynamic>()
                .Select(r => new ProductDetail
                {
                    Id = r.productid,
                    Name = r.productname,
                    Description = r.productdescription
                }).ToDictionary(p => p.Id, p => p);


            var lineItemsBlob = container.GetBlockBlobReference(lineItems.Result);
            var itemsText = lineItemsBlob.DownloadText();
            csv = new CsvReader(new StringReader(itemsText), new Configuration { HasHeaderRecord = true });
            foreach (var i in csv.GetRecords<dynamic>())
            {
                var o = (Order)orderDictionary[Convert.ToString(i.ponumber)];

                var p = productDictionary[Convert.ToString(i.productid)];

                var li = new LineItem
                {
                    Product = p,
                    Quantity = Convert.ToInt32(i.quantity),
                    UnitCost = Convert.ToDouble(i.unitcost),
                    TotalCost = Convert.ToDouble(i.totalcost),
                    TotalTax = Convert.ToDouble(i.totaltax)
                };

                o.LineItems.Add(li);
            }

            foreach (var order in orderDictionary)
                await context.CallActivityWithRetryAsync("PersistOrder", new RetryOptions(TimeSpan.FromSeconds(2), 10), order.Value);

            headerBlob.Metadata.Add("imported", "true");
            await headerBlob.SetMetadataAsync();

            infoBlob.Metadata.Add("imported", "true");
            await infoBlob.SetMetadataAsync();

            lineItemsBlob.Metadata.Add("imported", "true");
            await lineItemsBlob.SetMetadataAsync();
        }

        public static class PersistOrder
        {
            [FunctionName("PersistOrder")]
            public static async Task Run(
                [ActivityTrigger] Order order,
                [DocumentDB(
                    databaseName: "OpenHack",
                    collectionName: "Orders",
                    ConnectionStringSetting = "CosmosDBConnection")]
                IAsyncCollector<Order> ordersOut,
                TraceWriter log)
            {
                await ordersOut.AddAsync(order);
            }
        }
    }
}
