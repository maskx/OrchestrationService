using maskx.OrchestrationService.Extensions;
using maskx.OrchestrationService.Worker;
using Microsoft.SqlServer.Management.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Utilities
{
    public static class Utility
    {
        internal static async Task ExecuteSqlScriptAsync(string scriptContent, string connectionString)
        {
            await using Microsoft.Data.SqlClient.SqlConnection scriptRunnerConnection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            var serverConnection = new ServerConnection(scriptRunnerConnection);
            serverConnection.ExecuteNonQuery(scriptContent);
        }
        internal static async Task ExecuteSqlScriptAsync(string scriptName, CommunicationWorkerOptions options)
        {
            string scriptContent = string.Format(await GetScriptTextAsync(scriptName), options.SchemaName, options.HubName);
            await ExecuteSqlScriptAsync(scriptContent, options.ConnectionString);
        }
        internal static async Task<string> GetScriptTextAsync(string scriptName, string schemaName, string hubName)
        {
            return string.Format(await GetScriptTextAsync(scriptName), schemaName, hubName);
        }
        internal static async Task<string> GetScriptTextAsync(string scriptName)
        {
            var assembly = typeof(Utility).Assembly;
            string assemblyName = assembly.GetName().Name;
            if (!scriptName.StartsWith(assemblyName))
                scriptName = $"{assembly.GetName().Name}.Scripts.{scriptName}";

            using Stream resourceStream = assembly.GetManifestResourceStream(scriptName);
            if (resourceStream == null)
                throw new ArgumentException($"Could not find assembly resource named '{scriptName}'.");
            using var reader = new StreamReader(resourceStream);
            return await reader.ReadToEndAsync();
        }

        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; private set; } = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        public static string BuildTableScript(Type type, string tableName = "", string defaultSchema = "")
        {
            List<string> cols = new List<string>();
            List<string> keys = new List<string>();
            if (string.IsNullOrEmpty(tableName))
                tableName = type.GetTableName();
            if (string.IsNullOrEmpty(defaultSchema))
                defaultSchema = type.GetSchemaName();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in properties)
            {
                string required;
                if (p.GetCustomAttribute<NotMappedAttribute>() != null)
                    continue;
                if (p.GetCustomAttribute<RequiredAttribute>() != null) required = "NOT NULL";
                else required = "NULL";
                cols.Add($"[{p.GetColumnName()}] {p.GetColumnType()} {required}");
                if (p.GetCustomAttribute<KeyAttribute>() != null)
                    keys.Add(p.GetColumnName());
            }
            if (keys.Count > 0)
            {
                cols.Add(@$"CONSTRAINT [PK_{defaultSchema}_{tableName}] PRIMARY KEY CLUSTERED ({string.Join(",", keys)})");
            }
            return $@"create table [{defaultSchema}].[{tableName}](
{string.Join("," + Environment.NewLine, cols)}
) ON [PRIMARY]";
        }

    }
}
