using DurableTask.Core;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{

    [Trait("C", "CommunicationWorker")]
    public class FetchRuleManageTest : IDisposable
    {
        IHost workerHost;
        CommunicationWorker communicationWorker = null;
        IOrchestrationService SQLServerOrchestrationService = null;
        public FetchRuleManageTest()
        {
            workerHost = TestHelpers.CreateHostBuilder(
                 hubName: "A" + DateTime.Now.Millisecond.ToString()
                ).Build();
            workerHost.RunAsync();
            communicationWorker = workerHost.Services.GetService<CommunicationWorker>();
            SQLServerOrchestrationService = workerHost.Services.GetService<IOrchestrationService>();
        }
        [Fact(DisplayName = "GetFetchRuleAsync")]
        public async Task GetFetchRuleAsync()
        {
            await CreateFetchRuleAsync();
            var rtv = await communicationWorker.GetFetchRuleAsync();
            Assert.NotEmpty(rtv);
        }
        [Fact(DisplayName = "GetFetchRuleAsyncById")]
        public async Task GetFetchRuleAsyncById()
        {
            var r = await CreateFetchRuleAsync();
            var rtv = await communicationWorker.GetFetchRuleAsync(r.Id);
            Assert.Equal(r.Id, rtv.Id);
            Assert.Equal(r.Name, rtv.Name);
            Assert.Equal(r.Description, rtv.Description);
            Assert.Equal(r.CreatedTimeUtc, rtv.CreatedTimeUtc);
            Assert.Equal(r.UpdatedTimeUtc, rtv.UpdatedTimeUtc);
        }
        [Fact(DisplayName = "DeleteFetchRuleAsync")]
        public async Task DeleteFetchRuleAsync()
        {
            var r = await CreateFetchRuleAsync();
            var r1 = await communicationWorker.GetFetchRuleAsync(r.Id);
            Assert.NotNull(r1);
            await communicationWorker.DeleteFetchRuleAsync(r.Id);
            var r2 = await communicationWorker.GetFetchRuleAsync(r.Id);
            Assert.Null(r2);
        }
        [Fact(DisplayName = "CreateFetchRuleAsync")]
        public async Task<FetchRule> CreateFetchRuleAsync()
        {
            var rtv = await communicationWorker.CreateFetchRuleAsync(new FetchRule()
            {
                Name = "Rule1"
            });
            Assert.NotEqual(Guid.Empty, rtv.Id);
            Assert.Equal("Rule1", rtv.Name);
            Assert.Null(rtv.Description);
            return rtv;
        }
        [Fact(DisplayName = "UpdateFetchRuleAsync")]
        public async Task UpdateFetchRuleAsync()
        {
            var r = await CreateFetchRuleAsync();
            r.Description = "UpdateFetchRuleAsync";
            r.What = new List<Where>();
            var r2 = await communicationWorker.UpdateFetchRuleAsync(r);
            Assert.Equal("UpdateFetchRuleAsync", r2.Description);
            Assert.Empty(r2.What);
            Assert.Equal(r.Id, r2.Id);
        }
        [Fact(DisplayName = "UpdateFetchRuleWhat")]
        public async Task UpdateFetchRuleWhat()
        {
            var r = await CreateFetchRuleAsync();
            r.Description = "UpdateFetchRuleWhat";
            r.What = new List<Where>() { new Where() {
                Name="Processor",
                Operator="=",
                Value="'MockCommunicationProcessor'"
            } ,new Where(){
                Name="Status",
                Operator="<>",
                Value="2"
            } };
            await communicationWorker.UpdateFetchRuleAsync(r);
            var r3 = await communicationWorker.GetFetchRuleAsync(r.Id);
            Assert.Equal("UpdateFetchRuleWhat", r3.Description);
            Assert.Equal(2, r3.What.Count);
            Assert.Equal(FetchRule.SerializeWhat(r.What),FetchRule.SerializeWhat(r3.What));
            Assert.Equal(r.Id, r3.Id);
        }
        [Fact(DisplayName = "UpdateFetchRuleScope")]
        public async Task UpdateFetchRuleScope()
        {
            var l = await CreateFetchRuleAsync();
            l.Concurrency = 99;
            l.Scope.Add("EventName");
            await communicationWorker.UpdateFetchRuleAsync(l);
            var r =await communicationWorker.GetFetchRuleAsync(l.Id);
            Assert.Single(r.Scope);
            Assert.Equal("EventName",r.Scope[0]);
            Assert.Equal(99,r.Concurrency);
        }
        [Fact(DisplayName = "BuildFetchCommunicationJobSPAsync")]
        public async Task BuildFetchCommunicationJobSPAsync()
        {
            await communicationWorker.BuildFetchCommunicationJobSPAsync();

            await CreateFetchRuleAsync();
            await communicationWorker.BuildFetchCommunicationJobSPAsync();

            await UpdateFetchRuleWhat();
            await communicationWorker.BuildFetchCommunicationJobSPAsync();

            await UpdateFetchRuleScope();
            await communicationWorker.BuildFetchCommunicationJobSPAsync();
        }

        public void Dispose()
        {
            if (communicationWorker != null)
                communicationWorker.DeleteCommunicationAsync().Wait();
            if (SQLServerOrchestrationService != null)
                SQLServerOrchestrationService.DeleteAsync(true).Wait();
        }
    }
}
