using DurableTask.Core;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace maskx.OrchestrationService.Activity
{
    public class CommunicationActivity : TaskActivity<(string name, string input), string>
    {
        public static string DbConnectionString { get; set; }

        protected override string Execute(TaskContext context, (string name, string input) e)
        {
            using (var conn = new SqlConnection(DbConnectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = $"insert into communication (InstanceId,ExecutionId,EventName,RequestUri,[Status]) values (@InstanceId,@ExecutionId,@EventName,@RequestUri,@Status)";
                cmd.Parameters.AddWithValue("InstanceId", context.OrchestrationInstance.InstanceId);
                cmd.Parameters.AddWithValue("ExecutionId", context.OrchestrationInstance.ExecutionId);
                cmd.Parameters.AddWithValue("EventName", e.name);
                cmd.Parameters.AddWithValue("RequestUri", e.name);
                cmd.Parameters.AddWithValue("Status", "Pending");
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return "OK";
        }
    }
}