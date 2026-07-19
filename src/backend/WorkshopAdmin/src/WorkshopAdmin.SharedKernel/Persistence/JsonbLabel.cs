using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace WorkshopAdmin.SharedKernel.Persistence;

/// <summary>
/// Multi-language JSONB labels (<c>{"en": "...", "sr": "..."}</c>) map to
/// <c>Dictionary&lt;string, string&gt;</c> — same shape the legacy backend used.
/// </summary>
public static class JsonbLabel
{
    public static readonly ValueConverter<Dictionary<string, string>, string> Converter = new(
        label => JsonSerializer.Serialize(label, (JsonSerializerOptions?)null),
        json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)
                ?? new Dictionary<string, string>());

    public static readonly ValueComparer<Dictionary<string, string>> Comparer = new(
        (left, right) => left == null
            ? right == null
            : right != null && left.Count == right.Count && !left.Except(right).Any(),
        label => label.Aggregate(0, (hash, pair) => HashCode.Combine(hash, pair.Key, pair.Value)),
        label => new Dictionary<string, string>(label));

    /// <summary>Map a label property to its jsonb column: <c>e.Property(x =&gt; x.Label).HasJsonbLabel()</c>.</summary>
    public static PropertyBuilder<Dictionary<string, string>> HasJsonbLabel(
        this PropertyBuilder<Dictionary<string, string>> builder) =>
        builder.HasColumnType("jsonb").HasConversion(Converter, Comparer);
}
