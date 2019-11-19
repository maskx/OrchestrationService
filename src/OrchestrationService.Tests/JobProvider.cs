using maskx.OrchestrationService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OrchestrationService.Tests
{
    public class JobProvider : IJobProvider
    {
        public int Interval { get; set; } = 1000;

        public Task<IList<Job>> FetchAsync(int top)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Job orchestration)
        {
            throw new NotImplementedException();
        }
    }
}