using maskx.OrchestrationService.Worker;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{
    public class MockCommunicationProcessor : ICommunicationProcessor
    {
        public string Name { get; set; } = "MockCommunicationProcessor";
        public int MaxBatchCount { get; set; } = 1;
        public CommunicationWorker CommunicationWorker { get; set; }

        public Task<CommunicationJob[]> ProcessAsync(params CommunicationJob[] jobs)
        {
            List<CommunicationJob> rtv = new List<CommunicationJob>();
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