using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace maskx.OrchestrationService.Activity
{
    public class OrchestrationTrackingArgs
    {
        public string Source { get; set; }
        public string InstanceId { get; set; }
        public string ExecutionId { get; set; }
        public EventLevel EventLevel { get; set; }
        public string EventType { get; set; }
        public string Message { get; set; }
        public string Info { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}