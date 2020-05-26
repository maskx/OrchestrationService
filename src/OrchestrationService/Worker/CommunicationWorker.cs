using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.OrchestrationService.SQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Worker
{
    public class CommunicationWorker : BackgroundService
    {
        private readonly TaskHubClient taskHubClient;
        private readonly CommunicationWorkerOptions options;
        private string fetchCommandText;
        private readonly Dictionary<string, ICommunicationProcessor> processors;
        private DataConverter dataConverter = new JsonDataConverter();

        private int RunningTaskCount = 0;
        private readonly IServiceProvider serviceProvider;

        public CommunicationWorker(
            IServiceProvider serviceProvider,
            IOrchestrationServiceClient orchestrationServiceClient,
            IOptions<CommunicationWorkerOptions> options)
        {
            this.serviceProvider = serviceProvider;
            this.options = options?.Value;
            this.taskHubClient = new TaskHubClient(orchestrationServiceClient);
            this.processors = new Dictionary<string, ICommunicationProcessor>();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var p = this.serviceProvider.GetServices<ICommunicationProcessor>();
            foreach (var item in p)
            {
                item.CommunicationWorker = this;
                this.processors.Add(item.Name, item);
            }
            if (this.options.AutoCreate)
                await CreateIfNotExistsAsync(false);
            fetchCommandText = BuildFetchCommand();
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var jobs = await FetchJob();
                    Dictionary<string, List<List<CommunicationJob>>> batchJobs = new Dictionary<string, List<List<CommunicationJob>>>();
                    foreach (var job in jobs)
                    {
                        var processor = this.processors[job.Processor];
                        if (processor.MaxBatchCount == 1)
                        {
                            Interlocked.Increment(ref RunningTaskCount);
                            var _ = ProcessJobs(processor, job)
                                 .ContinueWith((t) =>
                                 {
                                     Interlocked.Decrement(ref RunningTaskCount);
                                 });
                        }
                        else
                        {
                            if (!batchJobs.TryGetValue(processor.Name, out List<List<CommunicationJob>> procJobs))
                            {
                                procJobs = new List<List<CommunicationJob>>();
                                batchJobs[processor.Name] = procJobs;
                            }
                            List<CommunicationJob> jobList = null;
                            foreach (var communicationJobs in procJobs)
                            {
                                if (communicationJobs.Count < processor.MaxBatchCount)
                                {
                                    jobList = communicationJobs;
                                }
                            }
                            if (jobList == null)
                            {
                                jobList = new List<CommunicationJob>();
                                procJobs.Add(jobList);
                            }
                            jobList.Add(job);
                        }
                    }
                    foreach (var batchJob in batchJobs)
                    {
                        foreach (var item in batchJob.Value)
                        {
                            //为RunningTaskCount增减每个CommunicationProcessor里JobCount
                            Interlocked.Add(ref RunningTaskCount, item.Count);
                            var _ = ProcessJobs(this.processors[batchJob.Key], item.ToArray())
                                .ContinueWith((t) =>
                                {
                                    Interlocked.Add(ref RunningTaskCount, 0 - item.Count);
                                });
                        }
                    }
                    if (jobs.Count == 0)
                        await Task.Delay(this.options.IdelMilliseconds);
                }
                catch (Exception e)
                {
                    CommunicationEventSource.Log.TraceEvent(System.Diagnostics.TraceEventType.Critical, "CommunicationWorker", e.Message, e.ToString(), "Error");
                }
            }
        }

        private async Task<List<CommunicationJob>> FetchJob()
        {
            List<CommunicationJob> jobs = new List<CommunicationJob>();
            if (options.MaxConcurrencyRequest - RunningTaskCount < 1)
            {
                return jobs;
            }
            using (var db = new DbAccess(this.options.ConnectionString))
            {
                db.AddStatement(fetchCommandText, new
                {
                    MaxCount = options.MaxConcurrencyRequest - RunningTaskCount
                });
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
                        CreateTime = DateTime.Parse(reader["CreateTime"].ToString()),
                        LockedUntilUtc = DateTime.Parse(reader["LockedUntilUtc"].ToString()),
                        RequestOperation = reader["RequestOperation"]?.ToString(),
                        RequestContent = reader["RequestContent"]?.ToString(),
                        RequestProperty = reader["RequestProperty"]?.ToString(),
                        Status = (CommunicationJob.JobStatus)(int)reader["Status"],
                        ResponseContent = reader["ResponseContent"]?.ToString(),
                    };
                    if (reader["Context"] != DBNull.Value)
                    {
                        job.Context = reader["Context"].ToString();
                    }
                    if (reader["ResponseCode"] != DBNull.Value)
                        job.ResponseCode = (int)reader["ResponseCode"];
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

        private async Task ProcessJobs(ICommunicationProcessor processor, params CommunicationJob[] jobs)
        {
            //使用Task.Run包装CommunicationProcessor里的代码执行，避免CommunicationProcessor里有类似于Thread.Sleep这样阻塞线程的情况
            //CommunicationProcessor里不要使用Task.WaitAll,建议使用Task.WhenAll
            var response = await Task.Run(async () => await processor.ProcessAsync(jobs));
            await Task.WhenAll(UpdateJobs(response), RaiseEvent(response));
        }

        public async Task UpdateJobs(params CommunicationJob[] jobs)
        {
            using (var db = new DbAccess(this.options.ConnectionString))
            {
                foreach (var job in jobs)
                {
                    db.AddStatement(string.Format(updatejobTemplate, options.CommunicationTableName), new
                    {
                        Status = (int)job.Status,
                        job.LockedUntilUtc,
                        job.Context,
                        job.ResponseCode,
                        job.RequestId,
                        job.ResponseContent,
                        CompletedTime = CommunicationJob.JobStatus.Completed == job.Status ? DateTime.UtcNow : default(DateTime)
                    });
                }

                await db.ExecuteNonQueryAsync();
            }
        }

        private async Task RaiseEvent(params CommunicationJob[] jobs)
        {
            List<Task> tasks = new List<Task>();
            foreach (var job in jobs)
            {
                if (job.Status == CommunicationJob.JobStatus.Completed)
                {
                    tasks.Add(this.taskHubClient.RaiseEventAsync(
                                        new OrchestrationInstance()
                                        {
                                            InstanceId = job.InstanceId,
                                            ExecutionId = job.ExecutionId
                                        },
                                        job.EventName,
                                        dataConverter.Serialize(new TaskResult() { Code = job.ResponseCode, Content = dataConverter.Serialize(new CommunicationResult { ResponseContent = job.ResponseContent, Context = job.Context }) })
                                        ));
                }
            }
            await Task.WhenAll(tasks);
        }

        private string BuildFetchCommand()
        {
            StringBuilder sb = new StringBuilder("declare @RequestId nvarchar(50);");
            if (this.options.GetFetchRules != null)
            {
                var fetchRules = this.options.GetFetchRules(serviceProvider);
                if (fetchRules.Count > 0)
                    return sb.Append(FetchRule.BuildFetchCommand(fetchRules, options)).ToString();
            }
            return sb.Append(string.Format(fetchTemplate,
                options.MaxConcurrencyRequest,
                options.CommunicationTableName,
                (int)CommunicationJob.JobStatus.Completed,
                (int)CommunicationJob.JobStatus.Locked,
                options.IdelMilliseconds)).ToString();
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
	    [RequestContent] [nvarchar](max) NULL,
	    [RequestProperty] [nvarchar](max) NULL,
	    [Status] [int] NULL,
	    [LockedUntilUtc] [datetime2](7) NULL,
	    [ResponseContent] [nvarchar](max) NULL,
	    [ResponseCode] [int] NULL,
        [Context] [nvarchar](max) NULL,
	    [RequestId] [nvarchar](50) NULL,
	    [CompletedTime] [datetime2](7) NULL,
	    [CreateTime] [datetime2](7) NULL,
    CONSTRAINT [PK_{options.SchemaName}_{options.HubName}{CommunicationWorkerOptions.CommunicationTable}] PRIMARY KEY CLUSTERED
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
        // {2} Completed status code
        // {3} Locked status code
        // {4} IdelMilliseconds
        private const string fetchTemplate = @"
update top({0}) T
set T.[Status]={3} ,T.[LockedUntilUtc]=DATEADD(millisecond,{4},getutcdate())
output INSERTED.*
FROM {1} AS T
where [status]<{2} and [LockedUntilUtc]<=getutcdate();
";

        // {0} Communication table name
        private const string updatejobTemplate = @"
update {0}
set [Status]=@Status,[LockedUntilUtc]=@LockedUntilUtc,[Context]=@Context, [ResponseCode]=@ResponseCode,[ResponseContent]=@ResponseContent,CompletedTime=@CompletedTime
where RequestId=@RequestId;";
    }
}