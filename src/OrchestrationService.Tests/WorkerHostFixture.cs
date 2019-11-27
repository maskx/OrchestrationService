﻿using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Orchestration;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrchestrationService.Tests.Orchestration;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OrchestrationService.Tests
{
    public class WorkerHostFixture : IDisposable
    {
        private IHost workerHost = null;

        public WorkerHostFixture()
        {
            CommunicationWorkerOptions options = new CommunicationWorkerOptions();
            options.HubName = "NoRule";
            List<Type> orchestrationTypes = new List<Type>();
            orchestrationTypes.Add(typeof(PrepareVMTemplateAuthorizeOrchestration));
            workerHost = TestHelpers.CreateHostBuilder(options, orchestrationTypes).Build();
            workerHost.RunAsync();
        }

        public void Dispose()
        {
            if (workerHost != null)
                workerHost.StopAsync();
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
                  Dictionary<string, Type> orchestrationTypes = new Dictionary<string, Type>();
                  List<Type> activityTypes = new List<Type>();

                  orchestrationTypes.Add(typeof(PrepareVMTemplateAuthorizeOrchestration).FullName, typeof(PrepareVMTemplateAuthorizeOrchestration));
                  orchestrationTypes.Add(typeof(AsyncRequestOrchestration).FullName, typeof(AsyncRequestOrchestration));

                  activityTypes.Add(typeof(AsyncRequestActivity));

                  services.Configure<CommunicationWorkerOptions>(options => TestHelpers.Configuration.GetSection("CommunicationWorker").Bind(options));

                  services.AddSingleton<IOrchestrationCreatorFactory>((sp) =>
                  {
                      OrchestrationCreatorFactory orchestrationCreatorFactory = new OrchestrationCreatorFactory(sp);
                      orchestrationCreatorFactory.RegistCreator("DICreator", typeof(DICreator<TaskOrchestration>));
                      orchestrationCreatorFactory.RegistCreator("DefaultObjectCreator", typeof(DefaultObjectCreator<TaskOrchestration>));
                      return orchestrationCreatorFactory;
                  });
                  services.Configure<OrchestrationWorkerOptions>(options =>
                  {
                      options.GetBuildInOrchestrators = () => orchestrationTypes.Values.ToList();
                      options.GetBuildInTaskActivities = () => activityTypes;
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
                  services.AddHttpClient();
              });
    }

    [CollectionDefinition("WorkerHost Collection")]
    public class WebHostCollection : ICollectionFixture<WorkerHostFixture>
    {
    }
}