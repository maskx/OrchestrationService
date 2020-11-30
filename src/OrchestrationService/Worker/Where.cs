using System.Collections.Generic;

namespace maskx.OrchestrationService.Worker
{
    public class Where
    {
        public readonly static List<string> ValidationOperation = new List<string>() {
        "=","<>",">","<",">=","<=","like"
        };
        public string Name { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }
}
