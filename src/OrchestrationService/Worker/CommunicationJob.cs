using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace maskx.OrchestrationService.Worker
{
    [Table("Communication")]
    public class CommunicationJob
    {
        [Required()]
        [Key]
        [MaxLength(50)]
        public string InstanceId { get; set; }
        [Required()]
        [MaxLength(50)]
        [Key]
        public string ExecutionId { get; set; }
        [Required()]
        [MaxLength(50)]
        [Key]
        public string EventName { get; set; }
        [MaxLength(50)]
        public string Processor { get; set; }
        [MaxLength(50)]
        public string RequestTo { get; set; }
        [MaxLength(50)]
        public string RequestOperation { get; set; }
        public string RequestContent { get; set; }
        public string RequestProperty { get; set; }
        public int ResponseCode { get; set; }
        public string ResponseContent { get; set; }
        [MaxLength(50)]
        public string RequestId { get; set; }
        public string Context { get; set; }
        public JobStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LockedUntilUtc { get; internal set; }
        [NotMapped]
        public double? NextTryAfterSecond { get; set; }
        public DateTime CompletedTime { get; set; }
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