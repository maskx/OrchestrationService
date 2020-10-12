using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

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
        /// 限制并发请求的范围，如Subscription、ManagementUnit
        /// </summary>
        public List<Limitation> Limitions { get; set; } = new List<Limitation>();
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