using DurableTask.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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

        public CommunicationWorker(
            IOrchestrationServiceClient orchestrationServiceClient,
            IOptions<CommunicationWorkerOptions> options)
        {
            this.options = options?.Value;
            this.taskHubClient = new TaskHubClient(orchestrationServiceClient);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            fetchCommandText = BuildFetchCommadn();
            return base.StartAsync(cancellationToken);
        }

        private string BuildFetchCommadn()
        {
            var r1 = new FetchRule()
            {
                ConcurrencyCount = 1
            };
            r1.What.Add("ServiceType", "VirtualMachine");
            r1.Scope.Add("SubscriptionId");
            var fetchRules = new List<FetchRule>() {
                r1
            };
            StringBuilder sb = new StringBuilder("declare @RequestId nvarchar(50);");
            List<string> others = new List<string>();
            foreach (var rule in fetchRules)
            {
                sb.Append(string.Format(ruleTemplate, rule.Where, rule.Group, rule.ConcurrencyCount, rule.On));
                others.Add($"({rule.Where})");
            }
            sb.Append(string.Format(otherTemplat, string.Join(" and ", others)));
            return sb.ToString();
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

        //{0} where
        //{1} group
        //{2} fetch limit count
        //{3} on
        private const string ruleTemplate = @"
update top(1) T
set @RequestId=T.RequestId=newid(),T.[Status]=N'Locked'
output INSERTED.InstanceId,INSERTED.ExecutionId,INSERTED.EventName,INSERTED.RequestId
FROM Communication AS T
    inner join (
        select
            COUNT(case when [status]='Locked' then 1 else null end) as Locked,
	        {1}
        from Communication
        where    {0}
        group by {1}
    ) as T1
    on {3}
where
    [status]=N'Pending'
    and T1.Locked<{2}

if @RequestId is not null
begin
    return
end
";

        private const string otherTemplat = @"
update top(1) T
set @RequestId=T.RequestId=newid(),T.[Status]=N'Locked'
output INSERTED.InstanceId,INSERTED.ExecutionId,INSERTED.EventName,INSERTED.RequestId
FROM Communication AS T
where [status]=N'Pending' and not ({0})
";
    }
}