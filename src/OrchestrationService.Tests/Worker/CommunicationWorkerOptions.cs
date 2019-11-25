using System;
using System.Collections.Generic;
using System.Text;

namespace OrchestrationService.Tests.Worker
{
    public class CommunicationWorkerOptions
    {
        public string ConnectionString { get; set; }
        public int Concurrency { get; set; }
    }
}