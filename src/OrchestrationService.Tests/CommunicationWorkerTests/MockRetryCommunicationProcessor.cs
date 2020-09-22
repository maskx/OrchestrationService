using maskx.OrchestrationService.Worker;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{
    public class MockRetryCommunicationProcessor : ICommunicationProcessor
    {
        public string Name { get; set; } = "MockRetryCommunicationProcessor";
        public int MaxBatchCount { get; set; } = 1;
        public CommunicationWorker CommunicationWorker { get; set; }

        public Task<CommunicationJob[]> ProcessAsync(params CommunicationJob[] jobs)
        {
            List<CommunicationJob> rtv = new List<CommunicationJob>();
            foreach (var job in jobs)
            {
                job.ResponseCode = 200;

                if (job.ResponseContent == "Retry")
                {
                    job.ResponseContent = "Retry->Completed";
                    job.Status = CommunicationJob.JobStatus.Completed;
                }
                else
                {
                    job.ResponseContent = "Retry";
                    job.Status = CommunicationJob.JobStatus.Pending;
                }

                rtv.Add(job);
            }
            return Task.FromResult(rtv.ToArray());
        }
    }
}