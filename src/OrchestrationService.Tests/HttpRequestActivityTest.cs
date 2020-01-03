using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace OrchestrationService.Tests
{
    [Trait("C", "HttpRequestActivity")]
    public class HttpRequestActivityTest : IDisposable
    {
        private DataConverter dataConverter = new JsonDataConverter();
        private IHost workerHost = null;
        private OrchestrationWorker orchestrationWorker;

        public HttpRequestActivityTest()
        {
            CommunicationWorkerOptions options = new CommunicationWorkerOptions();
            options.HubName = "NoRule";
            List<Type> orchestrationTypes = new List<Type>();
            orchestrationTypes.Add(typeof(HttpOrchestration));
            workerHost = TestHelpers.CreateHostBuilder(options, orchestrationTypes).Build();
            workerHost.RunAsync();
            orchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
        }

        public void Dispose()
        {
            if (workerHost != null)
                workerHost.StopAsync();
        }

        [Fact(DisplayName = "Get")]
        public void Get()
        {
            HttpRequestInput request = new HttpRequestInput()
            {
                Method = HttpMethod.Get,
                Uri = "https://services.odata.org/TripPinRESTierService/People('russellwhyte')"
            };

            var instance = orchestrationWorker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new maskx.OrchestrationService.OrchestrationSetting()
                {
                    Creator = "DICreator",
                    Uri = typeof(HttpOrchestration).FullName + "_"
                },
                Input = dataConverter.Serialize(request)
            }).Result;
            while (true)
            {
                var result = TestHelpers.TaskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    var response = dataConverter.Deserialize<TaskResult>(result.Output);
                    Assert.Equal(200, response.Code);
                    Assert.Contains("russellwhyte", response.Content);
                    break;
                }
            }
        }

        [Fact(DisplayName = "Post")]
        public void Post()
        {
            string json = @"{
    ""UserName"":""lewisblack"",
    ""FirstName"":""Lewis"",
    ""LastName"":""Black"",
    ""Emails"":[
        ""lewisblack@example.com""
    ],
    ""AddressInfo"": [
    {
      ""Address"": ""187 Suffolk Ln."",
      ""City"": {
        ""Name"": ""Boise"",
        ""CountryRegion"": ""United States"",
        ""Region"": ""ID""
            }
}
    ]
}";
            HttpRequestInput request = new HttpRequestInput()
            {
                Method = HttpMethod.Post,
                Uri = "https://services.odata.org/TripPinRESTierService/People('russellwhyte')",
                Content = json
            };

            var instance = orchestrationWorker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new maskx.OrchestrationService.OrchestrationSetting()
                {
                    Creator = "DICreator",
                    Uri = typeof(HttpOrchestration).FullName + "_"
                },
                Input = dataConverter.Serialize(request)
            }).Result;
            while (true)
            {
                var result = TestHelpers.TaskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    var response = dataConverter.Deserialize<TaskResult>(result.Output);
                    Assert.Equal(200, response.Code);
                    Assert.Contains("russellwhyte", response.Content);
                    break;
                }
            }
        }

        [Fact(DisplayName = "Delete")]
        public void Delete()
        {
            HttpRequestInput request = new HttpRequestInput()
            {
                Method = HttpMethod.Delete,
                Uri = "https://services.odata.org/TripPinRESTierService/People('russellwhyte')"
            };

            var instance = orchestrationWorker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new maskx.OrchestrationService.OrchestrationSetting()
                {
                    Creator = "DICreator",
                    Uri = typeof(HttpOrchestration).FullName + "_"
                },
                Input = dataConverter.Serialize(request)
            }).Result;
            while (true)
            {
                var result = TestHelpers.TaskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    var response = dataConverter.Deserialize<TaskResult>(result.Output);
                    Assert.Equal(204, response.Code);
                    Assert.Empty(response.Content);
                    break;
                }
            }
        }

        [Fact(DisplayName = "Patch")]
        public void Patch()
        {
            string json = @"{
    ""FirstName"": ""Mirs"",
    ""LastName"": ""King""
}";
            HttpRequestInput request = new HttpRequestInput()
            {
                Method = HttpMethod.Patch,
                Uri = "https://services.odata.org/TripPinRESTierService/People('russellwhyte')",
                Content = json
            };

            var instance = orchestrationWorker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new OrchestrationSetting()
                {
                    Creator = "DICreator",
                    Uri = typeof(HttpOrchestration).FullName + "_"
                },
                Input = dataConverter.Serialize(request)
            }).Result;
            while (true)
            {
                var result = TestHelpers.TaskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    var response = dataConverter.Deserialize<TaskResult>(result.Output);
                    Assert.Equal(204, response.Code);
                    Assert.Empty(response.Content);
                    break;
                }
            }
        }

        public class HttpOrchestration : TaskOrchestration<TaskResult, string>
        {
            public override async Task<TaskResult> RunTask(OrchestrationContext context, string input)
            {
                var request = DataConverter.Deserialize<HttpRequestInput>(input);
                return await context.ScheduleTask<TaskResult>(typeof(HttpRequestActivity), request);
            }
        }
    }
}