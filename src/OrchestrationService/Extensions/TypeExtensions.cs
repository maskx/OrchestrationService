using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace maskx.OrchestrationService.Extensions
{
    public static class TypeExtensions
    {
        public static string GetSchemaName(this Type type, string defaultSchema = "dbo")
        {
            var table = type.GetCustomAttribute<TableAttribute>();
            if (table == null || string.IsNullOrEmpty(table.Schema))
                return defaultSchema;
            return table.Schema;
        }
        public static string GetTableName(this Type type)
        {
            var table = type.GetCustomAttribute<TableAttribute>();
            if (table != null)
                return table.Name;
            return type.Name;
        }
        public static string GetFullTableName(this Type type, string defaultSchema = "dbo")
        {
            var table = type.GetCustomAttribute<TableAttribute>();
            string schemaName = string.IsNullOrEmpty(defaultSchema) ? "dbo" : defaultSchema;
            string tableName;
            if (table == null)
            {
                tableName = type.Name;
            }
            else
            {
                tableName = table.Name;
                if (!string.IsNullOrEmpty(table.Schema))
                    schemaName = table.Schema;
            }
            return $"[{schemaName}].[{tableName}]";
        }
    }
}
