using System.Text.Json;

namespace maskx.OrchestrationService.Worker
{
    public class Where
    {
        public string Name { get; set; }
        public string Operator { get; set; }
        private string v;
        public string Value
        {
            get { return WhereExtension.SaveSQLValue(v); }
            set { v = value; }
        }
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, Utilities.Utility.DefaultJsonSerializerOptions);
        }
    }
    public static class WhereExtension
    {
        // todo: validate user input
        public static (bool Result, string Message) IsValid(this Where where)
        {
            return (true, string.Empty);
        }
        public static string SaveSQLValue(string s)
        {
            return s.Replace(" ","").Replace("\n","").Replace("\r","");
        }
    }
}
