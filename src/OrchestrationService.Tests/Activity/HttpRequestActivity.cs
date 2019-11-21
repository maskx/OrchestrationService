using DurableTask.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrchestrationService.Tests.Orchestration
{
    public class HttpRequestActivity : TaskActivity<string, string>
    {
        public string Uri { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Method { get; set; }
        public string Body { get; set; }

        protected override string Execute(TaskContext context, string input)
        {
            throw new NotImplementedException();
        }
    }
}