using DurableTask.Core;
using Microsoft.Extensions.Configuration;
using OrchestrationService.Tests.Extensions;
using System.Data.SqlClient;
using System.Net.Http;

namespace OrchestrationService.Tests.Activity
{
    public class AsyncRequestActivity : TaskActivity<(string eventName, string requset), string>
    {
        protected override string Execute(TaskContext context, (string eventName, string requset) e)
        {
            using (var conn = new SqlConnection(context.GetConnectionString()))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = fetchCommand;
                cmd.Parameters.AddWithValue("InstanceId", context.OrchestrationInstance.InstanceId);
                cmd.Parameters.AddWithValue("ExecutionId", context.OrchestrationInstance.ExecutionId);
                cmd.Parameters.AddWithValue("EventName", e.eventName);
                cmd.Parameters.AddWithValue("RequestMethod", "POST");
                cmd.Parameters.AddWithValue("Status", "Pending");
                cmd.Parameters.AddWithValue("ServiceType", "VirtualMachine");
                cmd.Parameters.AddWithValue("SubscriptionId", "123");
                cmd.Parameters.AddWithValue("ManagementUnit", "MmmM");
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return "OK";
        }

        private const string fetchCommand = @"
MERGE communication as TARGET
USING (VALUES (@InstanceId,@ExecutionId,@EventName)) AS SOURCE ([InstanceId],[ExecutionId],[EventName])
ON [Target].InstanceId = [Source].InstanceId AND [Target].ExecutionId = [Source].ExecutionId AND [Target].EventName = [Source].EventName
WHEN NOT MATCHED THEN
    INSERT
        (InstanceId,ExecutionId,EventName,[RequestMethod],[Status],[ServiceType],[SubscriptionId],[ManagementUnit])
    values
        (@InstanceId,@ExecutionId,@EventName,@RequestMethod,@Status,@ServiceType,@SubscriptionId,@ManagementUnit)
;";
    }
}