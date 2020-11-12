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

    [Trait("C", "CommunicationWorkerClient")]
    public class CommunicationWorkerClientTest : IDisposable
    {
        readonly IHost workerHost;
        readonly CommunicationWorkerClient _CommunicationWorkerClient = null;
        readonly IOrchestrationService _SQLServerOrchestrationService = null;
        readonly CommunicationWorker<CommunicationJob> _CommunicationWorker = null;
        public CommunicationWorkerClientTest()
        {
            workerHost = TestHelpers.CreateHostBuilder(
                 hubName: "A" + DateTime.Now.Millisecond.ToString()
                ).Build();
            workerHost.RunAsync();
            _CommunicationWorkerClient = workerHost.Services.GetService<CommunicationWorkerClient>();
            _SQLServerOrchestrationService = workerHost.Services.GetService<IOrchestrationService>();
            _CommunicationWorker = workerHost.Services.GetService<CommunicationWorker<CommunicationJob>>();
        }
        [Fact(DisplayName = "GetFetchRuleAsync")]
        public async Task GetFetchRuleAsync()
        {
            await CreateFetchRuleAsync();
            var rtv = await _CommunicationWorkerClient.GetFetchRuleAsync();
            Assert.NotEmpty(rtv);
        }
        [Fact(DisplayName = "GetFetchRuleAsyncById")]
        public async Task GetFetchRuleAsyncById()
        {
            var r = await CreateFetchRuleAsync();
            var rtv = await _CommunicationWorkerClient.GetFetchRuleAsync(r.Id);
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
            var r1 = await _CommunicationWorkerClient.GetFetchRuleAsync(r.Id);
            Assert.NotNull(r1);
            await _CommunicationWorkerClient.DeleteFetchRuleAsync(r.Id);
            var r2 = await _CommunicationWorkerClient.GetFetchRuleAsync(r.Id);
            Assert.Null(r2);
        }
        [Fact(DisplayName = "CreateFetchRuleAsync")]
        public async Task<FetchRule> CreateFetchRuleAsync()
        {
            var rtv = await _CommunicationWorkerClient.CreateFetchRuleAsync(new FetchRule()
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
            var r2 = await _CommunicationWorkerClient.UpdateFetchRuleAsync(r);
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
            await _CommunicationWorkerClient.UpdateFetchRuleAsync(r);
            var r3 = await _CommunicationWorkerClient.GetFetchRuleAsync(r.Id);
            Assert.Equal("UpdateFetchRuleWhat", r3.Description);
            Assert.Equal(2, r3.What.Count);
            Assert.Equal(r.What.SerializeWhat(), r3.What.SerializeWhat());
            Assert.Equal(r.Id, r3.Id);
        }
        [Fact(DisplayName = "UpdateFetchRuleScope")]
        public async Task UpdateFetchRuleScope()
        {
            var l = await CreateFetchRuleAsync();
            l.Concurrency = 99;
            l.Scope.Add("EventName");
            await _CommunicationWorkerClient.UpdateFetchRuleAsync(l);
            var r = await _CommunicationWorkerClient.GetFetchRuleAsync(l.Id);
            Assert.Single(r.Scope);
            Assert.Equal("EventName", r.Scope[0]);
            Assert.Equal(99, r.Concurrency);
        }
        [Fact(DisplayName = "UpdateFetchRuleFetchOrder")]
        public async Task UpdateFetchRuleFetchOrder()
        {
            var l = await CreateFetchRuleAsync();
            l.Concurrency = 99;
            l.FetchOrder = new List<FetchOrder>() { new FetchOrder() { Field = "Processor" } };
            l.Scope.Add("Processor");
            await _CommunicationWorkerClient.UpdateFetchRuleAsync(l);
            var r = await _CommunicationWorkerClient.GetFetchRuleAsync(l.Id);
            Assert.Single(r.FetchOrder);
            Assert.Equal("Processor", r.FetchOrder[0].Field);
            Assert.Equal("ASC", r.FetchOrder[0].Order);
        }
        [Fact(DisplayName = "BuildFetchCommunicationJobSPAsync")]
        public async Task BuildFetchCommunicationJobSPAsync()
        {
            await _CommunicationWorkerClient.BuildFetchCommunicationJobSPAsync();

            await CreateFetchRuleAsync();
            await _CommunicationWorkerClient.BuildFetchCommunicationJobSPAsync();

            await UpdateFetchRuleWhat();
            await _CommunicationWorkerClient.BuildFetchCommunicationJobSPAsync();

            await UpdateFetchRuleScope();
            await _CommunicationWorkerClient.BuildFetchCommunicationJobSPAsync();
        }
        [Fact(DisplayName = "SetCommonFetchOrder")]
        public async Task SetCommonFetchOrder()
        {
            await _CommunicationWorkerClient.SetCommonFetchOrderAsyc(new List<FetchOrder>());
            var r = await _CommunicationWorkerClient.GetCommonFetchOrderAsync();
            Assert.Empty(r);
            await _CommunicationWorkerClient.SetCommonFetchOrderAsyc(new List<FetchOrder>() {
                new FetchOrder() { Field="Processor",Order="desc"} });
            r = await _CommunicationWorkerClient.GetCommonFetchOrderAsync();
            Assert.Single(r);

        }
        public void Dispose()
        {
            if (_CommunicationWorker != null)
                _CommunicationWorker.DeleteCommunicationAsync().Wait();
            if (_SQLServerOrchestrationService != null)
                _SQLServerOrchestrationService.DeleteAsync(true).Wait();
            GC.SuppressFinalize(this);
        }
    }
}
