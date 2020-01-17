﻿using System.Diagnostics;

namespace maskx.OrchestrationService.Activity
{
    public class TraceActivityInput
    {
        public TraceEventType EventLevel { get; set; }
        public string EventType { get; set; }
        public string Format { get; set; }
        public object[] Args { get; set; }
    }
}