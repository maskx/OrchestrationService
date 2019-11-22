using DurableTask.Core;
using maskx.OrchestrationService;
using Newtonsoft.Json.Linq;
using OrchestrationService.Tests.Activity;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.Orchestration
{
    public class PrepareVMTemplateAuthorizeOrchestration : TaskOrchestration<bool, string>
    {
        private const string queryEventName = "Query";
        private TaskCompletionSource<string> queryWaitHandler = null;
        private const string createEventName = "Create";
        private TaskCompletionSource<string> createWaitHandle = null;

        public override async Task<bool> RunTask(OrchestrationContext context, string input)
        {
            string cloudSubscriptionId = string.Empty;

            //queryWaitHandler = new TaskCompletionSource<string>();
            //await context.ScheduleTask<string>(typeof(AsyncRequestActivity), (queryEventName, input));
            //await queryWaitHandler.Task;
            //var r = DataConverter.Deserialize<TaskResult>(queryWaitHandler.Task.Result);
            var s = await context.CreateSubOrchestrationInstance<string>(typeof(AsyncRequestOrchestration), input);
            var r = DataConverter.Deserialize<TaskResult>(s);
            if (r.Code == 200)
            {
                JArray grantedToList = null;// JObject.Parse(r.Content)["GrantedToList"] as JArray;
                if (grantedToList == null)
                    grantedToList = new JArray();
                if (IsGranted(grantedToList, cloudSubscriptionId))
                {
                    return true;
                }
                else
                {
                    createWaitHandle = new TaskCompletionSource<string>();
                    await context.ScheduleTask<string>(typeof(AsyncRequestActivity), (createEventName, input));
                    await createWaitHandle.Task;
                    var r1 = DataConverter.Deserialize<TaskResult>(createWaitHandle.Task.Result);
                    if (r1.Code == 200)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
        }

        private bool IsGranted(JArray grantedToList, string cloudSubscriptionId)
        {
            return false;
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.queryWaitHandler != null && name == queryEventName)
            {
                this.queryWaitHandler.SetResult(input);
            }
            else if (this.createWaitHandle != null && name == createEventName)
            {
                this.createWaitHandle.SetResult(input);
            }
        }
    }
}