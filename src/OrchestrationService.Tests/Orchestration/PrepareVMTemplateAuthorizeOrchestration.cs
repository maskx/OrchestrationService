using DurableTask.Core;
using maskx.OrchestrationService.Activity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.Orchestration
{
    public class PrepareVMTemplateAuthorizeOrchestration : TaskOrchestration<string, string>
    {
        private Dictionary<string, TaskCompletionSource<string>> waitHandlers = new Dictionary<string, TaskCompletionSource<string>>();

        public override async Task<string> RunTask(OrchestrationContext context, string input)
        {
            var name = this.waitHandlers.Count.ToString();
            waitHandlers.Add(name, new TaskCompletionSource<string>());
            await context.ScheduleTask<string>(typeof(CommunicationActivity), (name, input));
            await waitHandlers[name].Task;

            return "done";
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.waitHandlers.TryGetValue(name, out TaskCompletionSource<string> t))
            {
                t.SetResult(input);
            }
        }
    }
}