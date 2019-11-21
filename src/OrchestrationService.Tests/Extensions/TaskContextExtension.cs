using DurableTask.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrchestrationService.Tests.Extensions
{
    public static class TaskContextExtension
    {
        public static IServiceProvider ServiceProvider { get; set; }
        public static IConfiguration Configuration { get; set; }

        public static IConfiguration GetConfiguration(this TaskContext cxt)
        {
            return Configuration;
        }

        public static string GetConnectionString(this TaskContext cxt)
        {
            return Configuration.GetConnectionString("dbConnection");
        }
    }
}