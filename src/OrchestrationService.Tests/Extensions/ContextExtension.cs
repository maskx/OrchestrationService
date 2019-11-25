using DurableTask.Core;
using maskx.OrchestrationService;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Polly.Contrib.WaitAndRetry;

namespace OrchestrationService.Tests.Extensions
{
    public static class ContextExtension
    {
        public static IServiceProvider ServiceProvider { get; set; }
        public static IConfiguration Configuration { get; set; }

        public static IConfiguration GetConfiguration(this TaskContext cxt)
        {
            return Configuration;
        }

        public static IServiceProvider GetService(this TaskContext cxt)
        {
            return ServiceProvider;
        }

        public static string GetConnectionString(this TaskContext cxt)
        {
            return Configuration.GetConnectionString("dbConnection");
        }

        public static async Task<TaskResult> HttpGet(this TaskContext cxt, string requestUri)
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);

            var retryPolicy = Policy
                .Handle<TimeoutException>()
                .WaitAndRetryAsync(delay);
            var clientFactory = ServiceProvider.GetService<IHttpClientFactory>();
            var client = clientFactory.CreateClient();
            var response = await retryPolicy.ExecuteAndCaptureAsync<HttpResponseMessage>(async () =>
             {
                 return await client.GetAsync(requestUri);
             });
            string content = string.Empty;
            if (response.FaultType == null)
            {
                try
                {
                    content = await response.Result.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    content = ex.Message;
                }
                return new TaskResult() { Code = (int)response.Result.StatusCode, Content = content };
            }
            return new TaskResult() { Code = 400, Content = response.FinalException.Message };
        }

        public static IConfiguration GetConfiguration(this OrchestrationContext cxt)
        {
            return Configuration;
        }

        public static string GetConnectionString(this OrchestrationContext cxt)
        {
            return Configuration.GetConnectionString("dbConnection");
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            Random jitterer = new Random();
            var retryWithJitterPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                .WaitAndRetryAsync(6,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                  + TimeSpan.FromMilliseconds(jitterer.Next(0, 100))
                );
            return retryWithJitterPolicy;
        }
    }
}