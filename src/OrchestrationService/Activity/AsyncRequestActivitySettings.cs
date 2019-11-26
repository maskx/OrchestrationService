using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationService.Activity
{
    public class AsyncRequestActivitySettings
    {
        internal const string CommunicationTable = "_Communication";
        public string ConnectionString { get; set; }
        public List<FetchRule> FetchRules { get; set; }
        public List<string> RuleFields { get; set; }

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