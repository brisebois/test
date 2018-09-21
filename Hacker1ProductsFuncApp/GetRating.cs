using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Hacker1ProductsFuncApp
{
    public static class GetRating
    {
        [FunctionName("GetRating")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ratings/{ratingId}")]HttpRequestMessage req,
            [DocumentDB(
            databaseName: "OpenHack",
            collectionName: "Ratings",
            ConnectionStringSetting = "CosmosDBConnection",
            Id = "{ratingId}")]Rating rating,
            TraceWriter log)
        {
            if (rating == null)
                return req.CreateErrorResponse(HttpStatusCode.NotFound, "not found");

            return req.CreateResponse(HttpStatusCode.OK, rating);
        }
    }
}
