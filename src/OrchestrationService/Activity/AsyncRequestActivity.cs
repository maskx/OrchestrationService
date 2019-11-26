using DurableTask.Core;
using maskx.OrchestrationService.SQL;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Activity
{
    public class AsyncRequestActivity : TaskActivity<(string eventName, string requset), TaskResult>
    {
        private readonly AsyncRequestActivitySettings settings;
        private readonly string commandText;

        public AsyncRequestActivity(AsyncRequestActivitySettings settings)
        {
            this.settings = settings;
            this.commandText = string.Format(commandTemplate,
                string.Join("],[", settings.RuleFields),
                string.Join(",@", settings.RuleFields));
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, (string eventName, string requset) input)
        {
            Dictionary<string, object> pars = new Dictionary<string, object>();
            pars.Add("InstanceId", context.OrchestrationInstance.InstanceId);
            pars.Add("ExecutionId", context.OrchestrationInstance.ExecutionId);
            pars.Add("EventName", input.eventName);
            pars.Add("Status", "Pending");
            using (var db = new DbAccess(settings.ConnectionString))
            {
                db.AddStatement(this.commandText, pars);
                await db.ExecuteNonQueryAsync();
            }
            return new TaskResult() { Code = 200 };
        }

        protected override TaskResult Execute(TaskContext context, (string eventName, string requset) e)
        {
            return ExecuteAsync(context, e).Result;
        }

        private const string commandTemplate = @"
MERGE communication as TARGET
USING (VALUES (@InstanceId,@ExecutionId,@EventName)) AS SOURCE ([InstanceId],[ExecutionId],[EventName])
ON [Target].InstanceId = [Source].InstanceId AND [Target].ExecutionId = [Source].ExecutionId AND [Target].EventName = [Source].EventName
WHEN NOT MATCHED THEN
    INSERT
        ([InstanceId],[ExecutionId],[EventName],[Status],[{0}])
    values
        (@InstanceId,@ExecutionId,@EventName,@Status,@{1})
;";
    }
}