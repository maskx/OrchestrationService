using System.Collections.Generic;

namespace maskx.OrchestrationService.Worker
{
    public class CommunicationJob
    {
        public string InstanceId { get; set; }
        public string ExecutionId { get; set; }
        public string EventName { get; set; }
        public string Processor { get; set; }
        public string RequestTo { get; set; }
        public string RequestOperation { get; set; }
        public string RequsetContent { get; set; }
        public string RequestProperty { get; set; }
        public Dictionary<string, object> RuleField { get; set; }
        public int ResponseCode { get; set; }
        public string ResponseContent { get; set; }
        public string RequestId { get; set; }
    }
}