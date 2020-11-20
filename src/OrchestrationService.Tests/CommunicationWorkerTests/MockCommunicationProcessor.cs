using maskx.OrchestrationService.Worker;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{
    public class MockCommunicationProcessor : ICommunicationProcessor<CustomCommunicationJob>
    {
        public string Name { get; set; } = "MockCommunicationProcessor";
        public int MaxBatchCount { get; set; } = 1;
        public CommunicationWorker<CustomCommunicationJob> CommunicationWorker { get; set; }
        public MockCommunicationProcessor(CommunicationWorker<CustomCommunicationJob> communicationWorker)
        {
            CommunicationWorker = communicationWorker;
        }
        public Task<CustomCommunicationJob[]> ProcessAsync(params CustomCommunicationJob[] jobs)
        {
            List<CustomCommunicationJob> rtv = new();
            foreach (var job in jobs)
            {
                job.ResponseCode = 200;
                job.ResponseContent = "MockCommunicationProcessor";
                job.Status = CommunicationJob.JobStatus.Completed;
                rtv.Add(job);
            }
            // update job in processor
            // worker.UpdateJobs(jobs);
            return Task.FromResult(rtv.ToArray());
        }
    }
}