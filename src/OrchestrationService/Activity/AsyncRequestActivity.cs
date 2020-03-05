using DurableTask.Core;
using maskx.OrchestrationService.SQL;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Activity
{
    public class AsyncRequestActivity : TaskActivity<AsyncRequestInput, TaskResult>
    {
        private readonly CommunicationWorkerOptions options;
        private readonly string commandText;

        public AsyncRequestActivity(IOptions<CommunicationWorkerOptions> options)
        {
            this.options = options.Value;
            if (this.options.RuleFields.Count == 0)
            {
                this.commandText = string.Format(commandTemplate, "", "", this.options.CommunicationTableName);
            }
            else
            {
                this.commandText = string.Format(commandTemplate,
                $",[{string.Join("],[", this.options.RuleFields)}]",
                ",@" + string.Join(",@", this.options.RuleFields),
                this.options.CommunicationTableName);
            }
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, AsyncRequestInput input)
        {
            Dictionary<string, object> pars = new Dictionary<string, object>();
            pars.Add("InstanceId", context.OrchestrationInstance.InstanceId);
            pars.Add("ExecutionId", context.OrchestrationInstance.ExecutionId);
            pars.Add("EventName", input.EventName);
            pars.Add("Status", "Pending");
            pars.Add("RequestTo", input.RequestTo);
            pars.Add("RequestOperation", input.RequestOperation);
            pars.Add("RequsetContent", input.RequsetContent);
            pars.Add("RequestProperty", input.RequestProperty);
            pars.Add("Processor", input.Processor);
            if (input.RuleField != null)
            {
                foreach (var item in input.RuleField)
                {
                    pars.Add(item.Key, item.Value);
                }
            }

            using (var db = new DbAccess(this.options.ConnectionString))
            {
                db.AddStatement(this.commandText, pars);
                await db.ExecuteNonQueryAsync();
            }
            return new TaskResult() { Code = 200 };
        }

        protected override TaskResult Execute(TaskContext context, AsyncRequestInput input)
        {
            return ExecuteAsync(context, input).Result;
        }

        //{0} rule columns
        //{1} rule value
        //{2} Communication table name
        private const string commandTemplate = @"
MERGE {2} with (serializable) as TARGET
USING (VALUES (@InstanceId,@ExecutionId,@EventName)) AS SOURCE ([InstanceId],[ExecutionId],[EventName])
ON [Target].InstanceId = [Source].InstanceId AND [Target].ExecutionId = [Source].ExecutionId AND [Target].EventName = [Source].EventName
WHEN NOT MATCHED THEN
    INSERT
        ([RequestId],[NextFetchTime],[CreateTime],[InstanceId],[ExecutionId],[EventName],[Status],[RequestTo],[RequestOperation],[RequsetContent],[RequestProperty],[Processor]{0})
    values
        (newid(),getutcdate(),getutcdate(),@InstanceId,@ExecutionId,@EventName,@Status,@RequestTo,@RequestOperation,@RequsetContent,@RequestProperty,@Processor {1})
;";
    }
}