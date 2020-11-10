using maskx.OrchestrationService.Worker;
using Microsoft.SqlServer.Management.Common;
using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.Utilities
{
    public static class Utility
    {
        internal static async Task ExecuteSqlScriptAsync(string scriptName, CommunicationWorkerOptions options)
        {
            string schemaCommands = string.Format(await GetScriptTextAsync(scriptName), options.SchemaName, options.HubName);
            await using Microsoft.Data.SqlClient.SqlConnection scriptRunnerConnection = new Microsoft.Data.SqlClient.SqlConnection(options.ConnectionString);
            var serverConnection = new ServerConnection(scriptRunnerConnection);
            serverConnection.ExecuteNonQuery(schemaCommands);
        }
        internal static async Task<string> GetScriptTextAsync(string scriptName)
        {
            var assembly = typeof(Utility).Assembly;
            string assemblyName = assembly.GetName().Name;
            if (!scriptName.StartsWith(assemblyName))
            {
                scriptName = $"{assembly.GetName().Name}.Scripts.{scriptName}";
            }

            using Stream resourceStream = assembly.GetManifestResourceStream(scriptName);
            if (resourceStream == null)
            {
                throw new ArgumentException($"Could not find assembly resource named '{scriptName}'.");
            }

            using var reader = new StreamReader(resourceStream);
            return await reader.ReadToEndAsync();
        }

        public static JsonSerializerOptions DefaultJsonSerializerOptions{get;private set;} = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
