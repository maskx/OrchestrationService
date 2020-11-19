using maskx.DurableTask.SQLServer.Settings;

namespace maskx.OrchestrationService.Extensions
{
    public class SqlServerOrchestrationConfiguration : SQLServerOrchestrationServiceSettings
    {
        public string ConnectionString { get; set; }
        public string HubName { get; set; } = "DTF";
    }
}
