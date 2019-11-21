﻿using DurableTask.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationService.Worker
{
    public class OrchestrationWorkerOptions
    {
        public int FetchJobCount { get; set; } = 100;
        public Func<IList<Type>> GetBuildInTaskActivities { get; set; }
        public Func<IList<Type>> GetBuildInOrchestrators { get; set; }
        public Func<Orchestration, ObjectCreator<TaskOrchestration>> GetOrchestrationCreator { get; set; }
    }
}