using System;
using System.Collections.Generic;

namespace maskx.OrchestrationService.Worker
{
    public class OrchestrationWorkerOptions
    {
        /// <summary>
        /// Auto-creates the necessary resources for the orchestration service and the instance store
        /// </summary>
        public bool AutoCreate { get; set; } = false;
        public int FetchJobCount { get; set; } = 100;

        /// <summary>
        /// indicate whether include the exception details in task events
        /// </summary>
        public bool IncludeDetails { get; set; } = false;

        public Func<IServiceProvider, IList<(string Name, string Version, Type Type)>> GetBuildInTaskActivities { get; set; }
        public Func<IServiceProvider, IList<(string Name, string Version, Type Type)>> GetBuildInOrchestrators { get; set; }
        public Func<IServiceProvider, IDictionary<Type, (string Version, object Instance)>> GetBuildInTaskActivitiesFromInterface { get; set; }
    }
}