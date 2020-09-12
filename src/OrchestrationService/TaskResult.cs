using DurableTask.Core.Serializing;

namespace maskx.OrchestrationService
{
    public class TaskResult
    {
        public TaskResult()
        {
        }

        public TaskResult(int code, string contentType, string content)
        {
            this.Code = code;
            this.Content = content;
            this.ContentType = contentType;
        }
        public TaskResult(int code, object content)
        {
            this.Code = code;
            this.ContentType = content.GetType().FullName;
            if (this.ContentType == typeof(string).FullName)
                this.Content = content.ToString();
            else
                this.Content = DataConverter.Serialize(content);
        }
        public int Code { get; set; }
        public string ContentType { get; set; }
        public string Content { get; set; }
        private DataConverter DataConverter = new JsonDataConverter();
    }
}