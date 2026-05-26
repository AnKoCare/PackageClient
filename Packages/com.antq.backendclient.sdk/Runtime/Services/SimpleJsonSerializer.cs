using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace GameBackendModule.Services
{
    public static class SimpleJsonSerializer
    {
        public static string ToJson(object value)
        {
            StringBuilder builder = new StringBuilder(256);
            WriteValue(builder, value);
            return builder.ToString();
        }

        private static void WriteValue(StringBuilder builder, object value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            switch (value)
            {
                case string s:
                    WriteString(builder, s);
                    return;
                case bool b:
                    builder.Append(b ? "true" : "false");
                    return;
                case int or long or float or double or decimal:
                    builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    return;
                case IDictionary<string, object> dict:
                    WriteDictionary(builder, dict);
                    return;
                case IDictionary nonGenericDict:
                    WriteDictionary(builder, ToStringObjectDict(nonGenericDict));
                    return;
                case IEnumerable enumerable when value is not string:
                    WriteArray(builder, enumerable);
                    return;
                default:
                    // Serialize POCO bằng reflection để hỗ trợ field Dictionary
                    WritePoco(builder, value);
                    return;
            }
        }

        private static void WriteString(StringBuilder builder, string s)
        {
            builder.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            builder.AppendFormat("\\u{0:X4}", (int)c);
                        }
                        else
                        {
                            builder.Append(c);
                        }
                        break;
                }
            }
            builder.Append('"');
        }

        private static void WriteArray(StringBuilder builder, IEnumerable array)
        {
            builder.Append('[');
            bool first = true;
            foreach (object item in array)
            {
                if (!first) builder.Append(',');
                first = false;
                WriteValue(builder, item);
            }
            builder.Append(']');
        }

        private static void WriteDictionary(StringBuilder builder, IDictionary<string, object> dict)
        {
            builder.Append('{');
            bool first = true;
            foreach (KeyValuePair<string, object> kv in dict)
            {
                if (!first) builder.Append(',');
                first = false;
                WriteString(builder, kv.Key);
                builder.Append(':');
                WriteValue(builder, kv.Value);
            }
            builder.Append('}');
        }

        private static void WritePoco(StringBuilder builder, object poco)
        {
            if (poco == null)
            {
                builder.Append("null");
                return;
            }

            Type type = poco.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            Dictionary<string, object> dict = new Dictionary<string, object>(fields.Length);
            foreach (FieldInfo field in fields)
            {
                object val = field.GetValue(poco);
                dict[field.Name] = val;
            }
            WriteDictionary(builder, dict);
        }

        private static Dictionary<string, object> ToStringObjectDict(IDictionary dict)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (DictionaryEntry entry in dict)
            {
                string key = entry.Key?.ToString() ?? string.Empty;
                result[key] = entry.Value;
            }
            return result;
        }
    }
}


