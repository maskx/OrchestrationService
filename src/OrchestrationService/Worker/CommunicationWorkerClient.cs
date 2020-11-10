using maskx.OrchestrationService.SQL;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Worker
{
    public class CommunicationWorkerClient
    {
        private readonly CommunicationWorkerOptions _Options;
        public CommunicationWorkerClient(IOptions<CommunicationWorkerOptions> options)
        {
            _Options = options?.Value;
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
                    What = reader.IsDBNull(3) ? new List<Where>() : FetchRule.DeserializeWhat(reader.GetString(3)),
                    Scope = reader.IsDBNull(4) ? new List<string>() : FetchRule.DeserializeScope(reader.GetString(4)),
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
                    What = reader.IsDBNull(3) ? new List<Where>() : FetchRule.DeserializeWhat(reader.GetString(3)),
                    Scope = reader.IsDBNull(4) ? new List<string>() : FetchRule.DeserializeScope(reader.GetString(4)),
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
            using var db = new SQLServerAccess(_Options.ConnectionString);
            db.AddStatement($"INSERT INTO {_Options.FetchRuleTableName} ([Name],[Description],[What],[Scope],[Concurrency],[FetchOrder]) OUTPUT inserted.Id,inserted.CreatedTimeUtc,inserted.UpdatedTimeUtc  VALUES (@Name,@Description,@What,@Scope,@Concurrency,@FetchOrder)",
                new
                {
                    fetchRule.Name,
                    fetchRule.Description,
                    What = FetchRule.SerializeWhat(fetchRule.What),
                    Scope = FetchRule.SerializeScope(fetchRule.Scope),
                    fetchRule.Concurrency,
                    FetchOrder = fetchRule.FetchOrder.Serialize()
                });
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
            using var db = new SQLServerAccess(_Options.ConnectionString);
            db.AddStatement($"update {_Options.FetchRuleTableName} set Name=@Name,Description=@Description,What=@What,Scope=@Scope,Concurrency=@Concurrency,UpdatedTimeUtc=getutcdate(),FetchOrder=@FetchOrder where Id=@Id",
                new
                {
                    fetchRule.Name,
                    fetchRule.Description,
                    What = FetchRule.SerializeWhat(fetchRule.What),
                    Scope = FetchRule.SerializeScope(fetchRule.Scope),
                    fetchRule.Concurrency,
                    fetchRule.Id,
                    FetchOrder = fetchRule.FetchOrder.Serialize()
                });
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
            using var db = new SQLServerAccess(_Options.ConnectionString);
            await db.ExecuteStoredProcedureASync(_Options.ConfigCommunicationSettingSPName,
                new
                {
                    Key = CommunicationWorkerOptions.FetchOrderConfigurationKey,
                    Value = fetchOrder.Serialize()
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
    }
}
