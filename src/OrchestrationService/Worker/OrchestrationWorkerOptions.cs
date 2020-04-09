namespace maskx.OrchestrationService.Worker
{
    public class OrchestrationWorkerOptions : Extensions.OrchestrationWorkerOptions
    {
        /// <summary>
        /// Auto-creates the necessary resources for the orchestration service and the instance store
        /// </summary>
        public bool AutoCreate { get; set; } = false;
       
    }
}