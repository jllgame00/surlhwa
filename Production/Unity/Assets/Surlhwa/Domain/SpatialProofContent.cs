using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
namespace Surlhwa.Domain
{
    public sealed class GuestIdentity { }
    public sealed class TextIdentity { }
    public sealed class VisualIdentity { }
    public readonly struct StableId<T> : IEquatable<StableId<T>>
    {
        public string Value { get; }
        public StableId(string value)
        {
            if (value == null || !Regex.IsMatch(value, @"\A[a-z][a-z0-9_]*\z"))
                throw new ArgumentException("Stable IDs require lowercase ASCII letters, digits and underscores.", nameof(value));
            Value = value;
        }
        public bool Equals(StableId<T> other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object other) => other is StableId<T> id && Equals(id);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? "<uninitialized>";
    }
    public enum GuestKind { Bartender, Ordinary, Special }
    public sealed class GuestDefinition
    {
        public StableId<GuestIdentity> Id { get; }
        public GuestKind Kind { get; }
        public StableId<TextIdentity> DisplayTextId { get; }
        public StableId<VisualIdentity> VisualBindingId { get; }
        public GuestDefinition(StableId<GuestIdentity> id, GuestKind kind,
            StableId<TextIdentity> displayTextId, StableId<VisualIdentity> visualBindingId)
        {
            if (id.Value == null || displayTextId.Value == null || visualBindingId.Value == null)
                throw new ArgumentException("Uninitialized IDs cannot be published.");
            if (!Enum.IsDefined(typeof(GuestKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            Id = id; Kind = kind; DisplayTextId = displayTextId; VisualBindingId = visualBindingId;
        }
    }
    public sealed class SpatialProofContent
    {
        public IReadOnlyDictionary<StableId<GuestIdentity>, GuestDefinition> Guests { get; }
        public IReadOnlyDictionary<StableId<TextIdentity>, string> Texts { get; }
        public IReadOnlyCollection<StableId<VisualIdentity>> VisualBindings { get; }
        public string ContentRevision { get; }
        public SpatialProofContent(string revision,
            IDictionary<StableId<GuestIdentity>, GuestDefinition> guests,
            IDictionary<StableId<TextIdentity>, string> texts,
            ICollection<StableId<VisualIdentity>> visuals)
        {
            if (string.IsNullOrWhiteSpace(revision)) throw new ArgumentException("Content revision is required.");
            ContentRevision = revision;
            Guests = new ReadOnlyDictionary<StableId<GuestIdentity>, GuestDefinition>(
                new Dictionary<StableId<GuestIdentity>, GuestDefinition>(guests));
            Texts = new ReadOnlyDictionary<StableId<TextIdentity>, string>(
                new Dictionary<StableId<TextIdentity>, string>(texts));
            VisualBindings = new ReadOnlyCollection<StableId<VisualIdentity>>(new List<StableId<VisualIdentity>>(visuals));
        }
    }
}

