using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Surlhwa.Domain;
namespace Surlhwa.ContentIO
{
    public sealed class ContentValidationException : Exception
    {
        public ContentValidationException(string message) : base(message) { }
    }
    public static class SpatialProofLoader
    {
        public static SpatialProofContent Load(string json)
        {
            try
            {
                if (json == null) throw Invalid("Content cannot be null.");
                JObject root;
                using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None, MaxDepth = 16 })
                {
                    root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                    if (reader.Read()) throw Invalid("Trailing JSON is forbidden.");
                }
                Fields(root, "schemaVersion", "contentRevision", "mode", "visualBindings", "texts", "guests");
                if (root["schemaVersion"].Type != JTokenType.Integer || root["schemaVersion"].Value<long>() != 1)
                    throw Invalid("Expected integer schemaVersion 1.");
                if (String(root, "mode") != "spatial_proof") throw Invalid("Unknown content mode.");
                string revision = String(root, "contentRevision");
                if (string.IsNullOrWhiteSpace(revision)) throw Invalid("Empty content revision.");
                var visuals = new HashSet<StableId<VisualIdentity>>();
                foreach (var entry in Array(root, "visualBindings"))
                {
                    var item = Object(entry); Fields(item, "id");
                    if (!visuals.Add(Id<VisualIdentity>(item, "id"))) throw Invalid("Duplicate visual ID.");
                }
                var texts = new Dictionary<StableId<TextIdentity>, string>();
                foreach (var entry in Array(root, "texts"))
                {
                    var item = Object(entry); Fields(item, "id", "value");
                    var id = Id<TextIdentity>(item, "id");
                    if (texts.ContainsKey(id)) throw Invalid("Duplicate text ID: " + id);
                    texts.Add(id, String(item, "value"));
                }
                var guests = new Dictionary<StableId<GuestIdentity>, GuestDefinition>();
                foreach (var entry in Array(root, "guests"))
                {
                    var item = Object(entry); Fields(item, "id", "kind", "displayTextId", "visualBindingId");
                    var id = Id<GuestIdentity>(item, "id");
                    var text = Id<TextIdentity>(item, "displayTextId");
                    var visual = Id<VisualIdentity>(item, "visualBindingId");
                    string kindText = String(item, "kind");
                    GuestKind kind;
                    switch (kindText)
                    {
                        case "bartender": kind = GuestKind.Bartender; break;
                        case "ordinary": kind = GuestKind.Ordinary; break;
                        case "special": kind = GuestKind.Special; break;
                        default: throw Invalid("Unknown guest kind: " + kindText);
                    }
                    if (!texts.ContainsKey(text)) throw Invalid("Missing text reference: " + text);
                    if (!visuals.Contains(visual)) throw Invalid("Missing visual reference: " + visual);
                    if (guests.ContainsKey(id)) throw Invalid("Duplicate guest ID: " + id);
                    guests.Add(id, new GuestDefinition(id, kind, text, visual));
                }
                return new SpatialProofContent(revision, guests, texts, visuals);
            }
            catch (ContentValidationException) { throw; }
            catch (Exception e) when (e is JsonException || e is ArgumentException || e is OverflowException)
            { throw Invalid("Invalid spatial proof content: " + e.Message); }
        }

        public static string Write(SpatialProofContent content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            var root = new JObject {
                ["schemaVersion"] = 1, ["contentRevision"] = content.ContentRevision, ["mode"] = "spatial_proof",
                ["visualBindings"] = new JArray(content.VisualBindings.OrderBy(x => x.Value, StringComparer.Ordinal)
                    .Select(x => new JObject { ["id"] = x.Value })),
                ["texts"] = new JArray(content.Texts.OrderBy(x => x.Key.Value, StringComparer.Ordinal)
                    .Select(x => new JObject { ["id"] = x.Key.Value, ["value"] = x.Value })),
                ["guests"] = new JArray(content.Guests.Values.OrderBy(x => x.Id.Value, StringComparer.Ordinal)
                    .Select(x => new JObject { ["id"] = x.Id.Value, ["kind"] = x.Kind.ToString().ToLowerInvariant(),
                        ["displayTextId"] = x.DisplayTextId.Value, ["visualBindingId"] = x.VisualBindingId.Value }))
            };
            return root.ToString(Formatting.Indented);
        }
        private static ContentValidationException Invalid(string message) => new ContentValidationException(message);
        private static JObject Object(JToken token) => token as JObject ?? throw Invalid("Expected object.");
        private static JArray Array(JObject item, string field) => item[field] as JArray ?? throw Invalid("Expected array: " + field);
        private static string String(JObject item, string field)
        {
            if (item[field]?.Type != JTokenType.String) throw Invalid("Expected string: " + field);
            return item[field].Value<string>();
        }
        private static StableId<T> Id<T>(JObject item, string field) => new StableId<T>(String(item, field));
        private static void Fields(JObject item, params string[] allowed)
        {
            foreach (string name in allowed) if (item.Property(name, StringComparison.Ordinal) == null) throw Invalid("Missing field: " + name);
            foreach (var property in item.Properties()) if (!allowed.Contains(property.Name, StringComparer.Ordinal)) throw Invalid("Unknown field: " + property.Name);
        }
    }
}

