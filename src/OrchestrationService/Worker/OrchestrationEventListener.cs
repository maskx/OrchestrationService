using DurableTask.Core.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace maskx.OrchestrationService.Worker
{
    public class OrchestrationEventListener : EventListener
    {
        private OrchestrationWorker worker = null;

        public OrchestrationEventListener(OrchestrationWorker worker)
        {
            this.worker = worker;
            this.EnableEvents(DefaultEventSource.Log, EventLevel.LogAlways);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (this.worker.jobProvider == null && this.worker.OrchestrationCompletedActions.Count == 0)
                return;
            if (eventData.Level == EventLevel.Informational || eventData.Level == EventLevel.Warning)
            {
                if (null != eventData.Payload[6] && eventData.Payload[6].ToString() == "TaskOrchestrationDispatcher-InstanceCompleted")
                {
                    if (eventData.Payload[4] != null && eventData.Payload[4] is string msg)
                    {
                        var args = new OrchestrationCompletedArgs()
                        {
                            InstanceId = msg.Substring(26, 32),
                            ExecutionId = msg.Substring(73, 32),
                            Status = eventData.Level == EventLevel.Informational ? true : false,
                            Result = msg.Substring(150)
                        };
                        if (this.worker.jobProvider != null)
                        {
                            this.worker.jobProvider.OrchestrationCompleted(args);
                        }
                        foreach (var action in this.worker.OrchestrationCompletedActions)
                        {
                            action(args);
                        }
                    }
                }
            }
        }
    }
}