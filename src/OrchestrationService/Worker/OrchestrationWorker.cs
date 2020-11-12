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
        private readonly TaskHubWorker taskHubWorker;
        private readonly TaskHubClient taskHubClient;
        internal readonly IJobProvider jobProvider;
        private readonly OrchestrationWorkerOptions options;
        private readonly IServiceProvider serviceProvider;
        internal List<Action<OrchestrationCompletedArgs>> OrchestrationCompletedActions { get; set; } = new List<Action<OrchestrationCompletedArgs>>();

        // hold orchestrationManager and activityManager, so we can remove unused orchestration
        private readonly DynamicNameVersionObjectManager<TaskOrchestration> orchestrationManager;

        private readonly DynamicNameVersionObjectManager<TaskActivity> activityManager;
      

        public OrchestrationWorker(
            IOrchestrationService orchestrationService,
            IOrchestrationServiceClient orchestrationServiceClient,
            IOptions<OrchestrationWorkerOptions> options,
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.options = options?.Value;
            this.jobProvider = serviceProvider.GetService<IJobProvider>();
            this.orchestrationManager = new DynamicNameVersionObjectManager<TaskOrchestration>();
            this.activityManager = new DynamicNameVersionObjectManager<TaskActivity>();
            this.taskHubWorker = new TaskHubWorker(orchestrationService,
                this.orchestrationManager,
                this.activityManager);
            this.taskHubClient = new TaskHubClient(orchestrationServiceClient);
          
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
            if (this.options.GetBuildInTaskActivities != null)
            {
                foreach (var activity in this.options.GetBuildInTaskActivities(serviceProvider))
                {
                    this.activityManager.TryAdd(new NameVersionDICreator<TaskActivity>(serviceProvider, activity.Name, activity.Version, activity.Type));
                }
            }
            if (this.options.GetBuildInOrchestrators != null)
            {
                foreach (var orchestrator in this.options.GetBuildInOrchestrators(serviceProvider))
                {
                    this.orchestrationManager.TryAdd(new NameVersionDICreator<TaskOrchestration>(serviceProvider, orchestrator.Name, orchestrator.Version, orchestrator.Type));
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
                        TaskActivity taskActivity = new ReflectionBasedTaskActivity(activities.Instance, methodInfo);
                        ObjectCreator<TaskActivity> creator =
                            new NameValueObjectCreator<TaskActivity>(
                                NameVersionHelper.GetDefaultName(methodInfo, true),
                                activities.Version,
                                taskActivity);
                        this.activityManager.Add(creator);
                    }
                }
            }

            this.taskHubWorker.StartAsync().Wait();
            if (this.options.IncludeDetails)
            {
                this.taskHubWorker.TaskOrchestrationDispatcher.IncludeDetails = true;
                this.taskHubWorker.TaskActivityDispatcher.IncludeDetails = true;
            }
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

                var taskLlist = new List<Task>();
                foreach (var item in orchestrations)
                {
                    var instance = JumpStartOrchestrationAsync(item);
                    if (instance != null)
                    {
                        taskLlist.Add(instance);
                    }
                }
                await Task.WhenAll(taskLlist);

                sw.Stop();
                if (sw.ElapsedMilliseconds < this.jobProvider.Interval)
                    await Task.Delay(this.jobProvider.Interval - (int)sw.ElapsedMilliseconds);
            }
        }

        public async Task<OrchestrationInstance> JumpStartOrchestrationAsync(Job job)
        {
            try
            {
                return await this.taskHubClient.CreateOrchestrationInstanceAsync(
                    job.Orchestration.Name,
                    job.Orchestration.Version,
                    job.InstanceId,
                    job.Input);
            }
            catch (Exception ex)
            {
                CommunicationEventSource.Log.TraceEvent(TraceEventType.Critical, "OrchestrationWorker", string.Format("Orchestration Start Failed: Id-{0},Message-{1}", job.InstanceId, ex.Message), ex.ToString(), "Error");
                return null;
            }
        }
    }
}