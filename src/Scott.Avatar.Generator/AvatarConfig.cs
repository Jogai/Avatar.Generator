using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scott.Avatar.Generator;

/// <summary>
/// The catalogue for an avatar set: the available styles and colour palettes for
/// each part, plus attribution metadata. Loaded from the set's <c>config.json</c>
/// (the bundled set is <c>personas/config.json</c>).
/// </summary>
public sealed record AvatarConfig
{
    /// <summary>Skin styles (the original ships a single <c>Skin</c> style).</summary>
    public required IReadOnlyList<string> SkinStyles { get; init; }
    /// <summary>Hair styles, including hats which render in the hair layer.</summary>
    public required IReadOnlyList<string> HairStyles { get; init; }
    /// <summary>Facial-hair styles, including <c>None</c>.</summary>
    public required IReadOnlyList<string> FacialHairStyles { get; init; }
    /// <summary>Body (shoulders) styles.</summary>
    public required IReadOnlyList<string> BodyStyles { get; init; }
    /// <summary>Eye styles.</summary>
    public required IReadOnlyList<string> EyeStyles { get; init; }
    /// <summary>Mouth styles.</summary>
    public required IReadOnlyList<string> MouthStyles { get; init; }
    /// <summary>Nose styles.</summary>
    public required IReadOnlyList<string> NoseStyles { get; init; }
    /// <summary>Background styles (the original ships a single <c>Background</c> style).</summary>
    public required IReadOnlyList<string> BgStyles { get; init; }

    /// <summary>Skin colour palette (hex without leading <c>#</c>).</summary>
    public required IReadOnlyList<string> SkinColors { get; init; }
    /// <summary>Hair colour palette.</summary>
    public required IReadOnlyList<string> HairColors { get; init; }
    /// <summary>Facial-hair colour palette.</summary>
    public required IReadOnlyList<string> FacialHairColors { get; init; }
    /// <summary>Body colour palette.</summary>
    public required IReadOnlyList<string> BodyColors { get; init; }
    /// <summary>Background colour palette.</summary>
    public required IReadOnlyList<string> BgColors { get; init; }
    /// <summary>Palette used to render disabled (non-colourable) swatches.</summary>
    public required IReadOnlyList<string> DisabledColors { get; init; }

    /// <summary>Attribution for this asset set (its origin, author, and licence). Optional.</summary>
    public SetAttribution? Attribution { get; init; }

    private static readonly Lazy<AvatarConfig> LazyDefault = new(LoadEmbedded);

    /// <summary>The configuration shipped with the library (the original palette and styles).</summary>
    public static AvatarConfig Default => LazyDefault.Value;

    /// <summary>Parses a <see cref="AvatarConfig"/> from a JSON string in the original schema.</summary>
    /// <remarks>Uses a source-generated serializer, so it is reflection-free and AOT/trim safe.</remarks>
    public static AvatarConfig FromJson(string json)
        => JsonSerializer.Deserialize(json, AvatarConfigJsonContext.Default.AvatarConfig)
           ?? throw new JsonException("config.json deserialised to null");

    private static AvatarConfig LoadEmbedded()
    {
        var name = EmbeddedResources.Names.Single(n => n.EndsWith("config.json", StringComparison.Ordinal));
        return FromJson(EmbeddedResources.Read(name));
    }
}

/// <summary>
/// Attribution metadata for an avatar set — credits the original work the artwork
/// came from. Surfaced via <see cref="AvatarConfig.Attribution"/> and the API's
/// <c>/config</c> endpoint.
/// </summary>
public sealed record SetAttribution
{
    /// <summary>Display name of the set, e.g. <c>Personas</c>.</summary>
    public string? Name { get; init; }
    /// <summary>A short description of the set.</summary>
    public string? Description { get; init; }
    /// <summary>The original author or organisation.</summary>
    public string? Author { get; init; }
    /// <summary>URL of the source repository the artwork was ported from.</summary>
    public string? Source { get; init; }
    /// <summary>URL of the original project's website.</summary>
    public string? Website { get; init; }
    /// <summary>SPDX licence identifier of the original artwork, e.g. <c>MIT</c>.</summary>
    public string? License { get; init; }
}

/// <summary>
/// Source-generated JSON metadata for <see cref="AvatarConfig"/>, so the library
/// deserialises catalogues without reflection and stays trim/AOT safe.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(AvatarConfig))]
internal sealed partial class AvatarConfigJsonContext : JsonSerializerContext;
