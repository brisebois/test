using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Refit;
using System;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

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
                var user = RestService.For<IService>("https://hacker1.azurewebsites.net/").GetUser(newRating.userId).Result;
            }
            catch
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "invalid userId");
            }

            try
            {
                var product = RestService.For<IService>("https://hacker1.azurewebsites.net/").GetProduct(newRating.productId).Result;
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
