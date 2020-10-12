using DurableTask.Core;
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