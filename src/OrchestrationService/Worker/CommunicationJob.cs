using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationService.Worker
{
    public class CommunicationJob
    {
        public string InstanceId { get; set; }
        public string ExecutionId { get; set; }
        public string EventName { get; set; }
        public string RequestUri { get; set; }
        public string RequestHeader { get; set; }
        public string RequestBody { get; set; }
        public string RequestMethod { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseContent { get; set; }
    }
}