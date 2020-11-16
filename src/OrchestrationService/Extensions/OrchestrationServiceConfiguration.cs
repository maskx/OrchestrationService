﻿using DurableTask.Core;
using maskx.OrchestrationService.Worker;
using System;

namespace maskx.OrchestrationService.Extensions
{
    public class OrchestrationServiceConfiguration
    {
        public IOrchestrationService OrchestrationService { get; set; }
        public IOrchestrationServiceClient OrchestrationServiceClient { get; set; }
        public Func<IServiceProvider, IOrchestrationCreatorFactory> GetOrchestrationCreatorFactory { get; set; }
        public Worker.OrchestrationWorkerOptions OrchestrationWorkerOptions { get; set; }
        public CommunicationWorkerOptions CommunicationWorkerOptions { get; set; }
    }
}