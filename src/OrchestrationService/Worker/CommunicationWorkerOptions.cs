﻿namespace maskx.OrchestrationService.Worker
{
    public class CommunicationWorkerOptions : Extensions.CommunicationWorkerOptions
    {
        internal const string CommunicationTable = "_Communication";
        internal const string FetchCommunicationJobSP = "_FetchCommunicationJob";
        internal const string UpdateCommunication = "_UpdateCommunication";

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
        public string UpdateCommunicationSPName=> $"[{SchemaName}].[{HubName}{UpdateCommunication}]";

    }
}