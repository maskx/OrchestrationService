using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
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
        /// default is createtime desc
        /// todo: 支持设置优先级，需提供无并发控制条目优先级设置的机制
        /// </summary>
        public List<(string Filed, string Order)> Priority = new List<(string Filed, string Order)>();
        public static List<Where> DeserializeWhat(string str)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Deserialize<List<Where>>(str, options);
        }
        public static string SerializeWhat(List<Where> what)
        {
            if (what == null || what.Count==0)
                return null;
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(what, options);
        }
        public static string SerializeScope(List<string> scope)
        {
            if (scope == null || scope.Count == 0)
                return null;
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(scope, options);
        }
        public static List<string> DeserializeScope(string scope)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Deserialize<List<string>>(scope, options);
        }
    }
    public class Where
    {
        public string Name { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public override string ToString()
        {            
            JsonSerializerOptions options = new JsonSerializerOptions() { 
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy=JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize<Where>(this,options);
        }
    }
}