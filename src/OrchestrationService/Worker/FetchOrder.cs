using maskx.OrchestrationService.Utilities;
using System.Collections.Generic;
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
        public static string Serialize(this List<FetchOrder> fetchOrders)
        {
            return JsonSerializer.Serialize(fetchOrders, Utility.DefaultJsonSerializerOptions);
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
