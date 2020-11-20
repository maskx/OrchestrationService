using maskx.OrchestrationService.Worker;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{
    public class MockRetryCommunicationProcessor : ICommunicationProcessor<CustomCommunicationJob>
    {
        public string Name { get; set; } = "MockRetryCommunicationProcessor";
        public int MaxBatchCount { get; set; } = 1;
        public CommunicationWorker<CustomCommunicationJob> CommunicationWorker { get; set; }
        public MockRetryCommunicationProcessor(CommunicationWorker<CustomCommunicationJob> communicationWorker)
        {
            CommunicationWorker = communicationWorker;
        }
        public Task<CustomCommunicationJob[]> ProcessAsync(params CustomCommunicationJob[] jobs)
        {
            List<CustomCommunicationJob> rtv = new();
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
                    job.NextTryAfterSecond = 1;
                }

                rtv.Add(job);
            }
            return Task.FromResult(rtv.ToArray());
        }
    }
}