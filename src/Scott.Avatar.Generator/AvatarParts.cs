using System.Collections.Frozen;

namespace Scott.Avatar.Generator;

/// <summary>
/// Raw SVG fragment templates for every avatar part, keyed by the style name used
/// in the set's configuration.
/// </summary>
/// <remarks>
/// The fragments live as plain <c>.svg</c> files under the repository's set folder
/// (one sub-folder per category, e.g. <c>personas/Hair/Mohawk.svg</c>) so designers
/// can edit them without touching code. They are embedded into this assembly at build
/// time, so the library remains self-contained at runtime. Each template keeps the
/// <c>$fill</c>/<c>$size</c> placeholders, which <see cref="Render"/> substitutes.
/// </remarks>
public static class AvatarParts
{
    private static readonly Lazy<FrozenDictionary<string, string>> LazyTemplates = new(Load);

    /// <summary>Style name -> SVG template (with <c>$fill</c>/<c>$size</c> placeholders).</summary>
    public static FrozenDictionary<string, string> Templates => LazyTemplates.Value;

    private static FrozenDictionary<string, string> Load()
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var resource in EmbeddedResources.Names)
        {
            if (!resource.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                continue;

            // Embedded as e.g. "Scott.Avatar.Generator.personas.Hair.Mohawk.svg"; the key
            // is the file's stem ("Mohawk"). Style names are unique across categories.
            var withoutExtension = resource[..^4];
            var key = withoutExtension[(withoutExtension.LastIndexOf('.') + 1)..];
            dict[key] = EmbeddedResources.Read(resource);
        }

        return dict.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>
    /// Renders a single avatar part to a standalone SVG string, substituting the
    /// requested fill colour and pixel size. Returns an empty string for unknown
    /// or intentionally empty styles (e.g. facial-hair <c>None</c>), exactly as the
    /// original loader did.
    /// </summary>
    /// <param name="name">Style name, e.g. <c>Skin</c>, <c>Bobcut</c>, <c>Sunglasses</c>.</param>
    /// <param name="fill">Fill colour including any leading <c>#</c>, e.g. <c>#B16A5B</c>.</param>
    /// <param name="size">Pixel size for the SVG width/height attributes.</param>
    public static string Render(string name, string fill = "#000", string size = "64")
    {
        if (name is null || !Templates.TryGetValue(name, out var template))
            return string.Empty;
        return template.Replace("$fill", fill).Replace("$size", size);
    }
}
