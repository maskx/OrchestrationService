using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationService
{
    public class Job
    {
        public string InstanceId { get; set; }
        public string Input { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public Orchestration Orchestration { get; set; }
        public RuntimeStatus RuntimeStatus { get; set; }
    }

    public class RuntimeStatus
    {
        public string InstanceId { get; set; }
        public string ExecutionId { get; set; }
        public string Status { get; set; }
    }
}