using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Orchestration;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.Orchestration
{
    public class PrepareVMTemplateAuthorizeOrchestration : TaskOrchestration<bool, string>
    {
        public override async Task<bool> RunTask(OrchestrationContext context, string input)
        {
            string cloudSubscriptionId = string.Empty;

            var r = await context.CreateSubOrchestrationInstance<TaskResult>(typeof(AsyncRequestOrchestration), new AsyncRequestInput());
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
                    var r1 = await context.CreateSubOrchestrationInstance<TaskResult>(typeof(AsyncRequestOrchestration), new AsyncRequestInput());
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
    }
}