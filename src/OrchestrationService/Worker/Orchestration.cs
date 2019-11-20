using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationService.Worker
{
    public class Orchestration
    {
        public string Uri { get; set; }
        public string Version { get; set; }
        public string Content { get; set; }
        public string Creator { get; set; }
    }
}