using DurableTask.Core;
using maskx.OrchestrationService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.Orchestration
{
    public class CreateVirtualMachineOrchestration : TaskOrchestration<string, string>
    {
        public override async Task<string> RunTask(OrchestrationContext context, string input)
        {
            var t1 = await context.CreateSubOrchestrationInstance<bool>(typeof(PrepareVMTemplateAuthorizeOrchestration), input);
            if (!t1)
            {
                return "fail";
            }
            return "done";
        }
    }
}