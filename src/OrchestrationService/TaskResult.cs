namespace maskx.OrchestrationService
{
    public class TaskResult
    {
        public TaskResult()
        {
        }
        public TaskResult(int code, object content)
        {
            this.Code = code;
            this.ContentType = content?.GetType().FullName;
            this.Content = content;
        }
        public int Code { get; set; }
        public string ContentType { get; set; }
        public object Content { get; set; }
    }
}