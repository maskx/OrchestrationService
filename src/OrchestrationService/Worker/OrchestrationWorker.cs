using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using DurableTask.Core.History;
using maskx.OrchestrationService.OrchestrationCreator;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using DurableTask.Core.Common;

namespace maskx.OrchestrationService.Worker
{
    public class OrchestrationWorker : BackgroundService
    {
        private readonly ILogger<OrchestrationWorker> logger;
        private readonly TaskHubWorker taskHubWorker;
        private readonly TaskHubClient taskHubClient;
        private readonly IJobProvider jobProvider;
        private readonly OrchestrationWorkerOptions options;
        private readonly IServiceProvider serviceProvider;

        // hold orchestrationManager and activityManager, so we can remove unused orchestration
        private readonly DynamicNameVersionObjectManager<TaskOrchestration> orchestrationManager;

        private readonly DynamicNameVersionObjectManager<TaskActivity> activityManager;
        private readonly IOrchestrationCreatorFactory orchestrationCreatorFactory;

        public OrchestrationWorker(ILogger<OrchestrationWorker> logger,
            IOrchestrationService orchestrationService,
            IOrchestrationServiceClient orchestrationServiceClient,
            IOptions<OrchestrationWorkerOptions> options,
            IServiceProvider serviceProvider,
            IOrchestrationCreatorFactory orchestrationCreatorFactory)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            this.options = options.Value;
            this.jobProvider = serviceProvider.GetService<IJobProvider>();
            this.orchestrationManager = new DynamicNameVersionObjectManager<TaskOrchestration>();
            this.activityManager = new DynamicNameVersionObjectManager<TaskActivity>();
            this.taskHubWorker = new TaskHubWorker(orchestrationService,
                this.orchestrationManager,
                this.activityManager);
            this.taskHubClient = new TaskHubClient(orchestrationServiceClient);
            this.orchestrationCreatorFactory = orchestrationCreatorFactory;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var activity in this.options.GetBuildInTaskActivities())
            {
                this.activityManager.TryAdd(new DICreator<TaskActivity>(serviceProvider, activity));
            }
            foreach (var orchestrator in this.options.GetBuildInOrchestrators())
            {
                this.orchestrationManager.TryAdd(new DICreator<TaskOrchestration>(serviceProvider, orchestrator));
            }
            this.taskHubWorker.orchestrationService.CreateIfNotExistsAsync().Wait();
            this.taskHubWorker.StartAsync().Wait();

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.taskHubWorker.StopAsync();
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (this.jobProvider == null)
                return;
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
                    throw ex;
                }
                sw.Stop();
                if (sw.ElapsedMilliseconds < this.jobProvider.Interval)
                    await Task.Delay(this.jobProvider.Interval - (int)sw.ElapsedMilliseconds);
            }
        }

        public async Task<OrchestrationInstance> JumpStartOrchestrationAsync(Job job)
        {
            ObjectCreator<TaskOrchestration> creator = this.orchestrationManager.GetCreator(job.Orchestration.Uri);
            if (creator == null)
            {
                creator = this.orchestrationCreatorFactory.Create<ObjectCreator<TaskOrchestration>>(job.Orchestration.Creator, job.Orchestration.Uri);
                this.orchestrationManager.TryAdd(creator);
            }
            return await this.taskHubClient.CreateOrchestrationInstanceAsync(
                creator.Name,
                creator.Version,
                job.InstanceId,
                job.Input);
        }
    }
}