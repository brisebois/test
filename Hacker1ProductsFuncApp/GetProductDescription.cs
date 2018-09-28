using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace Hacker1ProductsFuncApp
{
    public static class GetProductDescription
    {
        [FunctionName("GetProductDescription")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequestMessage req, 
            TraceWriter log)
        {
            // parse query parameter
            var productId = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "productId", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;

            return productId == null ? req.CreateErrorResponse(HttpStatusCode.BadRequest,"productId has no value") 
                                     : req.CreateResponse(HttpStatusCode.OK, $"The product name for your product id {productId} is Starfruit Explosion");
        }
    }
}
