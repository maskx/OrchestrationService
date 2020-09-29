using System.Collections.Generic;

namespace maskx.OrchestrationService.Worker
{
    public class Limitation
    {
        /// <summary>
        /// 并发请求的上限
        /// </summary>
        public int Concurrency { get; set; }
        // todo: 考虑 不做范围限制的情况
        /// <summary>
        /// 限制并发请求的范围，如Subscription、ManagementUnit
        /// </summary>
        public List<string> Scope { get; set; }

        private string group;

        public string Group
        {
            get
            {
                if (string.IsNullOrEmpty(group))
                {
                    if (Scope.Count > 0)
                        group = string.Join(",", Scope);
                }
                return group;
            }
        }

        public string On(int index)
        {
            if (Scope.Count == 0)
                return string.Empty;
            var s = new List<string>();
            foreach (var item in Scope)
            {
                s.Add($"T{index}.{item}=T.{item}");
            }
            return string.Join(" and ", s);
        }
    }
}