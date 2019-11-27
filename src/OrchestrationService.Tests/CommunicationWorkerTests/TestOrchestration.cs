using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Orchestration;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{
    public class TestOrchestration : TaskOrchestration<TaskResult, string>
    {
        public override async Task<TaskResult> RunTask(OrchestrationContext context, string input)
        {
            var response = await context.CreateSubOrchestrationInstance<TaskResult>(
                  typeof(AsyncRequestOrchestration),
                 DataConverter.Deserialize<AsyncRequestInput>(input));
            return response;
        }
    }
}