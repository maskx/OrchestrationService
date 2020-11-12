using DurableTask.Core;
using maskx.OrchestrationService.Activity;
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
using maskx.OrchestrationService.Extensions;

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

            var p = this.serviceProvider.GetServices<ICommunicationProcessor<T>>();
            foreach (var item in p)
            {
                // todo: CommunicationWorker can be set by DI ?
                item.CommunicationWorker = this;
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
            using (var db = new SQLServerAccess(this.options.ConnectionString))
            {
                await db.ExecuteStoredProcedureASync(this.options.FetchCommunicationJobSPName, (reader, index) =>
                 {
                     //var job = new T()
                     //{
                     //    InstanceId = reader["InstanceId"].ToString(),
                     //    ExecutionId = reader["ExecutionId"].ToString(),
                     //    EventName = reader["EventName"].ToString(),
                     //    RequestId = reader["RequestId"].ToString(),
                     //    Processor = reader["Processor"].ToString(),
                     //    RequestTo = reader["RequestTo"]?.ToString(),
                     //    CreateTime = DateTime.Parse(reader["CreateTime"].ToString()),
                     //    LockedUntilUtc = DateTime.Parse(reader["LockedUntilUtc"].ToString()),
                     //    RequestOperation = reader["RequestOperation"]?.ToString(),
                     //    RequestContent = reader["RequestContent"]?.ToString(),
                     //    RequestProperty = reader["RequestProperty"]?.ToString(),
                     //    Status = (CommunicationJob.JobStatus)(int)reader["Status"],
                     //    ResponseContent = reader["ResponseContent"]?.ToString(),
                     //};
                     //if (reader["Context"] != DBNull.Value)
                     //    job.Context = reader["Context"].ToString();
                     //if (reader["ResponseCode"] != DBNull.Value)
                     //    job.ResponseCode = (int)reader["ResponseCode"];
                     //job.RuleField = new Dictionary<string, object>();
                     //foreach (var item in options.RuleFields.Keys)
                     //{
                     //    job.RuleField.Add(item, reader[item]);
                     //}
                     var job = reader.CreateObject<T>();
                     jobs.Add(job);
                 }, new
                 {
                     LockedBy = AgentId,
                     this.options.MessageLockedSeconds,
                     MaxCount = options.MaxConcurrencyRequest - RunningTaskCount
                 });
            }
            return jobs;
        }

        private async Task ProcessJobs(ICommunicationProcessor<T> processor, params T[] jobs)
        {
            //使用Task.Run包装CommunicationProcessor里的代码执行，避免CommunicationProcessor里有类似于Thread.Sleep这样阻塞线程的情况
            //CommunicationProcessor里不要使用Task.WaitAll,建议使用Task.WhenAll
            var response = await Task.Run(async () => await processor.ProcessAsync(jobs));
            await Task.WhenAll(UpdateJobs(response), RaiseEvent(response));
        }

        public async Task UpdateJobs(params CommunicationJob[] jobs)
        {
            using var db = new SQLServerAccess(this.options.ConnectionString);
            foreach (var job in jobs)
            {
                await db.ExecuteStoredProcedureASync(options.UpdateCommunicationSPName, new
                {
                    Status = (int)job.Status,
                    job.Context,
                    job.ResponseCode,
                    job.RequestId,
                    job.ResponseContent,
                    options.MessageLockedSeconds
                });
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
                                        new TaskResult(job.ResponseCode, new CommunicationResult { ResponseContent = job.ResponseContent, Context = job.Context })
                                        ));
                }
            }
            await Task.WhenAll(tasks);
        }
        public async Task DeleteCommunicationAsync()
        {
            await Utilities.Utility.ExecuteSqlScriptAsync("drop-schema.sql", this.options);
        }
        // todo: support create custom communicationjob table
        public async Task CreateIfNotExistsAsync(bool recreate)
        {
            if (recreate) await DeleteCommunicationAsync();
            await Utilities.Utility.ExecuteSqlScriptAsync("create-schema.sql", this.options);
        }

    }
}