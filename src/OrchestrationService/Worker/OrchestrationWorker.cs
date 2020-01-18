using DurableTask.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Worker
{
    public class OrchestrationWorker : BackgroundService
    {
        private readonly ILogger<OrchestrationWorker> logger;
        private readonly TaskHubWorker taskHubWorker;
        private readonly TaskHubClient taskHubClient;
        internal readonly IJobProvider jobProvider;
        private readonly OrchestrationWorkerOptions options;
        private readonly IServiceProvider serviceProvider;
        internal List<Action<OrchestrationCompletedArgs>> OrchestrationCompletedActions { get; set; } = new List<Action<OrchestrationCompletedArgs>>();

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
            this.options = options?.Value;
            this.jobProvider = serviceProvider.GetService<IJobProvider>();
            this.orchestrationManager = new DynamicNameVersionObjectManager<TaskOrchestration>();
            this.activityManager = new DynamicNameVersionObjectManager<TaskActivity>();
            this.taskHubWorker = new TaskHubWorker(orchestrationService,
                this.orchestrationManager,
                this.activityManager);
            this.taskHubClient = new TaskHubClient(orchestrationServiceClient);
            this.orchestrationCreatorFactory = orchestrationCreatorFactory;
            if (this.options.AutoCreate)
                this.taskHubWorker.orchestrationService.CreateIfNotExistsAsync().Wait();
            // catch Orchestration Completed event
            var _ = new OrchestrationEventListener(this);
        }

        public void RegistOrchestrationCompletedAction(Action<OrchestrationCompletedArgs> action)
        {
            this.OrchestrationCompletedActions.Add(action);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.options.GetBuildInOrchestrators != null)
            {
                foreach (var activity in this.options.GetBuildInTaskActivities(serviceProvider))
                {
                    this.activityManager.TryAdd(new DICreator<TaskActivity>(serviceProvider, activity));
                }
            }
            if (this.options.GetBuildInTaskActivities != null)
            {
                foreach (var orchestrator in this.options.GetBuildInOrchestrators(serviceProvider))
                {
                    this.orchestrationManager.TryAdd(new DICreator<TaskOrchestration>(serviceProvider, orchestrator));
                }
            }

            if (this.options.GetBuildInTaskActivitiesFromInterface != null)
            {
                var interfaceActivitys = this.options.GetBuildInTaskActivitiesFromInterface(serviceProvider);
                foreach (var @interface in interfaceActivitys.Keys)
                {
                    var activities = interfaceActivitys[@interface];
                    foreach (var methodInfo in @interface.GetMethods())
                    {
                        TaskActivity taskActivity = new ReflectionBasedTaskActivity(activities, methodInfo);
                        ObjectCreator<TaskActivity> creator =
                            new NameValueObjectCreator<TaskActivity>(
                                NameVersionHelper.GetDefaultName(methodInfo, true),
                                NameVersionHelper.GetDefaultVersion(methodInfo), taskActivity);
                        this.activityManager.Add(creator);
                    }
                }
            }

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
            // TODO: support load TaskOrchestration from uri
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