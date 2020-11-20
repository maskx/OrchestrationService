using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

namespace maskx.OrchestrationService.Extensions
{
    public static class DataReaderExtensions
    {
        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> _PropertyInofCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        public static T CreateObject<T>(this DbDataReader dataReader) where T : new()
        {
            T newOjbect = new T();
            var type = typeof(T);
            if (!_PropertyInofCache.TryGetValue(type, out Dictionary<string, PropertyInfo> propertyDictionary))
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                propertyDictionary = new Dictionary<string, PropertyInfo>();
                foreach (var property in properties)
                {
                    if (!property.CanWrite) continue;
                    propertyDictionary.Add(property.Name, property);
                }
                _PropertyInofCache.TryAdd(type, propertyDictionary);
            }
            for (var i = 0; i < dataReader.FieldCount; i++)
            {
                var n = dataReader.GetName(i);
                if (!propertyDictionary.TryGetValue(n, out PropertyInfo prop)) continue;
                var val = dataReader.IsDBNull(i) ? null : dataReader.GetValue(i);
                prop.SetValue(newOjbect, val, null);
            }
            return newOjbect;
        }
    }
}
