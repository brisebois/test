using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Hacker1ProductsFuncApp
{
    public static class ImportOrders
    {
        [FunctionName("ImportOrders")]
        public static async Task Run(
            [TimerTrigger("0 */1 * * * *")]TimerInfo timer,
            [DocumentDB(
                databaseName: "OpenHack",
                collectionName: "Orders",
                ConnectionStringSetting = "CosmosDBConnection")]
            IAsyncCollector<Order> ordersOut,
            TraceWriter log)
        {
            var account = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StorageConnectionString"));
            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("challengesixblob");

            var orders = container.ListBlobs()
                .OfType<CloudBlockBlob>()
                .GroupBy(blob => blob.Name.Substring(0, blob.Name.IndexOf("-", StringComparison.Ordinal)))
                .Where(order => order.Count() == 3)
                .ToList();

            foreach (var order in orders)
            {
                var header = order.First(b => b.Name.EndsWith("OrderHeaderDetails.csv")).DownloadText();
                var csv = new CsvReader(new StringReader(header), new Configuration { HasHeaderRecord = true });
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

                var info = order.First(b => b.Name.EndsWith("ProductInformation.csv")).DownloadText();
                csv = new CsvReader(new StringReader(info), new Configuration { HasHeaderRecord = true });
                var productDictionary = csv.GetRecords<dynamic>()
                    .Select(r => new ProductDetail
                    {
                        Id = r.productid,
                        Name = r.productname,
                        Description = r.productdescription
                    }).ToDictionary(p => p.Id, p => p);


                var items = order.First(b => b.Name.EndsWith("OrderLineItems.csv")).DownloadText();
                csv = new CsvReader(new StringReader(items), new Configuration { HasHeaderRecord = true });
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

                foreach (var o in orderDictionary.Values)
                    await ordersOut.AddAsync(o);
            }

            await ordersOut.FlushAsync();

            orders.ForEach(async o =>
            {
                foreach (var blob in o)
                {
                    await blob.DeleteAsync();
                }
            });
        }
    }

    // Line Item ponumber,productid,quantity,unitcost,totalcost,totaltax

    [DataContract]
    public class LineItem
    {
        [DataMember(Name = "product")]
        public ProductDetail Product { get; set; }
        [DataMember(Name = "quantity")]
        public int Quantity { get; set; }
        [DataMember(Name = "unitCost")]
        public double UnitCost { get; set; }
        [DataMember(Name = "totalCost")]
        public double TotalCost { get; set; }
        [DataMember(Name = "totalTax")]
        public double TotalTax { get; set; }
    }

    // INFO productid,productname,productdescription

    [DataContract]
    public class ProductDetail
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
    }

    //HEADER ponumber,datetime,locationid,locationname,locationaddress,locationpostcode,totalcost,totaltax

    [DataContract]
    public class Order
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "datetime")]
        public DateTime Datetime { get; set; }
        [DataMember(Name = "location")]
        public Location Location { get; set; } = new Location();
        [DataMember(Name = "totalTax")]
        public double TotalTax { get; set; }
        [DataMember(Name = "totalCost")]
        public double TotalCost { get; set; }
        [DataMember(Name = "lineItems")]
        public List<LineItem> LineItems { get; set; } = new List<LineItem>();
    }

    [DataContract]
    public class Location
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "address")]
        public string Address { get; set; }
        [DataMember(Name = "postcode")]
        public string PostCode { get; set; }
    }
}
