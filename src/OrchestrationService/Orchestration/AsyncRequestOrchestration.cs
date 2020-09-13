using DurableTask.Core;
using maskx.OrchestrationService.Activity;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Orchestration
{
    public class AsyncRequestOrchestration : TaskOrchestration<TaskResult, AsyncRequestInput,TaskResult,string>
    {
        private const string eventName = "AsyncRequestOrch";
        private TaskCompletionSource<TaskResult> waitHandler = null;

        public override async Task<TaskResult> RunTask(OrchestrationContext context, AsyncRequestInput input)
        {
            this.waitHandler = new TaskCompletionSource<TaskResult>();
            input.EventName = eventName;
            await context.ScheduleTask<TaskResult>(typeof(AsyncRequestActivity), input);
            await waitHandler.Task;
            return waitHandler.Task.Result;
        }

        public override void OnEvent(OrchestrationContext context, string name, TaskResult input)
        {
            if (this.waitHandler != null && name == eventName && this.waitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.waitHandler.SetResult(input);
            }
            base.OnEvent(context, name, input);
        }
    }
}