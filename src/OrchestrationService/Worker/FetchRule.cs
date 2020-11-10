using maskx.OrchestrationService.Utilities;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.OrchestrationService.Worker
{
    public class FetchRule
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedTimeUtc { get; set; }
        public DateTime UpdatedTimeUtc { get; set; }
        /// <summary>
        /// 需要限制并发请求的内容，如ServicType,RequestMethod，Operation
        /// </summary>
        public List<Where> What { get; set; } = new List<Where>();

        /// <summary>
        /// 并发请求的上限
        /// </summary>
        public int Concurrency { get; set; }

        /// <summary>
        /// 限制并发请求的范围，如Subscription、ManagementUnit
        /// </summary>
        public List<string> Scope { get; set; } = new List<string>();

        /// <summary>
        /// todo: 支持设置优先级，如不设置则使用默认优先级
        /// </summary>
        public List<FetchOrder> FetchOrder { get; set; } = new List<FetchOrder>();
        public static List<Where> DeserializeWhat(string str)
        {
            return JsonSerializer.Deserialize<List<Where>>(str,Utility.DefaultJsonSerializerOptions);
        }
        public static string SerializeWhat(List<Where> what)
        {
            if (what == null || what.Count == 0)
                return null;
            return JsonSerializer.Serialize(what, Utility.DefaultJsonSerializerOptions);
        }
        public static string SerializeScope(List<string> scope)
        {
            if (scope == null || scope.Count == 0)
                return null;
            return JsonSerializer.Serialize(scope, Utility.DefaultJsonSerializerOptions);
        }
        public static List<string> DeserializeScope(string scope)
        {
            return JsonSerializer.Deserialize<List<string>>(scope, Utility.DefaultJsonSerializerOptions);
        }
    }
   
}