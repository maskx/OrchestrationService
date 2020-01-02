﻿using maskx.DurableTask.SQLServer.SQL;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{
    [Trait("C", "CommunicationWorker")]
    public class CreateCommunicationTableTest
    {
        [Fact(DisplayName = "CreateNewCommunicationTable")]
        public async Task CreateNewCommunicationTable()
        {
            CommunicationWorkerOptions options = new CommunicationWorkerOptions();
            options.ConnectionString = TestHelpers.ConnectionString;
            options.HubName = DateTime.Now.Millisecond.ToString();
            var client = TestHelpers.CreateOrchestrationClient();
            var host = Host.CreateDefaultBuilder()
                 .ConfigureServices((hostContext, services) =>
                 {
                 }).Build();
            CommunicationWorker worker = new CommunicationWorker(host.Services, client, Options.Create(options));
            await worker.StartAsync(new System.Threading.CancellationToken());
            object r = null;
            using (var db = new DbAccess(options.ConnectionString))
            {
                db.AddStatement($"select OBJECT_ID('{options.CommunicationTableName}')");
                r = await db.ExecuteScalarAsync();
            }
            Assert.NotNull(r);

            using (var db = new DbAccess(options.ConnectionString))
            {
                db.AddStatement($"DROP TABLE IF EXISTS {options.CommunicationTableName}");
                await db.ExecuteNonQueryAsync();
            }
        }
    }
}