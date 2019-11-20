using DurableTask.Core;
using DurableTask.Core.Common;
using DurableTask.Core.Settings;
using maskx.DurableTask.SQLServer;
using maskx.DurableTask.SQLServer.Settings;
using maskx.DurableTask.SQLServer.Tracking;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OrchestrationService.Tests.Orchestration;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using maskx.OrchestrationService.OrchestrationCreator;

namespace OrchestrationService.Tests
{
    public class WorkerHostFixture : IDisposable
    {
        private IHost WorkerHost = null;

        public WorkerHostFixture()
        {
            WorkerHost = CreateHostBuilder().Build();
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
                  config.AddJsonFile("appsettings.json", optional: true);
              })
              .ConfigureServices((hostContext, services) =>
              {
                  Dictionary<string, Type> orchestrationTypes = new Dictionary<string, Type>();
                  List<Type> activityTypes = new List<Type>();

                  orchestrationTypes.Add(typeof(PrepareVMTemplateAuthorizeOrchestration).FullName, typeof(PrepareVMTemplateAuthorizeOrchestration));
                  CommunicationActivity.DbConnectionString = TestHelpers.ConnectionString;
                  activityTypes.Add(typeof(CommunicationActivity));

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