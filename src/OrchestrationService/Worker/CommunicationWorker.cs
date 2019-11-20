using DurableTask.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Worker
{
    public class CommunicationWorker : BackgroundService
    {
        private readonly TaskHubClient taskHubClient;
        private readonly CommunicationWorkerOptions options;

        public CommunicationWorker(
            IOrchestrationServiceClient orchestrationServiceClient,
            IOptions<CommunicationWorkerOptions> options)
        {
            this.options = options?.Value;
            this.taskHubClient = new TaskHubClient(orchestrationServiceClient);
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
                        "done"
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
update top(1) communication
set [Status]=N'Locked'
output INSERTED.InstanceId,INSERTED.ExecutionId,INSERTED.EventName,INSERTED.RequestUri
where [status]=N'Pending'";

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