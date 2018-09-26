using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Rest;

namespace Hacker1ProductsFuncApp
{
    public static class CreateRating
    {
        static HttpClient client = new HttpClient();

        [FunctionName("CreateRating")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ratings")]HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient orchestrationClient,
            TraceWriter log)
        {
            NewRating newRating;
            try
            {
                dynamic body = await req.Content.ReadAsStringAsync();

                newRating = JsonConvert.DeserializeObject<NewRating>(body as string);
            }
            catch
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "wrong payload");
            }

            try
            {
                var json = await client.GetStringAsync($"https://hacker1.azurewebsites.net/api/users/{newRating.userId}");
                var user = JsonConvert.DeserializeObject<User>(json);
            }
            catch
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "invalid userId");
            }

            try
            {
                var json = await client.GetStringAsync($"https://hacker1.azurewebsites.net/api/products/{newRating.productId}");
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

                await orchestrationClient.StartNewAsync("PersistRating", rating.id, rating);

                return req.CreateResponse(HttpStatusCode.OK, rating);
            }

            return req.CreateErrorResponse(HttpStatusCode.BadRequest, "invalid rating");
        }
    }

    public static class PersistRating
    {
        private static string key = TelemetryConfiguration.Active.InstrumentationKey =
            System.Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY",
                EnvironmentVariableTarget.Process);

        private static TelemetryClient telemetryClient =
            new TelemetryClient {InstrumentationKey = key};

        [FunctionName("PersistRating")]
        public static async Task Run([OrchestrationTrigger] DurableOrchestrationContext context,
            [DocumentDB(
                databaseName: "OpenHack",
                collectionName: "Ratings",
                ConnectionStringSetting = "CosmosDBConnection")]
            IAsyncCollector<Rating> ratingsOut,
            TraceWriter log)
        {
            var rating  = context.GetInput<Rating>();

            ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
            {
                Endpoint = "https://southcentralus.api.cognitive.microsoft.com"
            };

            var result = await client.SentimentAsync(
                new MultiLanguageBatchInput(
                    new List<MultiLanguageInput>
                    {
                        new MultiLanguageInput("en", "0", rating.userNotes)
                    }));

            rating.sentimentScore = result.Documents.First().Score;

            telemetryClient.TrackEvent("Rating", new Dictionary<string, string>()
            {
                {"id", rating.id},
                {"productId", rating.productId},
                {"userNotes", rating.userNotes}
            }, new Dictionary<string, double>
            {
                {"sentimentScore", rating.sentimentScore ?? 0d}
            });

            await ratingsOut.AddAsync(rating);
        }
    }

    class ApiKeyServiceClientCredentials : ServiceClientCredentials
    {
        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("Ocp-Apim-Subscription-Key", "96ed654a877c4b0da2d5067507350904");
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}
