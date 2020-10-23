using System;

namespace maskx.OrchestrationService.Worker
{
    public class Job
    {
        public string InstanceId { get; set; }
        public object Input { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public OrchestrationSetting Orchestration { get; set; }
        public RuntimeStatus RuntimeStatus { get; set; }
    }

    public class RuntimeStatus
    {
        public string InstanceId { get; set; }
        public string ExecutionId { get; set; }
        public string Status { get; set; }
    }
}