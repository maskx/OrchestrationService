using DurableTask.Core;
using maskx.OrchestrationService.SQL;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using DurableTask.Core.Serializing;

namespace maskx.OrchestrationService.Worker
{
    public class CommunicationWorker : BackgroundService
    {
        private readonly TaskHubClient taskHubClient;
        private readonly CommunicationWorkerOptions options;
        private string fetchCommandText;
        private readonly Dictionary<string, ICommunicationProcessor> processors;
        private DataConverter dataConverter = new JsonDataConverter();

        public CommunicationWorker(
            IServiceProvider serviceProvider,
            IOrchestrationServiceClient orchestrationServiceClient,
            IOptions<CommunicationWorkerOptions> options)
        {
            this.options = options?.Value;
            this.taskHubClient = new TaskHubClient(orchestrationServiceClient);
            this.processors = new Dictionary<string, ICommunicationProcessor>();
            var p = serviceProvider.GetServices<ICommunicationProcessor>();
            foreach (var item in p)
            {
                this.processors.Add(item.Name, item);
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await CreateIfNotExistsAsync(false);
            fetchCommandText = BuildFetchCommand();
            await base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var jobs = await FetchJob();
                Parallel.ForEach(jobs, async (job) =>
                {
                    // 1. communicate with other system, and get response
                    var response = await this.processors[job.Processor].ProcessAsync(job);
                    // 2. update communication table
                    await UpdateJob(job.RequestId, job.ResponseCode, job.ResponseContent);
                    // 3. send the result back to orchestration
                    await this.taskHubClient.RaiseEventAsync(
                        new OrchestrationInstance()
                        {
                            InstanceId = job.InstanceId,
                            ExecutionId = job.ExecutionId
                        },
                        job.EventName,
                      dataConverter.Serialize(new TaskResult() { Code = response.ResponseCode, Content = response.ResponseContent })
                        );
                });
                if (jobs.Count == 0)
                    await Task.Delay(1000);
            }
        }

        private async Task<List<CommunicationJob>> FetchJob()
        {
            List<CommunicationJob> jobs = new List<CommunicationJob>();
            using (var db = new DbAccess(this.options.ConnectionString))
            {
                db.AddStatement(fetchCommandText);
                await db.ExecuteReaderAsync((reader, index) =>
                {
                    var job = new CommunicationJob()
                    {
                        InstanceId = reader["InstanceId"].ToString(),
                        ExecutionId = reader["ExecutionId"].ToString(),
                        EventName = reader["EventName"].ToString(),
                        RequestId = reader["RequestId"].ToString(),
                        Processor = reader["Processor"].ToString(),
                        RequestTo = reader["RequestTo"]?.ToString(),
                        RequestOperation = reader["RequestOperation"]?.ToString(),
                        RequsetContent = reader["RequsetContent"]?.ToString(),
                        RequestProperty = reader["RequestProperty"]?.ToString()
                    };
                    job.RuleField = new Dictionary<string, object>();
                    foreach (var item in options.RuleFields)
                    {
                        job.RuleField.Add(item, reader[item]);
                    }
                    jobs.Add(job);
                });
            }
            return jobs;
        }

        private async Task UpdateJob(string RequestId, int ResponseCode, string ResponseContent)
        {
            using (var db = new DbAccess(this.options.ConnectionString))
            {
                db.AddStatement(string.Format(updatejobTemplate, options.CommunicationTableName), new
                {
                    ResponseCode,
                    RequestId,
                    ResponseContent
                });
                await db.ExecuteNonQueryAsync();
            }
        }

        private string BuildFetchCommand()
        {
            StringBuilder sb = new StringBuilder("declare @RequestId nvarchar(50);");
            if (this.options.GetFetchRules != null)
            {
                var fetchRules = this.options.GetFetchRules();
                if (fetchRules.Count > 0)
                    return sb.Append(FetchRule.BuildFetchCommand(fetchRules, options)).ToString();
            }
            return sb.Append(string.Format(fetchTemplate, options.MaxConcurrencyRequest, options.CommunicationTableName)).ToString();
        }

        public async Task DeleteCommunicationAsync()
        {
            using (var db = new DbAccess(options.ConnectionString))
            {
                db.AddStatement($"DROP TABLE IF EXISTS {options.CommunicationTableName}");
                await db.ExecuteNonQueryAsync();
            }
        }

        public async Task CreateIfNotExistsAsync(bool recreate)
        {
            if (recreate) await DeleteCommunicationAsync();
            using (var db = new DbAccess(options.ConnectionString))
            {
                db.AddStatement($@"IF(SCHEMA_ID(@schema) IS NULL)
                    BEGIN
                        EXEC sp_executesql N'CREATE SCHEMA [{options.SchemaName}]'
                    END", new { schema = options.SchemaName });

                db.AddStatement($@"
IF(OBJECT_ID(@table) IS NULL)
BEGIN
    CREATE TABLE {options.CommunicationTableName} (
        [InstanceId] [nvarchar](50) NOT NULL,
	    [ExecutionId] [nvarchar](50) NOT NULL,
	    [EventName] [nvarchar](50) NOT NULL,
	    [Processor] [nvarchar](50) NULL,
	    [RequestTo] [nvarchar](50) NULL,
	    [RequestOperation] [nvarchar](50) NULL,
	    [RequsetContent] [nvarchar](max) NULL,
	    [RequestProperty] [nvarchar](max) NULL,
	    [Status] [nvarchar](50) NULL,
	    [LockedUntilUtc] [datetime2](7) NULL,
	    [ResponseContent] [nvarchar](max) NULL,
	    [ResponseCode] [int] NULL,
	    [RequestId] [nvarchar](50) NULL,
	    [CompletedTime] [datetime2](7) NULL,
	    [CreateTime] [datetime2](7) NULL,
    CONSTRAINT [PK_{options.SchemaName}_{options.HubName}_{CommunicationWorkerOptions.CommunicationTable}] PRIMARY KEY CLUSTERED
    (
	    [InstanceId] ASC,
	    [ExecutionId] ASC,
	    [EventName] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END", new { table = options.CommunicationTableName });

                await db.ExecuteNonQueryAsync();
            }
        }

        // {0} fetch count
        // {1} Communication table name
        private const string fetchTemplate = @"
update top({0}) T
set @RequestId=T.RequestId=newid(),T.[Status]=N'Locked'
output INSERTED.*
FROM {1} AS T
where [status]=N'Pending'
";

        // {0} Communication table name
        private const string updatejobTemplate = @"
update {0}
set [Status]=N'Completed', [ResponseCode]=@ResponseCode,[ResponseContent]=@ResponseContent,CompletedTime=getutcdate()
where RequestId=@RequestId";
    }
}