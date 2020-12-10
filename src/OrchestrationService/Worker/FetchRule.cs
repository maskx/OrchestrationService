using maskx.OrchestrationService.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace maskx.OrchestrationService.Worker
{
    public class FetchRule
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedTimeUtc { get; set; }
        public DateTime UpdatedTimeUtc { get; set; }
        /// <summary>
        /// 需要限制并发请求的内容，如ServicType,RequestMethod，Operation
        /// </summary>
        public List<Where> What { get; set; } = new List<Where>();

        /// <summary>
        /// 并发请求的上限
        /// </summary>
        public int Concurrency { get; set; }

        /// <summary>
        /// 限制并发请求的范围，如Subscription、ManagementUnit
        /// </summary>
        public List<string> Scope { get; set; } = new List<string>();

        /// <summary>
        /// 设置优先级，如不设置则使用默认优先级
        /// </summary>
        public List<FetchOrder> FetchOrder { get; set; } = new List<FetchOrder>();

    }
    public static class FetchRuleExtension
    {
        public static List<Where> DeserializeWhat(this string str, Type type)
        {
            List<Where> ws = new List<Where>();
            using JsonDocument json = JsonDocument.Parse(str);
            foreach (var item in json.RootElement.EnumerateArray())
            {
                string name = item.GetProperty("name").GetString();
                string value = "";
                if (!Utility.GetPropertyInfos(type).TryGetValue(name.ToLower(), out PropertyInfo p))
                    continue;
                var vp = item.GetProperty("value");
                if (vp.ValueKind == JsonValueKind.String)
                {
                    value = vp.GetString();
                    if (p.PropertyType.Name == "String") value = value[2..^1].Replace("''", "'");
                    else value = value[1..^1];
                }
                else if (vp.ValueKind == JsonValueKind.Number)
                {
                    value = vp.GetRawText();
                }
                else
                    throw new NotSupportedException($"not supported data type in the value of property:{name}");

                ws.Add(new Where()
                {
                    Name = name,
                    Operator = item.GetProperty("operator").GetString(),
                    Value = value
                }); ;
            }
            return ws;
        }
        public static bool TrySerializeWhat(this List<Where> what, Type type, out string result)
        {
            if (what == null || what.Count == 0)
            {
                result = "[]";
                return true;
            }
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartArray();
            foreach (var w in what)
            {
                if (!Utility.GetPropertyInfos(type).TryGetValue(w.Name.ToLower(), out PropertyInfo p))
                {
                    result = $"{w.Name} is not a validate column name";
                    return false;
                }
                if (!Where.ValidationOperation.Contains(w.Operator))
                {
                    result = $"{w.Operator} is not a validate operator";
                    return false;
                }
                writer.WriteStartObject();
                writer.WriteString("name", w.Name);
                writer.WriteString("operator", w.Operator);
                writer.WritePropertyName("value");
                switch (p.PropertyType.Name)
                {
                    case "String":
                        writer.WriteStringValue(JsonEncodedText.Encode($"N'{w.Value.Replace("'", "''")}'", JavaScriptEncoder.UnsafeRelaxedJsonEscaping));
                        break;
                    case "Boolean":
                    case "Int32":
                        if (!int.TryParse(w.Value, out int int32))
                        {
                            result = $"{w.Name} need a Int32 value";
                            return false;
                        }
                        writer.WriteNumberValue(int32);
                        break;
                    case "Int64":
                        if (!long.TryParse(w.Value, out long int64))
                        {
                            result = $"{w.Name} need a Int64 value";
                            return false;
                        }
                        writer.WriteNumberValue(int64);
                        break;
                    case "Decimal":
                        if (!decimal.TryParse(w.Value, out decimal dec))
                        {
                            result = $"{w.Name} need a decimal value";
                            return false;
                        }
                        writer.WriteNumberValue(dec);
                        break;
                    case "Double":
                        if (!double.TryParse(w.Value, out double dou))
                        {
                            result = $"{w.Name} need a double value";
                            return false;
                        }
                        writer.WriteNumberValue(dou);
                        break;
                    case "DateTime":
                        if (!Regex.IsMatch(w.Value, @"^\d{4}-(0?[1-9]|1[0-2])-(0?[1-9]|[12][0-9]|3[01])( (00|[0-9]|1[0-9]|2[0-3]):([0-9]|[0-5][0-9])((:([0-9]|[0-5][0-9])(.\d{1,7})?)?)?)?$"))
                        {
                            result = $"{w.Name} need datetime value with format 'YYYY-MM-DD hh:mm:ss.nnnnnnn'";
                            return false;
                        }
                        writer.WriteStringValue($"'{w.Value}'");
                        break;
                    case "Guid":
                        if (w.Value.Length != 36 || !Guid.TryParse(w.Value, out Guid g))
                        {
                            result = $"{w.Name} need Guid value";
                            return false;
                        }
                        writer.WriteStringValue($"'{w.Value}'");
                        break;
                    default:
                        if (p.PropertyType.IsEnum && int.TryParse(w.Value,out int enumInt))
                        {
                            writer.WriteNumberValue(enumInt);
                        }
                        else
                        {
                            result = $"does not support data type:{p.PropertyType.Name} for {w.Name} column";
                            return false;
                        }
                        break;
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.Flush();
            result = Encoding.UTF8.GetString(ms.ToArray());
            return true;
        }
        public static bool TrySerializeScope(this List<string> scope, Type type, out string result)
        {
            if (scope == null || scope.Count == 0)
            {
                result = "[]";
                return true;
            }
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartArray();
            foreach (var c in scope)
            {
                if (!Utility.GetPropertyInfos(type).TryGetValue(c.ToLower(), out PropertyInfo p))
                {
                    result = $"{c} is not a validate column name";
                    return false;
                }
                writer.WriteStringValue(c);
            }
            writer.WriteEndArray();
            writer.Flush();
            result = Encoding.UTF8.GetString(ms.ToArray());
            return true;
        }
        public static List<string> DeserializeScope(this string scope)
        {
            return JsonSerializer.Deserialize<List<string>>(scope, Utility.DefaultJsonSerializerOptions);
        }
        public static string Validate(this FetchRule rule, Type type, out Dictionary<string, object> par)
        {
            par = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(rule.Name))
                return "Name cannot be empty";
            if(rule.What.Count==0 && rule.Scope.Count==0)
                return "What and Scope cannot be empty at same time";
            if (!rule.What.TrySerializeWhat(type, out string what))
                return what;
            if (!rule.Scope.TrySerializeScope(type, out string scope))
                return scope;
            if (!rule.FetchOrder.TrySerialize(type, out string fetcheOrder))
                return fetcheOrder;
            par["Name"] = rule.Name;
            par["Description"] = rule.Description;
            par["What"] = what;
            par["Scope"] = scope;
            par["Concurrency"] = rule.Concurrency;
            par["FetchOrder"] = fetcheOrder;
            return string.Empty;
        }
    }
}