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
            return base.StartAsync(cancellationToken);
        }

        private string BuildFetchCommadn()
        {
            var fetchRules = new List<FetchRule>() {
                new FetchRule(){
                    ConcurrencyCount=1,
                    ServiceType="VirtualMachine",
                    InOneSubscription=true
                }
            };
            string ruleTemplate = @"
update top(1) T
set @RequestId=T.RequestId=newid(),T.[Status]=N'Locked'
output INSERTED.InstanceId,INSERTED.ExecutionId,INSERTED.EventName,INSERTED.RequestUri
FROM Communication AS T
    inner join (
        select
	        max(InstanceId) as InstanceId,
	        max(ExecutionId) as ExcutionId,
	        max(EventName) as EventName,
	        SubscriptionId,
	        COUNT(0) as Number
        from Communication
        where
            [status]=N'Locked'
            and ServiceType={0}
            and RequestMethod={0}
        group by SubscriptionId
    ) as T1
    on T1.InstanceId=T.InstanceId and T1.ExecutionId=T.ExecutionId and T1.EventName=T.EventName
where
    [status]=N'Pending'
    and T1.Number<{0}

if @RequestId is not null
    begin
        return
    end
";
            StringBuilder sb = new StringBuilder("declare @RequestId nvarchar(50);");
            foreach (var rule in fetchRules)
            {
            }
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
                cmd.CommandText = @"
declare @RequestId nvarchar(50)

update top(1) communication
set @RequestId=RequestId=newid(),[Status]=N'Locked'
output INSERTED.InstanceId,INSERTED.ExecutionId,INSERTED.EventName,INSERTED.RequestUri
where [status]=N'Pending'
if @RequestId is not null
begin
    return
end
";

                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    job = new CommunicationJob()
                    {
                        InstanceId = reader["InstanceId"].ToString(),
                        ExecutionId = reader["ExecutionId"].ToString(),
                        EventName = reader["EventName"].ToString(),
                    };
                }
            }
            return job;
        }
    }
}