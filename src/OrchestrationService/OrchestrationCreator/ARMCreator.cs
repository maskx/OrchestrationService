using DurableTask.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationService.OrchestrationCreator
{
    public class ARMCreator : ObjectCreator<TaskOrchestration>
    {
        private Type prototype;
        private readonly Orchestration orchestration;
        private readonly object thisLock = new object();

        public ARMCreator(Orchestration orchestration)
        {
            this.orchestration = orchestration;
            if (string.IsNullOrEmpty(orchestration.Name))
                this.Name = Guid.NewGuid().ToString("N");
            else
                this.Name = orchestration.Name;
            this.Version = orchestration.Version;
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