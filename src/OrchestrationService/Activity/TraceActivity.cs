using DurableTask.Core;

namespace maskx.OrchestrationService.Activity
{
    public class TraceActivity : TaskActivity<TraceActivityInput, TaskResult>
    {
        private const string Source = "OrchestrationService-TraceActivity";

        protected override TaskResult Execute(TaskContext context, TraceActivityInput input)
        {
            TraceActivityEventSource.Log.TraceEvent(input.EventLevel, Source, context.OrchestrationInstance.InstanceId, context.OrchestrationInstance.ExecutionId, input.Message, input.Info, input.EventType);
            return new TaskResult() { Code = 200 };
        }
    }
}