using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace maskx.OrchestrationService.Activity
{
    public class HttpRequestInput
    {
        public string Uri { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public HttpMethod Method { get; set; }
        public string Content { get; set; }
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public string MediaType { get; set; } = "application/json";
    }
}