using maskx.OrchestrationService.Worker;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrchestrationService.Tests
{
    public class JobProvider : IJobProvider
    {
        public int Interval { get; set; } = 1000;
        public static List<Job> Jobs = new List<Job>();

        public async Task<IList<Job>> FetchAsync(int top)
        {
            List<Job> jobs = new List<Job>();
            int i = 0;
            for (; i < Jobs.Count; i++)
            {
                if (i > top)
                    break;
                jobs.Add(Jobs[i]);
            }
            Jobs.RemoveRange(0, i);
            return jobs;
        }

        public Task UpdateAsync(Job orchestration)
        {
            throw new NotImplementedException();
        }
    }
}