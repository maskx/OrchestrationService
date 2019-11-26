using DurableTask.Core;
using Polly;
using Polly.Contrib.WaitAndRetry;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Activity
{
    public class HttpRequestActivity : TaskActivity<HttpRequest, TaskResult>
    {
        private IHttpClientFactory httpClientFactory;

        public HttpRequestActivity(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, HttpRequest input)
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);

            var retryPolicy = Policy
                .Handle<TimeoutException>()
                .WaitAndRetryAsync(delay);
            var client = httpClientFactory.CreateClient();
            var response = await retryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage();
                return await client.SendAsync(requestMessage);
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

        protected override TaskResult Execute(TaskContext context, HttpRequest request)
        {
            return ExecuteAsync(context, request).Result;
        }
    }
}