using System.Collections.Generic;

namespace maskx.OrchestrationService.Activity
{
    public class AsyncRequestInput
    {
        public string EventName { get; set; }
        public string RequestTo { get; set; }
        public string RequestOperation { get; set; }
        public string RequsetContent { get; set; }
        public string RequestProperty { get; set; }
        public Dictionary<string, object> RuleField { get; set; }
        public string Processor { get; set; }
    }
}