using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Worker
{
    public interface IJobProvider
    {
        /// <summary>
        /// the Interval between FetchAsync,Milliseconds
        /// </summary>
        int Interval { get; set; }

        Task<IList<Job>> FetchAsync(int top);
    }
}