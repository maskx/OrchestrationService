namespace maskx.OrchestrationService
{
    public class TaskResult
    {
        public TaskResult()
        {
        }

        public TaskResult(int code, string content)
        {
            this.Code = code;
            this.Content = content;
        }

        public int Code { get; set; }
        public string Content { get; set; }
    }
}