using DurableTask.Core;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrchestrationService.Tests.Orchestration;
using System;
using System.Collections.Generic;
using Xunit;
using System.Linq;
using maskx.OrchestrationService.OrchestrationCreator;
using OrchestrationService.Tests.Activity;
using OrchestrationService.Tests.Worker;
using OrchestrationService.Tests.Extensions;

namespace OrchestrationService.Tests
{
    public class WorkerHostFixture : IDisposable
    {
        private IHost WorkerHost = null;

        public WorkerHostFixture()
        {
            WorkerHost = CreateHostBuilder().Build();
            OrchestrationContextExtension.ServiceProvider = WorkerHost.Services;
            TaskContextExtension.ServiceProvider = WorkerHost.Services;

            WorkerHost.RunAsync();
        }

        public void Dispose()
        {
            if (WorkerHost != null)
                WorkerHost.StopAsync();
        }

        public IHostBuilder CreateHostBuilder() =>
          Host.CreateDefaultBuilder()
              .ConfigureAppConfiguration((hostingContext, config) =>
              {
                  config
                  .AddJsonFile("appsettings.json", optional: true)
                  .AddUserSecrets("D2705D0C-A231-4B0D-84B4-FD2BFC6AD8F0");
              })
              .ConfigureServices((hostContext, services) =>
              {
                  OrchestrationContextExtension.Configuration = hostContext.Configuration;
                  TaskContextExtension.Configuration = hostContext.Configuration;

                  Dictionary<string, Type> orchestrationTypes = new Dictionary<string, Type>();
                  List<Type> activityTypes = new List<Type>();

                  orchestrationTypes.Add(typeof(PrepareVMTemplateAuthorizeOrchestration).FullName, typeof(PrepareVMTemplateAuthorizeOrchestration));
                  orchestrationTypes.Add(typeof(AsyncRequestOrchestration).FullName, typeof(AsyncRequestOrchestration));

                  activityTypes.Add(typeof(AsyncRequestActivity));

                  services.Configure<CommunicationWorkerOptions>(options => TestHelpers.Configuration.GetSection("CommunicationWorker").Bind(options));

                  services.Configure<OrchestrationWorkerOptions>(options =>
                  {
                      options.GetBuildInOrchestrators = () => orchestrationTypes.Values.ToList();
                      options.GetBuildInTaskActivities = () => activityTypes;
                      options.GetOrchestrationCreator = (orch) =>
                      {
                          switch (orch.Creator)
                          {
                              case "DefaultObjectCreator":
                                  return new DefaultObjectCreator<TaskOrchestration>(orchestrationTypes[orch.Uri]);

                              case "ARMCreator":
                                  return new ARMCreator(orch);

                              default:
                                  return null;
                          }
                      };
                  });

                  services.AddSingleton<IJobProvider>(new JobProvider());
                  services.AddSingleton((sp) =>
                  {
                      return TestHelpers.CreateOrchestrationService();
                  });

                  services.AddSingleton((sp) =>
                  {
                      return TestHelpers.CreateOrchestrationClient();
                  });

                  services.AddHostedService<OrchestrationWorker>();
                  services.AddHostedService<CommunicationWorker>();
              });
    }

    [CollectionDefinition("WorkerHost Collection")]
    public class WebHostCollection : ICollectionFixture<WorkerHostFixture>
    {
    }
}