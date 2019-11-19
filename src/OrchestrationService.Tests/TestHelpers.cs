using DurableTask.Core;
using DurableTask.Core.Common;
using DurableTask.Core.Settings;
using maskx.DurableTask.SQLServer;
using maskx.DurableTask.SQLServer.Settings;
using maskx.DurableTask.SQLServer.Tracking;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace OrchestrationService.Tests
{
    internal class TestHelpers
    {
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
                GetDatabaseConnection = () => Task.Run(() => new SqlConnection(TestHelpers.ConnectionString) as DbConnection)
            });
        }

        public static SQLServerOrchestrationServiceSettings CreateOrchestrationServiceSettings(CompressionStyle style = CompressionStyle.Threshold)
        {
            var settings = new SQLServerOrchestrationServiceSettings
            {
                TaskOrchestrationDispatcherSettings = { CompressOrchestrationState = true },
                MessageCompressionSettings = new CompressionSettings { Style = style, ThresholdInBytes = 1024 }
            };

            return settings;
        }

        public static IOrchestrationService CreateOrchestrationService()
        {
            return new SQLServerOrchestrationService(
                         TestHelpers.ConnectionString,
                         TestHelpers.HubName,
                         CreateSQLServerInstanceStore(),
                         null,
                         CreateOrchestrationServiceSettings());
        }

        public static IOrchestrationServiceClient CreateOrchestrationClient()
        {
            return new SQLServerOrchestrationService(
                         TestHelpers.ConnectionString,
                         TestHelpers.HubName,
                         CreateSQLServerInstanceStore(),
                         null,
                         CreateOrchestrationServiceSettings());
        }
    }
}