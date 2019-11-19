using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using maskx.OrchestrationService.OrchestrationCreator;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace maskx.OrchestrationService
{
    public class OrchestrationWorker : BackgroundService
    {
        private readonly ILogger<OrchestrationWorker> logger;
        private readonly TaskHubWorker taskHubWorker;
        private readonly TaskHubClient taskHubClient;
        private readonly IJobProvider jobProvider;
        private readonly OrchestrationWorkerOptions options;
        private readonly Dictionary<string, Orchestration> OrchestrationDefine;

        // hold orchestrationManager and activityManager, so we can remove unused orchestration
        private readonly DynamicNameVersionObjectManager<TaskOrchestration> orchestrationManager;

        private readonly DynamicNameVersionObjectManager<TaskActivity> activityManager;

        public OrchestrationWorker(ILogger<OrchestrationWorker> logger,
            IOrchestrationService orchestrationService,
            IOrchestrationServiceClient orchestrationServiceClient,
            IJobProvider jobProvider,
            IOptions<OrchestrationWorkerOptions> options)
        {
            this.logger = logger;
            this.options = options.Value;
            this.jobProvider = jobProvider;
            this.orchestrationManager = new DynamicNameVersionObjectManager<TaskOrchestration>();
            this.activityManager = new DynamicNameVersionObjectManager<TaskActivity>();
            this.taskHubWorker = new TaskHubWorker(orchestrationService,
                this.orchestrationManager,
                this.activityManager);
            this.taskHubClient = new TaskHubClient(orchestrationServiceClient);
            this.OrchestrationDefine = new Dictionary<string, Orchestration>();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var activity in this.options.GetBuildInTaskActivities())
            {
                this.activityManager.TryAdd(new DefaultObjectCreator<TaskActivity>(activity));
            }
            foreach (var orchestrator in this.options.GetBuildInOrchestrators())
            {
                this.orchestrationManager.TryAdd(new DefaultObjectCreator<TaskOrchestration>(orchestrator));
            }
            this.taskHubWorker.StartAsync();
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.taskHubWorker.StopAsync();
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Stopwatch sw = new Stopwatch();
            while (!stoppingToken.IsCancellationRequested)
            {
                var orchestrations = await this.jobProvider.FetchAsync(this.options.FetchJobCount);
                sw.Restart();
                try
                {
                    var taskLlist = new List<Task>();
                    foreach (var item in orchestrations)
                    {
                        taskLlist.Add(JumpStartOrchestrationAsync(item));
                    }
                    await Task.WhenAll(taskLlist);
                }
                catch (Exception ex)
                {
                    throw;
                }
                sw.Stop();
                if (sw.ElapsedMilliseconds < this.jobProvider.Interval)
                    await Task.Delay(this.jobProvider.Interval - (int)sw.ElapsedMilliseconds);
            }
        }

        private async Task JumpStartOrchestrationAsync(Job job)
        {
            var creator = new ARMCreator(job.Orchestration);
            this.orchestrationManager.TryAdd(creator);
            var instance = await this.taskHubClient.CreateOrchestrationInstanceAsync(
                creator.Name,
                creator.Version,
                job.InstanceId,
                job.Input);
        }
    }
}