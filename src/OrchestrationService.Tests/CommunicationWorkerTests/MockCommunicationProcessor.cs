using maskx.OrchestrationService.Worker;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{
    public class MockCommunicationProcessor<T> : ICommunicationProcessor<T> where T:CommunicationJob,new()
    {
        public string Name { get; set; } = "MockCommunicationProcessor";
        public int MaxBatchCount { get; set; } = 1;
        public CommunicationWorker<T> CommunicationWorker { get; set; }

        public Task<T[]> ProcessAsync(params T[] jobs)
        {
            List<T> rtv = new();
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