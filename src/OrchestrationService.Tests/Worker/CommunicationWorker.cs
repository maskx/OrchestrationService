using DurableTask.Core;
using maskx.OrchestrationService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.Worker
{
    public class CommunicationWorker : BackgroundService
    {
        private readonly TaskHubClient taskHubClient;
        private readonly CommunicationWorkerOptions options;
        private string fetchCommandText;
        private IHttpClientFactory httpClientFactory;

        public CommunicationWorker(
            IOrchestrationServiceClient orchestrationServiceClient,
            IOptions<CommunicationWorkerOptions> options,
            IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
            this.options = options?.Value;
            this.taskHubClient = new TaskHubClient(orchestrationServiceClient);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            fetchCommandText = BuildFetchCommadn();
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var job = await FetchJob();
                if (job != null)
                {
                    // 1. communicate with other system, and get response
                    await UpdateJob(job.RequestId, 200, "{ Code:200,Content:\"done\"}");
                    // 2. send the result back to orchestration
                    await this.taskHubClient.RaiseEventAsync(
                        new OrchestrationInstance()
                        {
                            InstanceId = job.InstanceId,
                            ExecutionId = job.ExecutionId
                        },
                        job.EventName,
                        "{ Code:200,Content:\"done\"}"
                        );
                }
                await Task.Delay(1000);
            }
        }

        private async Task<CommunicationJob> FetchJob()
        {
            CommunicationJob job = null;
            using (var conn = new SqlConnection(this.options.ConnectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = fetchCommandText;
                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    job = new CommunicationJob()
                    {
                        InstanceId = reader["InstanceId"].ToString(),
                        ExecutionId = reader["ExecutionId"].ToString(),
                        EventName = reader["EventName"].ToString(),
                        RequestId = reader["RequestId"].ToString()
                    };
                }
            }
            return job;
        }

        private async Task UpdateJob(string requestId, int code, string content)
        {
            using (var conn = new SqlConnection(this.options.ConnectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
update Communication
set [Status]=N'Completed', [ResponseCode]=@ResponseCode,[ResponseContent]=@ResponseContent
where RequestId=@RequestId";
                cmd.Parameters.AddWithValue("ResponseCode", code);
                cmd.Parameters.AddWithValue("RequestId", requestId);
                cmd.Parameters.AddWithValue("ResponseContent", content);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private List<FetchRule> MockFetchRule()
        {
            var r1 = new FetchRule()
            {
                What = new Dictionary<string, string>() { { "ServiceType", "VirtualMachine" } },
                Limitions = new List<Limitation>()
            };
            r1.Limitions.Add(new Limitation()
            {
                Concurrency = 1,
                Scope = new List<string>()
               {
                   "SubscriptionId"
               }
            });
            r1.Limitions.Add(new Limitation
            {
                Concurrency = 5,
                Scope = new List<string>()
               {
                   "ManagementUnit"
               }
            });
            var fetchRules = new List<FetchRule>();
            fetchRules.Add(r1);
            return fetchRules;
        }

        private string BuildFetchCommadn()
        {
            var fetchRules = MockFetchRule();
            if (fetchRules.Count > 0)
                return FetchRule.BuildFetchCommand(fetchRules, options.Concurrency);
            else
                return string.Format(fetchTemplate, options.Concurrency);
        }

        // {0} top
        private const string fetchTemplate = @"
update top({0}) T
set @RequestId=T.RequestId=newid(),T.[Status]=N'Locked'
output INSERTED.InstanceId,INSERTED.ExecutionId,INSERTED.EventName,INSERTED.RequestId
FROM Communication AS T
where [status]=N'Pending'
";
    }
}