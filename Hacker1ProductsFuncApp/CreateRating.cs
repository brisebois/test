using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;

namespace Hacker1ProductsFuncApp
{
    public static class CreateRating
    {
        [FunctionName("CreateRating")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ratings")]HttpRequestMessage req,
            [DocumentDB(
                databaseName: "OpenHack",
                collectionName: "Ratings",
                ConnectionStringSetting = "CosmosDBConnection")]out dynamic document,
            TraceWriter log)
        {
            document = null;

            NewRating newRating;
            try
            {
                dynamic body = req.Content.ReadAsStringAsync().Result;

                newRating = JsonConvert.DeserializeObject<NewRating>(body as string);
            }
            catch
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "wrong payload");
            }

            try
            {
                HttpClient client = new HttpClient();
                var json = client.GetStringAsync($"https://hacker1.azurewebsites.net/api/users/{newRating.userId}").Result;
                var user = JsonConvert.DeserializeObject<User>(json);
            }
            catch
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "invalid userId");
            }

            try
            {
                HttpClient client = new HttpClient();
                var json = client.GetStringAsync($"https://hacker1.azurewebsites.net/api/products/{newRating.productId}").Result;
                var user = JsonConvert.DeserializeObject<Product>(json);
            }
            catch
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "invalid productId");
            }


            if (newRating.rating <= 5 && newRating.rating >= 0)
            {
                var rating = new Rating
                {
                    id = Guid.NewGuid().ToString(),
                    rating = newRating.rating,
                    locationName = newRating.locationName,
                    productId = newRating.productId,
                    timestamp = DateTime.UtcNow.ToString("u"),
                    userId = newRating.userId,
                    userNotes = newRating.userNotes
                };
                document = rating;
                return req.CreateResponse(HttpStatusCode.OK, rating);
            }

            return req.CreateErrorResponse(HttpStatusCode.BadRequest, "invalid rating");
        }
    }
}
