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
    }
}