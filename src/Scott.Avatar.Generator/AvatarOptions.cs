using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Scott.Avatar.Generator;

/// <summary>
/// A fully-specified avatar: one chosen style and colour per part. Mirrors the
/// original <c>Types.styles</c> record. Colours are hex strings <em>without</em>
/// a leading <c>#</c> (e.g. <c>"B16A5B"</c>), exactly as in the original config.
/// </summary>
public sealed record AvatarOptions
{
    /// <summary>Skin style. Only <c>Skin</c> exists in the stock catalogue.</summary>
    public string Skin { get; init; } = "Skin";
    /// <summary>Skin colour (hex, no <c>#</c>). Also used to fill the nose.</summary>
    public string SkinColor { get; init; } = "B16A5B";

    /// <summary>Hair (or hat) style.</summary>
    public string Hair { get; init; } = "Balding";
    /// <summary>Hair colour (hex, no <c>#</c>).</summary>
    public string HairColor { get; init; } = "E16381";

    /// <summary>Facial-hair style, or <c>None</c>.</summary>
    public string FacialHair { get; init; } = "Mustache";
    /// <summary>Facial-hair colour (hex, no <c>#</c>).</summary>
    public string FacialHairColor { get; init; } = "6C4545";

    /// <summary>Body (shoulders) style.</summary>
    public string Body { get; init; } = "Square";
    /// <summary>Body colour (hex, no <c>#</c>).</summary>
    public string BodyColor { get; init; } = "5A45FF";

    /// <summary>Eye style. Eyes always render in their own fixed colours.</summary>
    public string Eyes { get; init; } = "Glasses";

    /// <summary>Mouth style. Mouths always render in their own fixed colours.</summary>
    public string Mouth { get; init; } = "Pacifier";

    /// <summary>Nose style. The nose is filled with the skin colour.</summary>
    public string Nose { get; init; } = "Smallround";

    /// <summary>Background colour (hex, no <c>#</c>).</summary>
    public string BgColor { get; init; } = "FFCC65";

    /// <summary>The library's default avatar (identical to the original app's defaults).</summary>
    public static AvatarOptions Default => new();

    /// <summary>
    /// Produces a random avatar by picking a random style and colour for each part
    /// from the supplied configuration. Mirrors the original <c>randomizeStyles</c>.
    /// </summary>
    /// <param name="config">Catalogue to pick from. Defaults to <see cref="AvatarConfig.Default"/>.</param>
    /// <param name="random">RNG to use. Defaults to <see cref="System.Random.Shared"/> (pass a seeded instance for reproducibility).</param>
    public static AvatarOptions Random(AvatarConfig? config = null, Random? random = null)
    {
        config ??= AvatarConfig.Default;
        random ??= System.Random.Shared;
        return Build((_, list) => list[random.Next(list.Count)], config);
    }

    /// <summary>
    /// Produces a <em>deterministic</em> avatar from an arbitrary key (e.g. a username,
    /// e-mail or user id): the same key always yields the same avatar, different keys
    /// almost always differ. Ideal for stable profile-photo placeholders.
    /// </summary>
    /// <remarks>
    /// Each part is chosen by hashing the key (SHA-256), so the result is stable across
    /// processes, machines and library versions — unlike <see cref="string.GetHashCode()"/>
    /// or <see cref="System.Random"/> internals.
    /// </remarks>
    /// <param name="key">The string to derive the avatar from. <c>null</c> is treated as empty.</param>
    /// <param name="config">Catalogue to pick from. Defaults to <see cref="AvatarConfig.Default"/>.</param>
    public static AvatarOptions FromString(string key, AvatarConfig? config = null)
    {
        config ??= AvatarConfig.Default;
        key ??= string.Empty;

        return Build((field, list) =>
        {
            // Hash "<field>\0<key>" so each part varies independently for the same key.
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(Encoding.UTF8.GetBytes($"{field}\0{key}"), hash);
            var index = BinaryPrimitives.ReadUInt32BigEndian(hash) % (uint)list.Count;
            return list[(int)index];
        }, config);
    }

    /// <summary>Assembles an avatar by asking <paramref name="pick"/> to choose one entry from each list.</summary>
    /// <param name="pick">Chooses an item given a stable field name and the candidate list.</param>
    /// <param name="config">Catalogue supplying the candidate styles and colours.</param>
    private static AvatarOptions Build(Func<string, IReadOnlyList<string>, string> pick, AvatarConfig config) =>
        new()
        {
            Skin = "Skin",
            SkinColor = pick("skinColor", config.SkinColors),
            HairColor = pick("hairColor", config.HairColors),
            Hair = pick("hair", config.HairStyles),
            FacialHair = pick("facialHair", config.FacialHairStyles),
            FacialHairColor = pick("facialHairColor", config.FacialHairColors),
            Body = pick("body", config.BodyStyles),
            BodyColor = pick("bodyColor", config.BodyColors),
            Eyes = pick("eyes", config.EyeStyles),
            Mouth = pick("mouth", config.MouthStyles),
            Nose = pick("nose", config.NoseStyles),
            BgColor = pick("bgColor", config.BgColors),
        };
}
