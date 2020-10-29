﻿using DurableTask.Core;
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

namespace maskx.OrchestrationService.Worker
{
    public class CommunicationWorker : BackgroundService
    {
        private readonly TaskHubClient taskHubClient;
        private readonly CommunicationWorkerOptions options;
        private readonly Dictionary<string, ICommunicationProcessor> processors;
        // 'todo: communication table add agentId column
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
            this.processors = new Dictionary<string, ICommunicationProcessor>();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.options == null)
            {
                TraceActivityEventSource.Log.Critical(nameof(CommunicationWorker), string.Empty, string.Empty, "CommunicationWorker can not start", "options is null", "Failed");
                return;
            }

            var p = this.serviceProvider.GetServices<ICommunicationProcessor>();
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
            TraceActivityEventSource.Log.Critical(nameof(CommunicationWorker), string.Empty, string.Empty, "CommunicationWorker is stoped", "CommunicationWorker is stoped", "Failed");

            return base.StopAsync(cancellationToken);
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
            using (var db = new SQLServerAccess(this.options.ConnectionString))
            {
                await db.ExecuteStoredProcedureASync(this.options.FetchCommunicationJobSPName, (reader, index) =>
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
                 }, new
                 {
                     LockedBy = AgentId,
                     MessageLockedSeconds = this.options.MessageLockedSeconds,
                     MaxCount = options.MaxConcurrencyRequest - RunningTaskCount
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

        public async Task CreateIfNotExistsAsync(bool recreate)
        {
            if (recreate) await DeleteCommunicationAsync();
            await Utilities.Utility.ExecuteSqlScriptAsync("create-schema.sql", this.options);
        }

        public async Task<List<FetchRule>> GetFetchRuleAsync()
        {
            using var db = new SQLServerAccess(options.ConnectionString);
            db.AddStatement($"select Id,Name,Description,What,Scope,Concurrency,CreatedTimeUtc,UpdatedTimeUtc from {options.FetchRuleTableName}");
            List<FetchRule> rules = new List<FetchRule>();
            await db.ExecuteReaderAsync((reader, index) =>
            {
                rules.Add(new FetchRule()
                {
                    Id = reader.GetGuid(0),
                    Name = reader["Name"].ToString(),
                    Description = reader["Description"]?.ToString(),
                    What = reader.IsDBNull(3) ? new List<Where>() : FetchRule.DeserializeWhat(reader.GetString(3)),
                    Scope = reader.IsDBNull(4) ? new List<string>() : FetchRule.DeserializeScope(reader.GetString(4)),
                    Concurrency=reader.GetInt32(5),
                    CreatedTimeUtc = reader.GetDateTime(6),
                    UpdatedTimeUtc = reader.IsDBNull(7) ? default(DateTime) : reader.GetDateTime(7)
                }); ;
            });
            return rules;
        }
        public async Task<FetchRule> GetFetchRuleAsync(Guid id)
        {
            using var db = new SQLServerAccess(options.ConnectionString);
            db.AddStatement($"select Id,Name,Description,What,Scope,Concurrency,CreatedTimeUtc,UpdatedTimeUtc from {options.FetchRuleTableName} where Id=@Id", new { Id = id });
            FetchRule rule = null;
            await db.ExecuteReaderAsync((reader, index) =>
            {
                    rule = new FetchRule()
                    {
                        Id = id,
                        Name = reader[1].ToString(),
                        Description =reader.IsDBNull(2)?null: reader[2].ToString(),
                        What = reader.IsDBNull(3) ? new List<Where>() : FetchRule.DeserializeWhat(reader.GetString(3)),
                        Scope = reader.IsDBNull(4) ? new List<string>() : FetchRule.DeserializeScope(reader.GetString(4)),
                        Concurrency = reader.GetInt32(5),
                        CreatedTimeUtc = reader.GetDateTime(6),
                        UpdatedTimeUtc = reader.IsDBNull(7) ? default(DateTime) : reader.GetDateTime(7)
                    }; 
            });
            return rule;
        }
        public async Task DeleteFetchRuleAsync(Guid id)
        {
            using var db = new SQLServerAccess(options.ConnectionString);
            db.AddStatement($"delete {options.FetchRuleTableName} where Id=@Id", new { Id = id });
            await db.ExecuteNonQueryAsync();
        }
        public async Task<FetchRule> CreateFetchRuleAsync(FetchRule fetchRule)
        {
            using var db = new SQLServerAccess(options.ConnectionString);
            db.AddStatement($"INSERT INTO {options.FetchRuleTableName} ([Name],[Description],[What],[Scope],[Concurrency]) OUTPUT inserted.Id,inserted.CreatedTimeUtc,inserted.UpdatedTimeUtc  VALUES (@Name,@Description,@What,@Scope,@Concurrency)",
                new
                {
                    Name = fetchRule.Name,
                    Description = fetchRule.Description,
                    What = FetchRule.SerializeWhat(fetchRule.What),
                    Scope=FetchRule.SerializeScope(fetchRule.Scope),
                    Concurrency=fetchRule.Concurrency
                });   
            await db.ExecuteReaderAsync((reader, index) =>
            {
                fetchRule.Id = reader.GetGuid(0);
                fetchRule.CreatedTimeUtc = reader.GetDateTime(1);
                fetchRule.UpdatedTimeUtc = reader.GetDateTime(2);
            });

            return fetchRule;
        }
        public async Task<FetchRule> UpdateFetchRuleAsync(FetchRule fetchRule)
        {
            using var db = new SQLServerAccess(options.ConnectionString);
            db.AddStatement($"update {options.FetchRuleTableName} set Name=@Name,Description=@Description,What=@What,Scope=@Scope,Concurrency=@Concurrency,UpdatedTimeUtc=getutcdate() where Id=@Id",
                new
                {
                    Name = fetchRule.Name,
                    Description = fetchRule.Description,
                    What = FetchRule.SerializeWhat(fetchRule.What),
                    Scope=FetchRule.SerializeScope(fetchRule.Scope),
                    Concurrency=fetchRule.Concurrency,
                    Id=fetchRule.Id
                });
            await db.ExecuteNonQueryAsync();
            return fetchRule;
        }
        /// <summary>
        /// Apply the fetch rule settings 
        /// </summary>
        /// <returns></returns>
        public async Task BuildFetchCommunicationJobSPAsync()
        {
            using var db = new SQLServerAccess(options.ConnectionString);
            await db.ExecuteStoredProcedureASync(options.BuildFetchCommunicationJobSPName);
        }
    }
}