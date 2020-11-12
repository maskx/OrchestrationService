using DurableTask.Core;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Worker;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Orchestration
{
    public class AsyncRequestOrchestration<T> : TaskOrchestration<TaskResult, T,TaskResult,string> where T:CommunicationJob,new()
    {
        private const string eventName = "AsyncRequestOrch";
        private TaskCompletionSource<TaskResult> waitHandler = null;

        public override async Task<TaskResult> RunTask(OrchestrationContext context, T input)
        {
            this.waitHandler = new TaskCompletionSource<TaskResult>();
            input.EventName = eventName;
            input.InstanceId = context.OrchestrationInstance.InstanceId;
            input.ExecutionId = context.OrchestrationInstance.ExecutionId;
            await context.ScheduleTask<TaskResult>(typeof(AsyncRequestActivity<T>), input);
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