using maskx.OrchestrationService.SQL;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Worker
{
    public class CommunicationWorkerClient<T> where T : CommunicationJob, new()
    {
        private readonly CommunicationWorkerOptions _Options;
        public CommunicationWorkerClient(IOptions<CommunicationWorkerOptions> options)
        {
            _Options = options?.Value;
            if (_Options.AutoCreate)
                CreateIfNotExistsAsync(false).Wait();
        }
        public async Task<List<FetchRule>> GetFetchRuleAsync()
        {
            using var db = new SQLServerAccess(_Options.ConnectionString);
            db.AddStatement($"select Id,Name,Description,What,Scope,Concurrency,CreatedTimeUtc,UpdatedTimeUtc,FetchOrder from {_Options.FetchRuleTableName}");
            List<FetchRule> rules = new List<FetchRule>();
            await db.ExecuteReaderAsync((reader, index) =>
            {
                rules.Add(new FetchRule()
                {
                    Id = reader.GetGuid(0),
                    Name = reader["Name"].ToString(),
                    Description = reader["Description"]?.ToString(),
                    What = reader.IsDBNull(3) ? new List<Where>() : reader.GetString(3).DeserializeWhat(typeof(T)),
                    Scope = reader.IsDBNull(4) ? new List<string>() : reader.GetString(4).DeserializeScope(),
                    Concurrency = reader.GetInt32(5),
                    CreatedTimeUtc = reader.GetDateTime(6),
                    UpdatedTimeUtc = reader.IsDBNull(7) ? default : reader.GetDateTime(7),
                    FetchOrder = reader.IsDBNull(8) ? new List<FetchOrder>() : reader.GetString(8).DeserializeFetchOrderList()
                }); ;
            });
            return rules;
        }
        public async Task<FetchRule> GetFetchRuleAsync(Guid id)
        {
            using var db = new SQLServerAccess(_Options.ConnectionString);
            db.AddStatement($"select Id,Name,Description,What,Scope,Concurrency,CreatedTimeUtc,UpdatedTimeUtc,FetchOrder from {_Options.FetchRuleTableName} where Id=@Id", new { Id = id });
            FetchRule rule = null;
            await db.ExecuteReaderAsync((reader, index) =>
            {
                rule = new FetchRule()
                {
                    Id = id,
                    Name = reader[1].ToString(),
                    Description = reader.IsDBNull(2) ? null : reader[2].ToString(),
                    What = reader.IsDBNull(3) ? new List<Where>() : reader.GetString(3).DeserializeWhat(typeof(T)),
                    Scope = reader.IsDBNull(4) ? new List<string>() : reader.GetString(4).DeserializeScope(),
                    Concurrency = reader.GetInt32(5),
                    CreatedTimeUtc = reader.GetDateTime(6),
                    UpdatedTimeUtc = reader.IsDBNull(7) ? default : reader.GetDateTime(7),
                    FetchOrder = reader.IsDBNull(8) ? new List<FetchOrder>() : reader.GetString(8).DeserializeFetchOrderList()
                };
            });
            return rule;
        }
        public async Task DeleteFetchRuleAsync(Guid id)
        {
            using var db = new SQLServerAccess(_Options.ConnectionString);
            db.AddStatement($"delete {_Options.FetchRuleTableName} where Id=@Id", new { Id = id });
            await db.ExecuteNonQueryAsync();
        }
        public async Task<FetchRule> CreateFetchRuleAsync(FetchRule fetchRule)
        {
            var errorMsg = fetchRule.Validate(typeof(T), out Dictionary<string, object> par);
            if (!string.IsNullOrEmpty(errorMsg))
                throw new Exception(errorMsg);
            using var db = new SQLServerAccess(_Options.ConnectionString);
            db.AddStatement($"INSERT INTO {_Options.FetchRuleTableName} ([Name],[Description],[What],[Scope],[Concurrency],[FetchOrder]) OUTPUT inserted.Id,inserted.CreatedTimeUtc,inserted.UpdatedTimeUtc  VALUES (@Name,@Description,@What,@Scope,@Concurrency,@FetchOrder)",
                par);
            await db.ExecuteReaderAsync((reader, index) =>
            {
                fetchRule.Id = reader.GetGuid(0);
                fetchRule.CreatedTimeUtc = reader.GetDateTime(1);
                fetchRule.UpdatedTimeUtc = reader.GetDateTime(2);
            });

            return fetchRule;
        }
        public async Task<FetchRule> UpdateFetchRuleAsync(FetchRule fetchRule)
        {
            var errorMsg = fetchRule.Validate(typeof(T), out Dictionary<string, object> par);
            if (!string.IsNullOrEmpty(errorMsg))
                throw new Exception(errorMsg);
            par.Add("Id", fetchRule.Id);
            using var db = new SQLServerAccess(_Options.ConnectionString);
            db.AddStatement($"update {_Options.FetchRuleTableName} set Name=@Name,Description=@Description,What=@What,Scope=@Scope,Concurrency=@Concurrency,UpdatedTimeUtc=getutcdate(),FetchOrder=@FetchOrder where Id=@Id",
                par);
            await db.ExecuteNonQueryAsync();
            return fetchRule;
        }
        /// <summary>
        /// Apply the fetch rule settings 
        /// </summary>
        /// <returns></returns>
        public async Task BuildFetchCommunicationJobSPAsync()
        {
            using var db = new SQLServerAccess(_Options.ConnectionString);
            await db.ExecuteStoredProcedureASync(_Options.BuildFetchCommunicationJobSPName);
        }
        public async Task SetCommonFetchOrderAsyc(List<FetchOrder> fetchOrder)
        {
            if (!fetchOrder.TrySerialize(typeof(T), out string order))
                throw new Exception(order);
            using var db = new SQLServerAccess(_Options.ConnectionString);
            await db.ExecuteStoredProcedureASync(_Options.ConfigCommunicationSettingSPName,
                new
                {
                    Key = CommunicationWorkerOptions.FetchOrderConfigurationKey,
                    Value = order
                });
        }
        public async Task<List<FetchOrder>> GetCommonFetchOrderAsync()
        {
            using var db = new SQLServerAccess(_Options.ConnectionString);
            db.AddStatement($"select [Value] from {_Options.CommunicationSettingTableName} where [Key]=N'{CommunicationWorkerOptions.FetchOrderConfigurationKey}'");
            var r = await db.ExecuteScalarAsync();
            if (r == null)
                return new List<FetchOrder>();
            return r.ToString().DeserializeFetchOrderList();
        }
        public async Task DeleteCommunicationAsync()
        {
            await Utilities.Utility.ExecuteSqlScriptAsync("drop-schema.sql", this._Options);
        }
        public async Task CreateIfNotExistsAsync(bool recreate)
        {
            if (recreate) await DeleteCommunicationAsync();
            var str = string.Format(@"
IF(SCHEMA_ID('{0}') IS NULL)
BEGIN
    EXEC sp_executesql N'CREATE SCHEMA [{0}]'
END
GO
IF  NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}_{2}]') AND type in (N'U'))
BEGIN
", _Options.SchemaName, _Options.HubName, CommunicationWorkerOptions.CommunicationTable);
            str += Utilities.Utility.BuildTableScript(typeof(T), $"{_Options.HubName}_{CommunicationWorkerOptions.CommunicationTable}", _Options.SchemaName);
            str += @"
END
GO
";
            str += await Utilities.Utility.GetScriptTextAsync("create-schema.sql", _Options.SchemaName, _Options.HubName);
            await Utilities.Utility.ExecuteSqlScriptAsync(str, this._Options.ConnectionString);
        }
    }
}
