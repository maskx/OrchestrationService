using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationService.Orchestration
{
    public class HttpRequest
    {
        public string Uri { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Method { get; set; }
        public string Body { get; set; }
    }
}