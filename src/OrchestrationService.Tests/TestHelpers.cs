using DurableTask.Core;
using DurableTask.Core.Common;
using DurableTask.Core.Serializing;
using maskx.DurableTask.SQLServer;
using maskx.DurableTask.SQLServer.Settings;
using maskx.DurableTask.SQLServer.Tracking;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Orchestration;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrchestrationService.Tests.CommunicationWorkerTests;
using System;
using System.Collections.Generic;

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

        public static TaskHubClient TaskHubClient { get; private set; }
        public static string HubName { get { return Configuration["HubName"]; } }

        static TestHelpers()
        {
            Configuration = GetIConfigurationRoot(AppContext.BaseDirectory);
            TaskHubClient = new TaskHubClient(CreateOrchestrationClient());
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

        public static SQLServerOrchestrationServiceSettings CreateOrchestrationServiceSettings(CompressionStyle style = CompressionStyle.Threshold)
        {
            var settings = new SQLServerOrchestrationServiceSettings
            {
                TaskOrchestrationDispatcherSettings = { CompressOrchestrationState = true }
            };

            return settings;
        }

        public static IOrchestrationService CreateOrchestrationService()
        {
            var service = new SQLServerOrchestrationService(
                         TestHelpers.ConnectionString,
                         TestHelpers.HubName,
                         CreateSQLServerInstanceStore(),
                         CreateOrchestrationServiceSettings());
            service.CreateIfNotExistsAsync().Wait();
            return service;
        }

        public static IOrchestrationServiceClient CreateOrchestrationClient()
        {
            return new SQLServerOrchestrationService(
                         TestHelpers.ConnectionString,
                         TestHelpers.HubName,
                         CreateSQLServerInstanceStore(),
                         CreateOrchestrationServiceSettings());
        }

        public static IHostBuilder CreateHostBuilder(
            CommunicationWorkerOptions communicationWorkerOptions = null,
            List<Type> orchestrationTypes = null,
            Action<HostBuilderContext, IServiceCollection> config = null)
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
                 if (config != null)
                     config(hostContext, services);
                 services.AddHttpClient();
                 services.AddSingleton((sp) =>
                 {
                     return CreateOrchestrationClient();
                 });
                 services.AddSingleton((sp) =>
                 {
                     return CreateOrchestrationService();
                 });

                 #region OrchestrationWorker

                 services.AddSingleton<IOrchestrationCreatorFactory>((sp) =>
                 {
                     OrchestrationCreatorFactory orchestrationCreatorFactory = new OrchestrationCreatorFactory(sp);
                     orchestrationCreatorFactory.RegistCreator("DICreator", typeof(DICreator<TaskOrchestration>));
                     orchestrationCreatorFactory.RegistCreator("DefaultObjectCreator", typeof(DefaultObjectCreator<TaskOrchestration>));
                     return orchestrationCreatorFactory;
                 });
                 if (orchestrationTypes == null)
                     orchestrationTypes = new List<Type>();
                 List<Type> activityTypes = new List<Type>();

                 orchestrationTypes.Add(typeof(AsyncRequestOrchestration));

                 activityTypes.Add(typeof(AsyncRequestActivity));
                 activityTypes.Add(typeof(HttpRequestActivity));
                 activityTypes.Add(typeof(TraceActivity));
                 services.Configure<OrchestrationWorkerOptions>(options =>
                 {
                     options.GetBuildInOrchestrators = () => orchestrationTypes;
                     options.GetBuildInTaskActivities = () => activityTypes;
                 });

                 services.AddSingleton<OrchestrationWorker>();
                 services.AddSingleton<IHostedService>(p => p.GetService<OrchestrationWorker>());

                 #endregion OrchestrationWorker

                 #region CommunicationWorker

                 services.AddSingleton<ICommunicationProcessor>(new MockCommunicationProcessor());
                 services.AddSingleton<ICommunicationProcessor>(new MockRetryCommunicationProcessor());
                 services.Configure<CommunicationWorkerOptions>((options) =>
                 {
                     TestHelpers.Configuration.GetSection("CommunicationWorker").Bind(options);
                     if (communicationWorkerOptions != null)
                     {
                         options.GetFetchRules = communicationWorkerOptions.GetFetchRules;
                         options.HubName = communicationWorkerOptions.HubName;
                         options.MaxConcurrencyRequest = communicationWorkerOptions.MaxConcurrencyRequest;
                         options.RuleFields.AddRange(communicationWorkerOptions.RuleFields);
                         options.SchemaName = communicationWorkerOptions.SchemaName;
                     }
                 });
                 services.AddHostedService<CommunicationWorker>();

                 #endregion CommunicationWorker

                 services.AddSingleton<OrchestrationWorkerClient>();
             });
        }
    }
}