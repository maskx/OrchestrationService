using DurableTask.Core;
using maskx.OrchestrationService.Extensions;
using maskx.OrchestrationService.SQL;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
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
            List<string> cols = new List<string>();
            List<string> pars = new List<string>();
            foreach (var p in Utilities.Utility.GetPropertyInfos(typeof(T)).Values)
            {
                if (p.GetCustomAttribute<NotMappedAttribute>() != null)
                    continue;
                string n = p.GetColumnName();
                cols.Add($"[{n}]");
                // todo: try to find a way support set default value in class define
                switch (n.ToLowerInvariant())
                {
                    case "createdtime":
                    case "lockeduntilutc":
                        pars.Add("getutcdate()");
                        break;
                    case "requestid":
                        pars.Add("newid()");
                        break;
                    default:
                        pars.Add($"@{n}");
                        break;
                }
            }
            this.commandText = string.Format(commandTemplate, string.Join(",", cols), string.Join(",", pars), this.options.CommunicationTableName);
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

        private const string commandTemplate = @"
MERGE {2} with (serializable) as TARGET
USING (VALUES (@InstanceId,@ExecutionId,@EventName)) AS SOURCE ([InstanceId],[ExecutionId],[EventName])
ON [Target].InstanceId = [Source].InstanceId AND [Target].ExecutionId = [Source].ExecutionId AND [Target].EventName = [Source].EventName
WHEN NOT MATCHED THEN INSERT ({0}) values ({1})
;";
    }
}