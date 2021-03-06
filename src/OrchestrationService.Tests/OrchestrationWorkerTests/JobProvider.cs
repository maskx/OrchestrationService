﻿using maskx.OrchestrationService.Worker;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.OrchestrationWorkerTests
{
    public class JobProvider : IJobProvider
    {
        public int Interval { get; set; } = 1000;
        public static List<Job> Jobs = new List<Job>();

        public Task<IList<Job>> FetchAsync(int top)
        {
            IList<Job> jobs = new List<Job>();
            int i = 0;
            for (; i < Jobs.Count; i++)
            {
                if (i > top)
                    break;
                jobs.Add(Jobs[i]);
            }
            Jobs.RemoveRange(0, i);
            return Task.FromResult(jobs);
        }

        public Task OrchestrationCompleted(OrchestrationCompletedArgs args)
        {
            return Task.CompletedTask;
        }
    }
}