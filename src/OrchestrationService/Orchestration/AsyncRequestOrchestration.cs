using DurableTask.Core;
using maskx.OrchestrationService.Activity;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Orchestration
{
    public class AsyncRequestOrchestration : TaskOrchestration<TaskResult, AsyncRequestInput>
    {
        private const string eventName = "AsyncRequestOrch";
        private TaskCompletionSource<string> waitHandler = null;

        public override async Task<TaskResult> RunTask(OrchestrationContext context, AsyncRequestInput input)
        {
            this.waitHandler = new TaskCompletionSource<string>();
            input.EventName = eventName;
            await context.ScheduleTask<TaskResult>(typeof(AsyncRequestActivity), input);
            await waitHandler.Task;
            return DataConverter.Deserialize<TaskResult>(waitHandler.Task.Result);
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.waitHandler != null && name == eventName && this.waitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.waitHandler.SetResult(input);
            }
            base.OnEvent(context, name, input);
        }
    }
}