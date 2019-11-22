using DurableTask.Core;
using Newtonsoft.Json.Linq;
using OrchestrationService.Tests.Activity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.Orchestration
{
    public class AsyncRequestOrchestration : TaskOrchestration<string, string>
    {
        private const string eventName = "AsyncRequestOrch";
        private TaskCompletionSource<string> waitHandler = null;

        public override async Task<string> RunTask(OrchestrationContext context, string input)
        {
            this.waitHandler = new TaskCompletionSource<string>();
            await context.ScheduleTask<string>(typeof(AsyncRequestActivity), (eventName, input));
            await waitHandler.Task;
            return waitHandler.Task.Result;
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.waitHandler != null && name == eventName)
            {
                this.waitHandler.SetResult(input);
            }
            base.OnEvent(context, name, input);
        }
    }
}