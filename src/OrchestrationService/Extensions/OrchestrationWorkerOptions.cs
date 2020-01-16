using System;
using System.Collections.Generic;

namespace maskx.OrchestrationService.Extensions
{
    public class OrchestrationWorkerOptions
    {
        public int FetchJobCount { get; set; } = 100;
        public Func<IList<Type>> GetBuildInTaskActivities { get; set; }
        public Func<IList<Type>> GetBuildInOrchestrators { get; set; }
        public Func<IDictionary<Type, object>> GetBuildInTaskActivitiesFromInterface { get; set; }
    }
}