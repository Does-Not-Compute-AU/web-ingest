using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;

namespace WebIngest.Common.Extensions
{
    public static class DatabaseExtensions
    {
        public static IEnumerable<T> Select<T>(this IDataReader reader, Func<IDataReader, T> selector)
        {
            while (reader.Read())
            {
                yield return selector(reader);
            }
        }
        
        public static IEnumerable<object> ReadAll(this IDbCommand @this)
        {
            using (var reader = @this.ExecuteReader())
                foreach (var record in reader as IEnumerable)
                    yield return record;
        }

        public static IEnumerable<dynamic> SelectAll(this IDbCommand @this)
        {
            using (var dataReader = @this.ExecuteReader())
            {
                while (dataReader.Read())
                {
                    var row = new ExpandoObject() as IDictionary<string, object>;
                    for (var fieldCount = 0; fieldCount < dataReader.FieldCount; fieldCount++)
                    {
                        row.Add(dataReader.GetName(fieldCount), dataReader[fieldCount]);
                    }
                    yield return row;
                }
            }
        }
    }
}