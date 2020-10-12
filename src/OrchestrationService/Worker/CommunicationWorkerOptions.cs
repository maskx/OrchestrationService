namespace maskx.OrchestrationService.Worker
{
    public class CommunicationWorkerOptions : Extensions.CommunicationWorkerOptions
    {
        private const string CommunicationTable = "_Communication";
        private const string FetchCommunicationJobSP = "_FetchCommunicationJob";
        private const string UpdateCommunication = "_UpdateCommunication";
        private const string BuildFetchCommunicationJobSP = "_BuildFetchCommunicationJobSP";
        private const string FetchRuleTable = "_FetchRule";
        private const string FetchRuleLimitationTable = "_FetchRuleLimitation";

        /// <summary>
        /// Auto-creates the necessary resources for the CommunicationWorker
        /// </summary>
        public bool AutoCreate { get; set; } = false;

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string ConnectionString { get; set; }

        /// Gets or sets the hub name for the database instance store.
        /// </summary>
        public string HubName { get; set; }

        /// <summary>
        /// Gets or sets the schema name to which the tables will be added.
        /// </summary>
        public string SchemaName { get; set; } = "dbo";

        public string CommunicationTableName => $"[{SchemaName}].[{HubName}{CommunicationTable}]";
        public string FetchCommunicationJobSPName => $"[{SchemaName}].[{HubName}{FetchCommunicationJobSP}]";
        public string UpdateCommunicationSPName => $"[{SchemaName}].[{HubName}{UpdateCommunication}]";
        public string BuildFetchCommunicationJobSPName => $"[{SchemaName}].[{HubName}{BuildFetchCommunicationJobSP}]";
        public string FetchRuleTableName => $"[{SchemaName}].[{HubName}{FetchRuleTable}]";
        public string FetchRuleLimitationTableName => $"[{SchemaName}].[{HubName}{FetchRuleLimitationTable}]";
    }
}