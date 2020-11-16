using System;
using System.Collections.Generic;

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


        /// <summary>
        /// 扩展的规则筛选字段
        /// </summary>
        public Dictionary<string, Type> RuleFields { get; set; } = new Dictionary<string, Type>();

        private const string CommunicationTable = "_Communication";
        private const string FetchCommunicationJobSP = "_FetchCommunicationJob";
        private const string UpdateCommunicationSP = "_UpdateCommunication";
        private const string BuildFetchCommunicationJobSP = "_BuildFetchCommunicationJobSP";
        private const string FetchRuleTable = "_FetchRule";
        private const string FetchRuleLimitationTable = "_FetchRuleLimitation";
        private const string ConfigCommunicationSettingSP = "_ConfigCommunicationSetting";
        private const string CommunicationSettingTable = "_CommunicationSetting";
        public const string FetchOrderConfigurationKey = "FetchOrder";

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
        public string UpdateCommunicationSPName => $"[{SchemaName}].[{HubName}{UpdateCommunicationSP}]";
        public string BuildFetchCommunicationJobSPName => $"[{SchemaName}].[{HubName}{BuildFetchCommunicationJobSP}]";
        public string FetchRuleTableName => $"[{SchemaName}].[{HubName}{FetchRuleTable}]";
        public string FetchRuleLimitationTableName => $"[{SchemaName}].[{HubName}{FetchRuleLimitationTable}]";
        public string ConfigCommunicationSettingSPName => $"[{SchemaName}].[{HubName}{ConfigCommunicationSettingSP}]";
        public string CommunicationSettingTableName => $"[{SchemaName}].[{HubName}{CommunicationSettingTable}]";
    }
}