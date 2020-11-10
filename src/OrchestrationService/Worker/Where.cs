using System.Text.Encodings.Web;
using System.Text.Json;

namespace maskx.OrchestrationService.Worker
{
    public class Where
    {
        public string Name { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public override string ToString()
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(this, options);
        }
    }
}
