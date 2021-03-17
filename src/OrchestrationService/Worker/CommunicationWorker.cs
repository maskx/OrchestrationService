using DurableTask.Core;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Extensions;
using maskx.OrchestrationService.Orchestration;
using maskx.OrchestrationService.SQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Worker
{
    public class CommunicationWorker<T> : BackgroundService where T : CommunicationJob, new()
    {
        private readonly TaskHubClient taskHubClient;
        private readonly CommunicationWorkerOptions options;
        private readonly Dictionary<string, ICommunicationProcessor<T>> processors;
        // todo: communication table add agentId column
        string _AgentId;
        private string AgentId
        {
            get
            {
                if (string.IsNullOrEmpty(_AgentId))
                {
                    var name = Dns.GetHostName();
                    var ip = Dns.GetHostEntry(name).AddressList.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    _AgentId = $"{Environment.MachineName}_{ip}";
                }

                return _AgentId;
            }
        }

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
            this.processors = new Dictionary<string, ICommunicationProcessor<T>>();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.options == null)
            {
                TraceActivityEventSource.Log.Critical(nameof(CommunicationWorker<T>), string.Empty, string.Empty, "CommunicationWorker can not start", "options is null", "Failed");
                return;
            }
            var orchestrationWorker = this.serviceProvider.GetService<OrchestrationWorker>();
            orchestrationWorker.AddActivity(typeof(AsyncRequestActivity<T>));
            orchestrationWorker.AddOrchestration(typeof(AsyncRequestOrchestration<T>));
            var p = this.serviceProvider.GetServices<ICommunicationProcessor<T>>();
            foreach (var item in p)
            {
                this.processors.Add(item.Name, item);
            }
            if (this.options.AutoCreate)
                await CreateIfNotExistsAsync(false);
            await base.StartAsync(cancellationToken);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            TraceActivityEventSource.Log.Critical(nameof(CommunicationWorker<T>), string.Empty, string.Empty, "CommunicationWorker is stoped", "CommunicationWorker is stoped", "Failed");

            return base.StopAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var jobs = await FetchJob();
                    Dictionary<string, List<List<T>>> batchJobs = new Dictionary<string, List<List<T>>>();
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
                            if (!batchJobs.TryGetValue(processor.Name, out List<List<T>> procJobs))
                            {
                                procJobs = new List<List<T>>();
                                batchJobs[processor.Name] = procJobs;
                            }
                            List<T> jobList = null;
                            foreach (var communicationJobs in procJobs)
                            {
                                if (communicationJobs.Count < processor.MaxBatchCount)
                                {
                                    jobList = communicationJobs;
                                }
                            }
                            if (jobList == null)
                            {
                                jobList = new List<T>();
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

        private async Task<List<T>> FetchJob()
        {
            List<T> jobs = new List<T>();
            if (options.MaxConcurrencyRequest - RunningTaskCount < 1)
            {
                return jobs;
            }
            try
            {
                using (var db = new SQLServerAccess(this.options.ConnectionString))
                {
                    await db.ExecuteStoredProcedureASync(this.options.FetchCommunicationJobSPName, (reader, index) =>
                    {
                        jobs.Add(reader.CreateObject<T>());
                    }, new
                    {
                        LockedBy = AgentId,
                        this.options.MessageLockedSeconds,
                        MaxCount = options.MaxConcurrencyRequest - RunningTaskCount
                    });
                }
            }
            catch (Exception ex)
            {
                CommunicationEventSource.Log.Critical("FetchJob", ex.Message, ex.StackTrace, "Error");
            }

            return jobs;
        }

        private async Task ProcessJobs(ICommunicationProcessor<T> processor, params T[] jobs)
        {
            //使用Task.Run包装CommunicationProcessor里的代码执行，避免CommunicationProcessor里有类似于Thread.Sleep这样阻塞线程的情况
            //CommunicationProcessor里不要使用Task.WaitAll,建议使用Task.WhenAll
            T[] response = null;
            try
            {
                response = await Task.Run(async () => await processor.ProcessAsync(jobs));
            }
            catch (Exception ex)
            {
                CommunicationEventSource.Log.Critical("ProcessJobs", ex.Message, ex.StackTrace, "Error");
            }
            if (response != null)
            {
                foreach (var item in response)
                {
                    if (await RaiseEvent(item)) await UpdateJobs(item);
                }
            }

        }

        public async Task UpdateJobs(params T[] jobs)
        {
            using var db = new SQLServerAccess(this.options.ConnectionString);
            foreach (var job in jobs)
            {
                try
                {
                    await db.ExecuteStoredProcedureASync(options.UpdateCommunicationSPName, new
                    {
                        Status = (int)job.Status,
                        job.Context,
                        job.ResponseCode,
                        job.RequestId,
                        job.ResponseContent,
                        MessageLockedSeconds = job.NextTryAfterSecond ?? options.MessageLockedSeconds
                    });
                }
                catch (Exception ex)
                {
                    CommunicationEventSource.Log.Critical("UpdateJobs", ex.Message, ex.StackTrace, "Error");
                }
            }
        }

        private async Task<bool> RaiseEvent(params T[] jobs)
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
                                        new TaskResult(job.ResponseCode, new CommunicationResult { ResponseContent = job.ResponseContent, Context = job.Context })
                                        ));
                }
            }
            await Task.WhenAll(tasks);
            return null == tasks.FirstOrDefault(t => !t.IsCompletedSuccessfully);
        }
        public async Task DeleteCommunicationAsync()
        {
            await Utilities.Utility.ExecuteSqlScriptAsync("drop-schema.sql", this.options);
        }

        public async Task CreateIfNotExistsAsync(bool recreate)
        {
            if (recreate) await DeleteCommunicationAsync();
            var str = string.Format(@"IF(SCHEMA_ID('{0}') IS NULL)
BEGIN
    EXEC sp_executesql N'CREATE SCHEMA [{0}]'
END
GO
IF  NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}_{2}]') AND type in (N'U'))
BEGIN
", options.SchemaName, options.HubName, CommunicationWorkerOptions.CommunicationTable);
            str += Utilities.Utility.BuildTableScript(typeof(T), $"{options.HubName}_{CommunicationWorkerOptions.CommunicationTable}", options.SchemaName);
            str += @"
END
GO
";
            str += await Utilities.Utility.GetScriptTextAsync("create-schema.sql", options.SchemaName, options.HubName);
            await Utilities.Utility.ExecuteSqlScriptAsync(str, this.options.ConnectionString);
        }

    }
}