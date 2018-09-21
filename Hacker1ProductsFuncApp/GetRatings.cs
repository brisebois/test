using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace Hacker1ProductsFuncApp
{
    public static class GetRatings
    {
        [FunctionName("GetRatings")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/{userId}/ratings/")]HttpRequestMessage req,
            [DocumentDB(
                databaseName: "OpenHack",
                collectionName: "Ratings",
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = "select * from Items r where r.userId = {userId}")] IEnumerable<Rating> ratings, TraceWriter log)
        {
            var list = ratings.ToList();
            if (list.Any())
                return req.CreateResponse(HttpStatusCode.OK, list);

            return req.CreateErrorResponse(HttpStatusCode.NotFound, "not found");
        }
    }
}
