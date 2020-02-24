using System;
using System.Diagnostics.Tracing;

namespace maskx.OrchestrationService.Activity
{
    public class TraceActivityEventListener : EventListener
    {
        public Action<OrchestrationTrackingArgs> OnTracing { get; set; }

        public TraceActivityEventListener(Action<OrchestrationTrackingArgs> onTracing)
        {
            this.OnTracing = onTracing;
            this.EnableEvents(TraceActivityEventSource.Log, EventLevel.Informational);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            var payload = eventData.Payload;
            this.OnTracing(new OrchestrationTrackingArgs()
            {
                EventLevel = eventData.Level,
                Source = payload[0]?.ToString(),
                InstanceId = payload[1]?.ToString(),
                ExecutionId = payload[2]?.ToString(),
                // TimeStamp = eventData.TimeStamp,
                Message = payload[3]?.ToString(),
                Info = payload[4]?.ToString(),
                EventType = payload[5]?.ToString()
            });
        }
    }
}