using maskx.DurableTask.SQLServer.Settings;
using maskx.OrchestrationService.Worker;

namespace maskx.OrchestrationService.Extensions
{
    public class SqlServerConfiguration
    {
        /// <summary>
        /// Auto-creates the necessary resources for the orchestration service and the communication service
        /// </summary>
        public bool AutoCreate { get; set; } = false;

        public string ConnectionString { get; set; }
        public string HubName { get; set; } = "DTF";

        /// <summary>
        /// Gets or sets the schema name to which the tables will be added.
        /// </summary>
        public string SchemaName { get; set; } = "dbo";

        public SQLServerOrchestrationServiceSettings OrchestrationServiceSettings { get; set; } = new SQLServerOrchestrationServiceSettings();
        public OrchestrationWorkerOptions OrchestrationWorkerOptions { get; set; } = new OrchestrationWorkerOptions();
        public CommunicationWorkerOptions CommunicationWorkerOptions { get; set; } = new CommunicationWorkerOptions();
    }
}