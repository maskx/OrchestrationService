using DurableTask.Core;
using maskx.OrchestrationService.Worker;
using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationService.OrchestrationCreator
{
    public class BPMNCreator : ObjectCreator<TaskOrchestration>
    {
        private Type prototype;
        private readonly maskx.OrchestrationService.Worker.Orchestration orchestration;
        private readonly object thisLock = new object();

        public BPMNCreator(maskx.OrchestrationService.Worker.Orchestration orchestration)
        {
            this.orchestration = orchestration;
        }

        public override TaskOrchestration Create()
        {
            if (this.prototype == null)
            {
                lock (this.thisLock)
                {
                    if (this.prototype == null)
                    {
                        this.prototype = GetOrchestrationType();
                    }
                }
            }
            return (TaskOrchestration)Activator.CreateInstance(this.prototype);
        }

        private Type GetOrchestrationType()
        {
            return typeof(int);
        }
    }
}