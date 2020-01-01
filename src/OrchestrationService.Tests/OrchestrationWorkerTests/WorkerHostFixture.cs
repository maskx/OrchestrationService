using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using Xunit;

namespace OrchestrationService.Tests.OrchestrationWorkerTests
{
    public class WorkerHostFixture : IDisposable
    {
        private IHost workerHost = null;

        public WorkerHostFixture()
        {
            CommunicationWorkerOptions options = new CommunicationWorkerOptions();
            options.HubName = "NoRule";
            List<Type> orchestrationTypes = new List<Type>();
            workerHost = TestHelpers.CreateHostBuilder(options, orchestrationTypes).Build();
            workerHost.RunAsync();
        }

        public void Dispose()
        {
            if (workerHost != null)
                workerHost.StopAsync();
        }
    }

    [CollectionDefinition("WorkerHost Collection")]
    public class WebHostCollection : ICollectionFixture<WorkerHostFixture>
    {
    }
}