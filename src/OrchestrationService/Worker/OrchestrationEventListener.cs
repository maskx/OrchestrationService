using DurableTask.Core.Tracing;
using System.Diagnostics.Tracing;

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
                            InstanceId = eventData.Payload[1].ToString(),
                            ExecutionId = eventData.Payload[2].ToString(),
                            Status = eventData.Level == EventLevel.Informational ? true : false,
                            Result = msg.Substring(msg.IndexOf("result:") + 8)
                        };
                        if (this.worker.jobProvider != null && !args.IsSubOrchestration)
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