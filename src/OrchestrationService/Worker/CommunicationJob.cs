﻿using System;
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
        public string RequestContent { get; set; }
        public string RequestProperty { get; set; }
        public int ResponseCode { get; set; }
        public string ResponseContent { get; set; }
        public string RequestId { get; set; }
        public string Context { get; set; }
        public JobStatus Status { get; set; }
        public DateTime CreateTime { get; set; }

        public DateTime LockedUntilUtc { get;internal set; }

        public enum JobStatus
        {
            /// <summary>
            /// Pending to send request
            /// </summary>
            Pending = 0,

            /// <summary>
            /// start to send request and wait the response
            /// </summary>
            Locked = 1,

            /// <summary>
            /// had got finally response
            /// </summary>
            Completed = 2,
        }
    }
}