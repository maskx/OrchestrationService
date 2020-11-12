using DurableTask.Core;
using maskx.OrchestrationService.SQL;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Activity
{
    public class AsyncRequestActivity<T> : AsyncTaskActivity<T, TaskResult> where T : CommunicationJob, new()
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
                $",[{string.Join("],[", this.options.RuleFields.Keys)}]",
                ",@" + string.Join(",@", this.options.RuleFields.Keys),
                this.options.CommunicationTableName);
            }
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, T input)
        {
            await SaveRequest(input);
            return new TaskResult() { Code = 200 };
        }

        public async Task SaveRequest(T input)
        {
            using var db = new SQLServerAccess(this.options.ConnectionString);
            db.AddStatement(this.commandText, input);
            await db.ExecuteNonQueryAsync();
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
        ([RequestId],[LockedUntilUtc],[CreateTime],[InstanceId],[ExecutionId],[EventName],[Status],[RequestTo],[RequestOperation],[RequestContent],[RequestProperty],[Processor]{0})
    values
        (newid(),getutcdate(),getutcdate(),@InstanceId,@ExecutionId,@EventName,@Status,@RequestTo,@RequestOperation,@RequestContent,@RequestProperty,@Processor {1})
;";
    }
}