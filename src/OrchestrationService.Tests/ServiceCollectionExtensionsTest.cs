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

namespace OrchestrationService.Tests
{
    [Trait("c", "ServiceCollectionExtensionsTest")]
    public class ServiceCollectionExtensionsTest
    {
        private void RunHost<T>(Action<IServiceCollection> config)
        {
            var webHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddUserSecrets("D2705D0C-A231-4B0D-84B4-FD2BFC6AD8F0");
                })
                .ConfigureServices((hostContext, services) =>
                {
                    config(services);
                })
                .Build();
            webHost.RunAsync();
            var worker = webHost.Services.GetService<OrchestrationWorker>();
            var client = webHost.Services.GetService<OrchestrationWorkerClient>();
            var instance = client.JumpStartOrchestrationAsync(new Job
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new OrchestrationSetting()
                {
                    Name = typeof(OrchestrationWorkerTests.TestOrchestration).Name
                },
                Input = ""
            }).Result;
            while (true)
            {
                var result = client.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    Assert.Equal("1", result.Output);
                    break;
                }
            }
        }

        [Fact(DisplayName = "UsingSqlServer")]
        public void UsingSqlServer()
        {
            var orchestrationTypes = new List<(string Name, string Version, Type Type)>();
            orchestrationTypes.Add(("TestOrchestration", "", typeof(OrchestrationWorkerTests.TestOrchestration)));
            var activityTypes = new List<(string Name, string Version, Type Type)>();
            var sqlConfig = new SqlServerConfiguration()
            {
                ConnectionString = TestHelpers.ConnectionString,
                HubName = "sql",
                SchemaName = "sql",
                AutoCreate = true,
                OrchestrationWorkerOptions = new maskx.OrchestrationService.Extensions.OrchestrationWorkerOptions()
                {
                    GetBuildInOrchestrators = (sp) => { return orchestrationTypes; },
                    GetBuildInTaskActivities = (sp) => { return activityTypes; }
                }
            };
            RunHost<SqlServerConfiguration>((sp) => sp.UsingOrchestration(sqlConfig));
        }
        [Fact(DisplayName = "UsingSqlServerFunc")]
        public void UsingSqlServerFunc()
        {
            var orchestrationTypes = new List<(string Name, string Version, Type Type)>();
            orchestrationTypes.Add(("TestOrchestration", "", typeof(OrchestrationWorkerTests.TestOrchestration)));
            var activityTypes = new List<(string Name, string Version, Type Type)>();
            var sqlConfig = new SqlServerConfiguration()
            {
                ConnectionString = TestHelpers.ConnectionString,
                HubName = "sql",
                SchemaName = "sql",
                AutoCreate = true,
                OrchestrationWorkerOptions = new maskx.OrchestrationService.Extensions.OrchestrationWorkerOptions()
                {
                    GetBuildInOrchestrators = (sp) => { return orchestrationTypes; },
                    GetBuildInTaskActivities = (sp) => { return activityTypes; }
                }
            };
            RunHost<SqlServerConfiguration>((sp) =>
            {
                sp.UsingOrchestration((sp) =>
                {
                    return sqlConfig;

                });
            });
        }
    }
}