﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace maskx.OrchestrationService.Extensions
{
    public static class PropertyInfoExtensions
    {
        public static string GetColumnName(this PropertyInfo propertyInfo)
        {
            var column = propertyInfo.GetCustomAttribute<ColumnAttribute>();
            if (column != null && !string.IsNullOrEmpty(column.Name))
                return column.Name;
            return propertyInfo.Name;
        }
        public static string GetColumnType(this PropertyInfo propertyInfo)
        {
            var column = propertyInfo.GetCustomAttribute<ColumnAttribute>();
            if (column != null && !string.IsNullOrEmpty(column.TypeName))
                return column.TypeName;
            return GetDbType(propertyInfo);
        }
        static string GetDbType(PropertyInfo propertyInfo)
        {
            string c = string.Empty;
            switch (propertyInfo.PropertyType.Name)
            {
                case "String":
                    var maxLen = propertyInfo.GetCustomAttribute<MaxLengthAttribute>();
                    var strLen = propertyInfo.GetCustomAttribute<StringLengthAttribute>();
                    c = "nvarchar(";
                    if (strLen != null) c += strLen.MaximumLength;
                    else if (maxLen != null) c += maxLen.Length;
                    else c += "max";
                    c += ")";
                    break;
                case "bool":
                    c = "bit";
                    break;
                case "DateTime":
                    c = "[datetime2](7)";
                    break;
                case "Int32":
                    c = "int";
                    break;
                case "double":
                    c = "float";
                    break;
                case "long":
                    c = "double";
                    break;
                case "decimal":
                    c = "decimal(38,6)";
                    break;
                default:
                    if (propertyInfo.PropertyType.IsEnum) c = "int";
                    break;
            }
            return c;
        }
    }
}
