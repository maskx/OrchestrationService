using maskx.OrchestrationService.Worker;
using System;
using System.Collections.Generic;

namespace maskx.OrchestrationService.Extensions
{
    public class CommunicationWorkerOptions
    {
        /// <summary>
        /// Idel time when no job fetched
        /// </summary>
        public int IdelMilliseconds { get; set; } = 10000;

        /// <summary>
        /// 外部系统请求的最大并发数
        /// </summary>
        public int MaxConcurrencyRequest { get; set; } = 100;

        /// <summary>
        /// 获取Job的规则
        /// </summary>
        public Func<IServiceProvider, List<FetchRule>> GetFetchRules { get; set; }

        /// <summary>
        /// 扩展的规则筛选字段
        /// </summary>
        public List<string> RuleFields { get; set; } = new List<string>();
    }
}