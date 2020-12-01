namespace maskx.OrchestrationService.Worker
{
    public class CommunicationWorkerOptions
    {
        /// <summary>
        /// Message locked time
        /// </summary>
        public double MessageLockedSeconds { get; set; } = 300;

        /// <summary>
        /// Idel time when no job fetched
        /// </summary>
        public int IdelMilliseconds { get; set; } = 10000;

        /// <summary>
        /// 外部系统请求的最大并发数
        /// </summary>
        public int MaxConcurrencyRequest { get; set; } = 100;


        internal const string CommunicationTable = "Communication";
        internal const string FetchCommunicationJobSP = "FetchCommunicationJob";
        internal const string UpdateCommunicationSP = "UpdateCommunication";
        internal const string BuildFetchCommunicationJobSP = "BuildFetchCommunicationJobSP";
        internal const string FetchRuleTable = "FetchRule";
        internal const string ConfigCommunicationSettingSP = "ConfigCommunicationSetting";
        internal const string CommunicationSettingTable = "CommunicationSetting";
        internal const string FetchOrderConfigurationKey = "FetchOrder";

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

        public string CommunicationTableName => $"[{SchemaName}].[{HubName}_{CommunicationTable}]";
        public string FetchCommunicationJobSPName => $"[{SchemaName}].[{HubName}_{FetchCommunicationJobSP}]";
        public string UpdateCommunicationSPName => $"[{SchemaName}].[{HubName}_{UpdateCommunicationSP}]";
        public string BuildFetchCommunicationJobSPName => $"[{SchemaName}].[{HubName}_{BuildFetchCommunicationJobSP}]";
        public string FetchRuleTableName => $"[{SchemaName}].[{HubName}_{FetchRuleTable}]";
        public string ConfigCommunicationSettingSPName => $"[{SchemaName}].[{HubName}_{ConfigCommunicationSettingSP}]";
        public string CommunicationSettingTableName => $"[{SchemaName}].[{HubName}_{CommunicationSettingTable}]";
    }
}