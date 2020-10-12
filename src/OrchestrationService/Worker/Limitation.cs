using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace maskx.OrchestrationService.Worker
{
    public class Limitation
    {
        public Guid Id { get; set; }
        public Guid FetchRuleId { get; set; }
        /// <summary>
        /// 并发请求的上限
        /// </summary>
        public int Concurrency { get; set; }

        /// <summary>
        /// 限制并发请求的范围，如Subscription、ManagementUnit
        /// </summary>
        public List<string> Scope { get; set; } = new List<string>();
        public DateTime CreatedTimeUtc { get; set; }
        public DateTime UpdatedTimeUtc { get; set; }

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
}