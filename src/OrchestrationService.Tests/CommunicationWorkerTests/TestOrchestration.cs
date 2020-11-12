using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Orchestration;
using maskx.OrchestrationService.Worker;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{
    public class TestOrchestration<T> : TaskOrchestration<TaskResult, string> where T : CommunicationJob, new()
    {
        public override async Task<TaskResult> RunTask(OrchestrationContext context, string input)
        {
            var response = await context.CreateSubOrchestrationInstance<TaskResult>(
                  typeof(AsyncRequestOrchestration<T>),
                 DataConverter.Deserialize<T>(input));
            return response;
        }
    }
    public class TestOrchestration : TestOrchestration<CommunicationJob>
    {

    }
}