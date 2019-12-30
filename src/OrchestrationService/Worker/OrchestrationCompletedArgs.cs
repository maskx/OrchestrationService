using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationService.Worker
{
    public class OrchestrationCompletedArgs
    {
        public string InstanceId { get; set; }
        public string ExecutionId { get; set; }
        public bool Status { get; set; }
        public string Result { get; set; }

        public bool IsSubOrchestration
        {
            get
            {
                return InstanceId.Length > 32;
            }
        }

        public string ParentExecutionId
        {
            get
            {
                if (IsSubOrchestration)
                    return InstanceId.Substring(0, 32);
                else
                    return this.InstanceId;
            }
        }
    }
}