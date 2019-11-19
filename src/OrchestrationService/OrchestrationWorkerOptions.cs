using DurableTask.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationService
{
    public class OrchestrationWorkerOptions
    {
        public int FetchJobCount { get; set; } = 100;
        public Func<IList<Type>> GetBuildInTaskActivities { get; set; }
        public Func<IList<Type>> GetBuildInOrchestrators { get; set; }
    }
}