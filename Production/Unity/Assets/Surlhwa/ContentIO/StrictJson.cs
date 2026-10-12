using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Surlhwa.ContentIO
{
    internal static class StrictJson
    {
        internal static ContentValidationException Invalid(string message) => new ContentValidationException(message);
        internal static JObject Parse(string json)
        {
            if(json==null || json.Length>4000000) throw Invalid("Missing or oversized JSON document.");
            using(var reader=new JsonTextReader(new StringReader(json)){DateParseHandling=DateParseHandling.None,FloatParseHandling=FloatParseHandling.Decimal,MaxDepth=32})
            {
                var obj=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
                if(reader.Read()) throw Invalid("Trailing JSON is forbidden.");
                return obj;
            }
        }
        internal static void Fields(JObject obj,params string[] fields)
        {
            foreach(var f in fields) if(obj.Property(f,StringComparison.Ordinal)==null) throw Invalid("Missing field: "+f);
            foreach(var p in obj.Properties()) if(!fields.Contains(p.Name,StringComparer.Ordinal)) throw Invalid("Unknown field: "+p.Name);
        }
        internal static JObject Object(JToken token) => token as JObject ?? throw Invalid("Expected object.");
        internal static JArray Array(JObject obj,string key) => obj[key] as JArray ?? throw Invalid("Expected array: "+key);
        internal static string String(JObject obj,string key)
        { if(obj[key]?.Type!=JTokenType.String || string.IsNullOrWhiteSpace(obj[key].Value<string>())) throw Invalid("Expected nonempty string: "+key); return obj[key].Value<string>(); }
        internal static string NullableString(JObject obj,string key) => obj[key]?.Type==JTokenType.Null ? null : String(obj,key);
        internal static bool Bool(JObject obj,string key)
        { if(obj[key]?.Type!=JTokenType.Boolean) throw Invalid("Expected boolean: "+key); return obj[key].Value<bool>(); }
        internal static int Int(JObject obj,string key)
        { if(obj[key]?.Type!=JTokenType.Integer) throw Invalid("Expected integer: "+key); return obj[key].Value<int>(); }
        internal static decimal Number(JObject obj,string key)
        { if(obj[key]?.Type!=JTokenType.Float && obj[key]?.Type!=JTokenType.Integer) throw Invalid("Expected decimal number: "+key); return obj[key].Value<decimal>(); }
        internal static string[] Strings(JObject obj,string key)
        { return Array(obj,key).Select(x=>x.Type==JTokenType.String && !string.IsNullOrWhiteSpace(x.Value<string>()) ? x.Value<string>() : throw Invalid("Expected string item: "+key)).ToArray(); }
        internal static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value));
        internal static string Hash(byte[] value) { using(var h=SHA256.Create()) return BitConverter.ToString(h.ComputeHash(value)).Replace("-","").ToLowerInvariant(); }
        internal static void Schema(JObject root) { if(Int(root,"schemaVersion")!=1) throw Invalid("Unsupported content schema."); }
    }
}
