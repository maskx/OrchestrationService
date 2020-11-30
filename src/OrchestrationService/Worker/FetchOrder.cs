using maskx.OrchestrationService.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace maskx.OrchestrationService.Worker
{
    public class FetchOrder
    {
        public string Field { get; set; }
        public string Order { get; set; } = "ASC";
    }
    public static class FetchOrderListExtension
    {
        public static bool TrySerialize(this List<FetchOrder> fetchOrders,Type type,out string result)
        {
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartArray();
            foreach (var f in fetchOrders)
            {
                if (!Utility.GetPropertyInfos(type).TryGetValue(f.Field.ToLower(), out PropertyInfo p))
                {
                    result = $"{f.Field} is not a validate column name";
                    return false;
                }
                if (!(string.IsNullOrEmpty(f.Order) || f.Order.ToLower()=="asc" || f.Order.ToLower()=="desc"))
                {
                    result = $"{f.Order} is not a validate operator";
                    return false;
                }
                writer.WriteStartObject();
                writer.WriteString("field",f.Field);
                writer.WriteString("order",f.Order);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.Flush();
            result = Encoding.UTF8.GetString(ms.ToArray());
            return true;
        }
        public static List<FetchOrder> DeserializeFetchOrderList(this string rawString)
        {
            if (string.IsNullOrEmpty(rawString))
                return null;
            return JsonSerializer.Deserialize<List<FetchOrder>>(rawString, Utility.DefaultJsonSerializerOptions);
        }
        public static bool IsValid(this List<FetchOrder> fetchOder)
        {
            return true;
        }
    }
}
