using DurableTask.Core;
using Polly;
using Polly.Contrib.WaitAndRetry;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Activity
{
    public class HttpRequestActivity : AsyncTaskActivity<HttpRequestInput, TaskResult>
    {
        private IHttpClientFactory httpClientFactory;

        public HttpRequestActivity(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, HttpRequestInput input)
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);

            var retryPolicy = Policy
                .Handle<TimeoutException>()
                .WaitAndRetryAsync(delay);
            var client = httpClientFactory.CreateClient();
            var request = new HttpRequestMessage()
            {
                Method = input.Method,
                RequestUri = new Uri(input.Uri)
            };
            if (!string.IsNullOrEmpty(input.Content))
            {
                request.Content = new StringContent(input.Content, input.Encoding, input.MediaType);
            }

            foreach (var item in input.Headers)
            {
                request.Headers.Add(item.Key, item.Value);
            }
            var response = await retryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                return await client.SendAsync(request);
            });
            object content = string.Empty;
            if (response.FaultType == null)
            {
                try
                {
                    content = await response.Result.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    content = ex;
                }
                return new TaskResult((int)response.Result.StatusCode, content);
            }
            return new TaskResult(400, response.FinalException);
        }
    }
}