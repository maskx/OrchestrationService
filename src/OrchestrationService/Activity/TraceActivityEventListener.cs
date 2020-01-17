using DurableTask.Core.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace maskx.OrchestrationService.Activity
{
    public class TraceActivityEventListener : EventListener
    {
        public TraceActivityEventListener()
        {
            this.EnableEvents(DefaultEventSource.Log, EventLevel.Informational);
        }
    }
}