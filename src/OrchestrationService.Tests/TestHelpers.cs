using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.DurableTask.SQLServer;
using maskx.DurableTask.SQLServer.Settings;
using maskx.DurableTask.SQLServer.Tracking;
using maskx.OrchestrationService.Extensions;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrchestrationService.Tests.CommunicationWorkerTests;
using System;

namespace OrchestrationService.Tests
{
    internal class TestHelpers
    {
        public static DataConverter DataConverter { get; private set; } = new JsonDataConverter();
        public static IConfigurationRoot Configuration { get; private set; }

        public static string ConnectionString
        {
            get
            {
                return Configuration.GetConnectionString("dbConnection");
            }
        }

        public static string HubName { get { return Configuration["HubName"]; } }
        public static string SchemaName { get { return Configuration["SchemaName"]; } }

        static TestHelpers()
        {
            Configuration = GetIConfigurationRoot(AppContext.BaseDirectory);
        }

        private static IConfigurationRoot GetIConfigurationRoot(string outputPath)
        {
            return new ConfigurationBuilder()
                .SetBasePath(outputPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets("D2705D0C-A231-4B0D-84B4-FD2BFC6AD8F0")
                .Build();
        }
        public static SqlServerInstanceStore CreateSQLServerInstanceStore()
        {
            return new SqlServerInstanceStore(new SqlServerInstanceStoreSettings()
            {
                HubName = TestHelpers.HubName,
                ConnectionString = TestHelpers.ConnectionString
            });
        }

        public static SQLServerOrchestrationServiceSettings CreateOrchestrationServiceSettings()
        {
            var settings = new SQLServerOrchestrationServiceSettings
            {
                TaskOrchestrationDispatcherSettings = { CompressOrchestrationState = true }
            };

            return settings;
        }

        public static IOrchestrationServiceClient CreateOrchestrationClient()
        {
            return new SQLServerOrchestrationService(
                         TestHelpers.ConnectionString,
                         TestHelpers.HubName,
                         CreateSQLServerInstanceStore(),
                         CreateOrchestrationServiceSettings());
        }
        public static IHostBuilder CreateHostBuilder(Action<HostBuilderContext, IServiceCollection> config = null,
            maskx.OrchestrationService.Extensions.OrchestrationWorkerOptions orchestrationWorkerOptions = null,
            maskx.OrchestrationService.Extensions.CommunicationWorkerOptions communicationWorkerOptions = null,
            string hubName = null)
        {
            return CreateHostBuilder<CommunicationJob>(config, orchestrationWorkerOptions, communicationWorkerOptions, hubName);
        }
        public static IHostBuilder CreateHostBuilder<T>(
       Action<HostBuilderContext, IServiceCollection> config = null,
        maskx.OrchestrationService.Extensions.OrchestrationWorkerOptions orchestrationWorkerOptions = null,
        maskx.OrchestrationService.Extensions.CommunicationWorkerOptions communicationWorkerOptions = null,
        string hubName = null) where T : CommunicationJob, new()
        {
            return Host.CreateDefaultBuilder()
             .ConfigureAppConfiguration((hostingContext, config) =>
             {
                 config
                 .AddJsonFile("appsettings.json", optional: true)
                 .AddUserSecrets("D2705D0C-A231-4B0D-84B4-FD2BFC6AD8F0");
             })
             .ConfigureServices((hostContext, services) =>
             {
                 config?.Invoke(hostContext, services);
                 services.UsingOrchestration<T>((sp) =>
                 {
                     var conf = new SqlServerConfiguration()
                     {
                         AutoCreate = true,
                         ConnectionString = TestHelpers.ConnectionString,
                         HubName = TestHelpers.HubName,
                         SchemaName = TestHelpers.SchemaName,
                         OrchestrationServiceSettings = new SQLServerOrchestrationServiceSettings() { SchemaName = TestHelpers.SchemaName }
                     };
                     if (hubName != null)
                         conf.HubName = hubName;
                     if (orchestrationWorkerOptions != null)
                         conf.OrchestrationWorkerOptions = orchestrationWorkerOptions;
                     if (communicationWorkerOptions != null)
                         conf.CommunicationWorkerOptions = communicationWorkerOptions;

                     return conf;
                 });
                 services.AddSingleton<ICommunicationProcessor<T>>(new MockCommunicationProcessor<T>());
                 services.AddSingleton<ICommunicationProcessor<T>>(new MockRetryCommunicationProcessor<T>());

             });
        }
    }
}