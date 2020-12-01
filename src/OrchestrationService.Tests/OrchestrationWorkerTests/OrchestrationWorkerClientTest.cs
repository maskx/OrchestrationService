using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Extensions;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using Xunit;

namespace OrchestrationService.Tests.OrchestrationWorkerTests
{
    [Trait("c", "OrchestrationWorkerClientTest")]
    public class OrchestrationWorkerClientTest : IDisposable
    {
        readonly OrchestrationWorkerClient _OrchestrationWorkerClient;
        readonly IOrchestrationService _OrchestrationService;
        public OrchestrationWorkerClientTest()
        {
            List<(string Name, string Version, Type Type)> orchestrationTypes = new()
            {
                ("TestOrchestration", "", typeof(TestOrchestration))
            };
            var workerHost = Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config
                        .AddJsonFile("appsettings.json", optional: true)
                        .AddUserSecrets("D2705D0C-A231-4B0D-84B4-FD2BFC6AD8F0");
                    })
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.UsingSQLServerOrchestration(sp=>new SqlServerOrchestrationConfiguration()
                        {
                            SchemaName = "comm",
                            HubName = "client",
                            ConnectionString = TestHelpers.ConnectionString,
                        });
                        services.AddSingleton<OrchestrationWorkerClient>();
                    }).Build();
            workerHost.RunAsync();
            _OrchestrationWorkerClient = workerHost.Services.GetService<OrchestrationWorkerClient>();
            _OrchestrationService = workerHost.Services.GetService<IOrchestrationService>();
            _OrchestrationService.CreateIfNotExistsAsync().Wait();
        }

        public void Dispose()
        {
            if (_OrchestrationService != null)
                _OrchestrationService.DeleteAsync(true).Wait();
            GC.SuppressFinalize(this);
        }

        [Fact(DisplayName = "JumpStartOrchestrationAsync")]
        public void JumpStartOrchestrationAsync()
        {
            var instance = _OrchestrationWorkerClient.JumpStartOrchestrationAsync(new Job
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new OrchestrationSetting()
                {
                    Name = typeof(TestOrchestration).Name,
                },
                Input = ""
            }).Result;
            var r = _OrchestrationWorkerClient.GetOrchestrationStateAsync(instance.InstanceId).Result;
            Assert.NotNull(r);
        }
    }
}