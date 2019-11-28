using DurableTask.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.OrchestrationWorkerTests
{
    public class TestOrchestration : TaskOrchestration<int, string>
    {
        public override Task<int> RunTask(OrchestrationContext context, string input)
        {
            return Task.FromResult(1);
        }
    }
}