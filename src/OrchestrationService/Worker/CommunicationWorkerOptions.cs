using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationService.Worker
{
    public class CommunicationWorkerOptions
    {
        internal const string CommunicationTable = "_Communication";

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 外部系统请求的最大并发数
        /// </summary>
        public int MaxConcurrencyRequest { get; set; } = 100;

        /// <summary>
        /// 获取Job的规则
        /// </summary>
        public Func<List<FetchRule>> GetFetchRules { get; set; }

        /// <summary>
        /// 扩展的规则筛选字段
        /// </summary>
        public List<string> RuleFields { get; set; } = new List<string>();

        /// Gets or sets the hub name for the database instance store.
        /// </summary>
        public string HubName { get; set; }

        /// <summary>
        /// Gets or sets the schema name to which the tables will be added.
        /// </summary>
        public string SchemaName { get; set; } = "dbo";

        public string CommunicationTableName => $"[{SchemaName}].[{HubName}{CommunicationTable}]";
    }
}