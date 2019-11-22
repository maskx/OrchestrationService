using System;
using System.Collections.Generic;
using System.Text;

namespace OrchestrationService.Tests.Worker
{
    public class FetchRule
    {
        /// <summary>
        /// 并发请求的上限
        /// </summary>
        public int ConcurrencyCount { get; set; }

        /// <summary>
        /// 需要限制并发请求的内容，如ServicType,RequestMethod，Operation
        /// </summary>
        public Dictionary<string, string> What { get; private set; }

        /// <summary>
        /// 限制并发请求的范围，如Subscription、ManagementUnit
        /// </summary>
        public List<string> Scope { get; private set; }

        private string where;

        public string Where
        {
            get
            {
                if (string.IsNullOrEmpty(where))
                {
                    List<string> s = new List<string>();
                    foreach (var item in What)
                    {
                        s.Add($"{item.Key}=N'{item.Value}'");
                    }
                    if (s.Count > 0)
                        where = string.Join(" And ", s);
                }
                return where;
            }
        }

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

        private string on;

        public string On
        {
            get
            {
                if (string.IsNullOrEmpty(on))
                {
                    var s = new List<string>();
                    foreach (var item in Scope)
                    {
                        s.Add($"T1.{item}=T.{item}");
                    }
                    if (Scope.Count > 0)
                        on = string.Join(" and ", s);
                }
                return on;
            }
        }

        public FetchRule()
        {
            What = new Dictionary<string, string>();
            Scope = new List<string>();
        }
    }
}