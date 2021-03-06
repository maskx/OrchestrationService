﻿using DurableTask.Core;
using DurableTask.Core.Serializing;
using DurableTask.SqlServer;
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


        public static SqlOrchestrationServiceSettings CreateOrchestrationServiceSettings()
        {
            var settings = new SqlOrchestrationServiceSettings(ConnectionString, HubName)
            {
                AppName = SchemaName
            };

            return settings;
        }

        public static IOrchestrationServiceClient CreateOrchestrationClient()
        {
            return new SqlOrchestrationService(CreateOrchestrationServiceSettings());
        }
        public static IHostBuilder CreateHostBuilder(
           Action<HostBuilderContext, IServiceCollection> config = null,
            OrchestrationWorkerOptions orchestrationWorkerOptions = null,
            CommunicationWorkerOptions communicationWorkerOptions = null,
            string hubName = null)
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
                 if (string.IsNullOrEmpty(hubName)) hubName = TestHelpers.HubName;
                 config?.Invoke(hostContext, services);
                 services.UsingSQLServerOrchestration(sp => new SqlOrchestrationServiceSettings(TestHelpers.ConnectionString, hubName)
                 {
                     AppName = TestHelpers.SchemaName
                 });
                 if (orchestrationWorkerOptions == null)
                     orchestrationWorkerOptions = new OrchestrationWorkerOptions();
                 orchestrationWorkerOptions.AutoCreate = true;
                 services.UsingOrchestrationWorker(sp => orchestrationWorkerOptions);
                 if (communicationWorkerOptions == null)
                     communicationWorkerOptions = new CommunicationWorkerOptions();
                 communicationWorkerOptions.AutoCreate = true;
                 communicationWorkerOptions.ConnectionString = TestHelpers.ConnectionString;
                 communicationWorkerOptions.HubName = hubName;
                 communicationWorkerOptions.SchemaName = TestHelpers.SchemaName;
                 services.UsingCommunicationWorker<CustomCommunicationJob>(sp => communicationWorkerOptions);
                 services.UsingCommunicationWorkerClient<CustomCommunicationJob>(sp => communicationWorkerOptions);
                 services.AddSingleton<OrchestrationWorkerClient>();
                 services.AddSingleton<ICommunicationProcessor<CustomCommunicationJob>, MockRetryCommunicationProcessor>();
                 services.AddSingleton<ICommunicationProcessor<CustomCommunicationJob>, MockCommunicationProcessor>();

             });
        }
    }
}