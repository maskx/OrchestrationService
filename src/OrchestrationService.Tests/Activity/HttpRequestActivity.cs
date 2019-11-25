using DurableTask.Core;
using maskx.OrchestrationService;
using System;
using System.Collections.Generic;
using System.Text;
using OrchestrationService.Tests.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.Activity
{
    public class HttpRequestActivity : TaskActivity<HttpRequest, TaskResult>
    {
        protected override TaskResult Execute(TaskContext context, HttpRequest request)
        {
            switch (request.Method)
            {
                case "GET":
                    return context.HttpGet(request.Uri).Result;

                default:
                    break;
            }
            return new TaskResult() { Code = 400, Content = "Method Error" };
        }
    }
}