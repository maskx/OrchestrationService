using DurableTask.Core;
using maskx.OrchestrationService;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.Orchestration
{
    public class HttpOrchestration : TaskOrchestration<TaskResult, HttpRequestActivity>
    {
        // TODO: need rewrite
        public static string DbConnectionString { get; set; }

        private const string EventName = "Response";
        private TaskCompletionSource<TaskResult> waitHandler;

        public override async Task<TaskResult> RunTask(OrchestrationContext context, HttpRequestActivity request)
        {
            waitHandler = new TaskCompletionSource<TaskResult>();
            await WriteRequest(context, request);
            await waitHandler.Task;
            var response = waitHandler.Task.Result;

            return response;
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (name == EventName && this.waitHandler != null)
            {
                this.waitHandler.SetResult(new TaskResult()
                {
                    Code = 200,
                    Content = input
                });
            }
        }

        private async Task WriteRequest(OrchestrationContext context, HttpRequestActivity request)
        {
            using (var conn = new SqlConnection(DbConnectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = $"insert into communication (InstanceId,ExecutionId,EventName,RequestUri,[Status]) values (@InstanceId,@ExecutionId,@EventName,@RequestUri,@Status)";
                cmd.Parameters.AddWithValue("InstanceId", context.OrchestrationInstance.InstanceId);
                cmd.Parameters.AddWithValue("ExecutionId", context.OrchestrationInstance.ExecutionId);
                cmd.Parameters.AddWithValue("EventName", EventName);
                cmd.Parameters.AddWithValue("RequestUri", request.Uri);
                cmd.Parameters.AddWithValue("Status", "Pending");
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}