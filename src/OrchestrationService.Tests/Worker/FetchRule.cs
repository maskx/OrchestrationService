using System;
using System.Collections.Generic;
using System.Text;

namespace OrchestrationService.Tests.Worker
{
    public class FetchRule
    {
        public int ConcurrencyCount { get; set; }
        public string ServiceType { get; set; }
        public string Method { get; set; }
        public string Operation { get; set; }
        public bool InOneSubscription { get; set; } = false;
        public bool InOneVnet { get; set; } = false;
        public bool InOneMU { get; set; } = false;
    }
}