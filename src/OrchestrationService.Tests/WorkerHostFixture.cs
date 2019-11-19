using DurableTask.Core.Common;
using DurableTask.Core.Settings;
using maskx.DurableTask.SQLServer;
using maskx.DurableTask.SQLServer.Settings;
using maskx.DurableTask.SQLServer.Tracking;
using maskx.OrchestrationService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OrchestrationService.Tests
{
    public class WorkerHostFixture : IDisposable
    {
        private IHost WorkerHost = null;

        public WorkerHostFixture()
        {
            WorkerHost = CreateHostBuilder().Build();
            WorkerHost.Run();
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
                  services.AddSingleton<JobProvider>();
                  services.AddSingleton((sp) =>
                  {
                      return TestHelpers.CreateOrchestrationService();
                  });
                  services.AddSingleton((sp) =>
                  {
                      return TestHelpers.CreateOrchestrationClient();
                  });
                  services.AddHostedService<OrchestrationWorker>();
              });
    }

    [CollectionDefinition("WorkerHost Collection")]
    public class WebHostCollection : ICollectionFixture<WorkerHostFixture>
    {
    }
}