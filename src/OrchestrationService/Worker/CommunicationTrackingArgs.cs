using System;
using System.Diagnostics.Tracing;

namespace maskx.OrchestrationService.Worker
{
    public class CommunicationTrackingArgs
    {
        public string Source { get; set; }
        public EventLevel EventLevel { get; set; }
        public string EventType { get; set; }
        public string Message { get; set; }
        public string Info { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}