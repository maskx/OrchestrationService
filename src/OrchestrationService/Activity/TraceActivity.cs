using DurableTask.Core;
using DurableTask.Core.Tracing;

namespace maskx.OrchestrationService.Activity
{
    public class TraceActivity : TaskActivity<TraceActivityInput, TaskResult>
    {
        protected override TaskResult Execute(TaskContext context, TraceActivityInput input)
        {
            TraceHelper.TraceInstance(input.EventLevel, input.EventType, context.OrchestrationInstance, input.Format, input.Args);
            return new TaskResult() { Code = 200 };
        }
    }
}