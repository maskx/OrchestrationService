using maskx.OrchestrationService.Extensions;
using maskx.OrchestrationService.SQL;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{

    [Trait("C", "CommunicationWorkerClient")]
    public class CommunicationWorkerClientTest : IDisposable
    {
        readonly IHost workerHost;
        readonly CommunicationWorkerClient<CustomCommunicationJob> _CommunicationWorkerClient = null;
        readonly CommunicationWorkerOptions _CommunicationWorkerOptions = null;
        public CommunicationWorkerClientTest()
        {
            _CommunicationWorkerOptions = new CommunicationWorkerOptions()
            {
                AutoCreate = true,
                SchemaName = "comm",
                HubName = "client",
                ConnectionString = TestHelpers.ConnectionString
            };
            workerHost = Host.CreateDefaultBuilder()
                  .ConfigureAppConfiguration((hostingContext, config) =>
                  {
                      config
                      .AddJsonFile("appsettings.json", optional: true)
                      .AddUserSecrets("D2705D0C-A231-4B0D-84B4-FD2BFC6AD8F0");
                  })
                  .ConfigureServices((hostContext, services) =>
                  {
                      services.UsingCommunicationWorkerClient<CustomCommunicationJob>((sp) => _CommunicationWorkerOptions);
                  })
                  .Build();
            workerHost.RunAsync();
            _CommunicationWorkerClient = workerHost.Services.GetService<CommunicationWorkerClient<CustomCommunicationJob>>();
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
                Name = "Rule1",
                Scope = new List<string>() { "ManagementUnit" }
            });
            Assert.NotEqual(Guid.Empty, rtv.Id);
            Assert.Equal("Rule1", rtv.Name);
            Assert.Null(rtv.Description);
            return rtv;
        }
        [Fact(DisplayName = "InjectScope")]
        public void InjectScope()
        {
            Assert.ThrowsAnyAsync<Exception>(() =>
            {
                var r = new FetchRule()
                {
                    Name = "Rule1",
                    Scope = new List<string>() { "ManagementUnit' ; truncate table CustomCommunicationJob;" }
                };

                return _CommunicationWorkerClient.CreateFetchRuleAsync(r);
            }).ContinueWith((ex) =>
            {
                Assert.EndsWith("is not a validate column name", ex.Result.Message);
            });
        }
        [Fact(DisplayName = "InjectWhereName")]
        public void InjectWhereName()
        {
            Assert.ThrowsAnyAsync<Exception>(() =>
            {
                var r = new FetchRule()
                {
                    Name = "Rule1",
                    Scope = new List<string>() { "ManagementUnit" }
                };
                r.What.Add(new Where()
                {
                    Name = "column1' ; truncate table CustomCommunicationJob;",
                    Operator = "=",
                    Value = "1"
                });

                return _CommunicationWorkerClient.CreateFetchRuleAsync(r);
            }).ContinueWith((ex) =>
            {
                Assert.EndsWith("is not a validate column name", ex.Result.Message);
            });
        }
        [Fact(DisplayName = "InjectWhereOperator")]
        public void InjectWhereOperator()
        {
            Assert.ThrowsAnyAsync<Exception>(() =>
            {
                var r = new FetchRule()
                {
                    Name = "Rule1",
                    Scope = new List<string>() { "ManagementUnit" }
                };
                r.What.Add(new Where()
                {
                    Name = "ManagementUnit",
                    Operator = "<>1; truncate table CustomCommunicationJob;",
                    Value = "1"
                });

                return _CommunicationWorkerClient.CreateFetchRuleAsync(r);
            }).ContinueWith((ex) =>
            {
                Assert.EndsWith("is not a validate operator", ex.Result.Message);
            });
        }
        [Fact(DisplayName = "InjectWhereValue")]
        public async Task InjectWhereValue()
        {
            var r = new FetchRule()
            {
                Name = "Rule1",
                Scope = new List<string>() { "ManagementUnit" }
            };
            r.What.Add(new Where()
            {
                Name = "ManagementUnit",
                Operator = "<>",
                Value = "'1' ; truncate table CustomCommunicationJob;"
            });
            var r1 = await _CommunicationWorkerClient.CreateFetchRuleAsync(r);

            using var db = new SQLServerAccess(TestHelpers.ConnectionString);
            db.AddStatement($"select What from {_CommunicationWorkerOptions.FetchRuleTableName} where Id='{r1.Id}'");
            var s = await db.ExecuteScalarAsync();
            Assert.NotNull(s);
            using var doc = JsonDocument.Parse(s.ToString());
            var w = doc.RootElement.EnumerateArray().FirstOrDefault();
            Assert.True(w.TryGetProperty("name", out JsonElement nameV));
            Assert.Equal("ManagementUnit", nameV.GetString());
            Assert.True(w.TryGetProperty("value", out JsonElement valueV));
            Assert.Equal("N'''1'' ; truncate table CustomCommunicationJob;'", valueV.GetString());

            var r2 = await _CommunicationWorkerClient.GetFetchRuleAsync(r1.Id);
            Assert.Single(r2.What);
            Assert.Equal("ManagementUnit", r2.What[0].Name);
            Assert.Equal("'1' ; truncate table CustomCommunicationJob;", r2.What[0].Value);
        }
        [Fact(DisplayName = "WrongDateFormateInWhere")]
        public void WrongDateFormateInWhere()
        {
            Assert.ThrowsAnyAsync<Exception>(() =>
            {
                var r = new FetchRule()
                {
                    Name = "Rule1",
                    Scope = new List<string>() { "ManagementUnit" }
                };
                r.What.Add(new Where()
                {
                    Name = "CreatedTime",
                    Operator = "<>",
                    Value = "11/22/20202"
                });

                return _CommunicationWorkerClient.CreateFetchRuleAsync(r);
            }).ContinueWith((ex) =>
            {
                Assert.EndsWith("need datetime value with format 'YYYY-MM-DD hh:mm:ss.nnnnnnn'", ex.Result.Message);
            });
        }
        [Fact(DisplayName = "InjectDateFormateInWhere")]
        public void InjectDateFormateInWhere()
        {
            Assert.ThrowsAnyAsync<Exception>(() =>
            {
                var r = new FetchRule()
                {
                    Name = "Rule1",
                    Scope = new List<string>() { "ManagementUnit" }
                };
                r.What.Add(new Where()
                {
                    Name = "CreatedTime",
                    Operator = "<>",
                    Value = "2020-11-2 11:10:10:1234567'; truncate talbe communication;"
                });

                return _CommunicationWorkerClient.CreateFetchRuleAsync(r);
            }).ContinueWith((ex) =>
            {
                Assert.EndsWith("need datetime value with format 'YYYY-MM-DD hh:mm:ss.nnnnnnn'", ex.Result.Message);
            });
        }
        [Fact(DisplayName = "DateValueInWhere")]
        public async Task DateValueInWhere()
        {

            var r = new FetchRule()
            {
                Name = "Rule1",
                Scope = new List<string>() { "ManagementUnit" }
            };
            r.What.Add(new Where()
            {
                Name = "CreatedTime",
                Operator = "<>",
                Value = "2020-11-2"
            });
            await _CommunicationWorkerClient.CreateFetchRuleAsync(r);
            var r1 = await _CommunicationWorkerClient.GetFetchRuleAsync(r.Id);
            Assert.NotNull(r1);
            Assert.Single(r1.What);
            Assert.Equal("2020-11-2", r1.What[0].Value);
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
            Assert.True(r.What.TrySerializeWhat(typeof(CustomCommunicationJob), out string s));
            Assert.True(r3.What.TrySerializeWhat(typeof(CustomCommunicationJob), out string s3));
            Assert.Equal(s, s3);
            Assert.Equal(r.Id, r3.Id);
        }
        [Fact(DisplayName = "UpdateFetchRuleScope")]
        public async Task UpdateFetchRuleScope()
        {
            var l = await CreateFetchRuleAsync();
            l.Concurrency = 99;
            int count = l.Scope.Count;
            l.Scope.Add("EventName");
            await _CommunicationWorkerClient.UpdateFetchRuleAsync(l);
            var r = await _CommunicationWorkerClient.GetFetchRuleAsync(l.Id);
            Assert.Equal(count + 1, r.Scope.Count);
            Assert.Contains("EventName", r.Scope);
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
            if (_CommunicationWorkerClient != null)
                _CommunicationWorkerClient.DeleteCommunicationAsync().Wait();

            GC.SuppressFinalize(this);
        }
    }
}
