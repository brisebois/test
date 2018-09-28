using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Hacker1ProductsFuncApp
{
    public static class Report
    {
        [FunctionName("Report")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            var account = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StorageConnectionString"));
            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("reports");

            // https://hacker1storage.blob.core.windows.net/reports/2018/09/28/14/0_3e6eb190b2154a79bba1636d05ac8c89_1.json
            var blob = container.ListBlobs($"{DateTime.UtcNow.ToString("yyyy/MM/dd/HH/")}").ToList();
            if(!blob.Any())
                blob = container.ListBlobs($"{DateTime.UtcNow.Subtract(TimeSpan.FromHours(1)).ToString("yyyy/MM/dd/HH/")}").ToList();
            
            var list = new List<Record>();

            foreach (var item in blob.OfType<CloudBlockBlob>())
            {
                string s = await item.DownloadTextAsync();
                if (s.Length>0 && !s.EndsWith("]"))
                    s = s + "]";

                var r = JsonConvert.DeserializeObject<Record[]>(s);
                list.AddRange(r);
            }

            var latest = list.Where(rec =>
            {
                return rec.time == list.Max(r => r.time);
            }).ToList();

            // A list of ice creams based on total distributor orders total sales, from highest to lowest
            var orders = latest.Select(r => new
            {
               name = r.productname,
               total = r.totalpo
            }).OrderByDescending(r => r.total).ToList();
            // A list of ice creams based on total POS sales, from highest to lowest
            var sales = latest.Select(r => new
            {
                name = r.productname,
                total = r.saleeventtotalcost
            }).OrderByDescending(r => r.total).ToList();
            // The average sentiment analysis value for each ice cream
            var sentiment = latest.Select(r => new
            {
               name = r.productname,
                avgsentiment = r.avgsentiment
            }).OrderByDescending(r => r.avgsentiment).ToList();

            return req.CreateResponse(HttpStatusCode.OK, new
            {
                Orders = orders,
                Sales = sales,
                Sentiment = sentiment
            });
        }
    }

    public class Record
    {
        public string productname { get; set; }
        public float saleeventtotalcost { get; set; }
        public float totalpo { get; set; }
        public float avgrating { get; set; }
        public float avgsentiment { get; set; }
        public DateTime time { get; set; }
    }
}
