using System.Text;
using System.Text.RegularExpressions;

namespace Scott.Avatar.Generator;

/// <summary>
/// Generates Personas avatars as SVG strings. The original app rendered each part
/// as a separate, absolutely-positioned <c>&lt;svg&gt;</c> stacked by z-index; this
/// port composes the same parts into a single, standalone SVG document by nesting
/// each part (sized to the shared <c>0 0 64 64</c> view box) in z-order.
/// </summary>
public static partial class AvatarGenerator
{
    /// <summary>
    /// Renders a complete avatar to a single SVG document.
    /// </summary>
    /// <param name="options">The avatar to render. Defaults to <see cref="AvatarOptions.Default"/>.</param>
    /// <param name="size">Pixel width/height of the output SVG (the view box is always 64×64).</param>
    /// <returns>A standalone, self-contained SVG string.</returns>
    public static string Generate(AvatarOptions? options = null, int size = 512)
    {
        var o = options ?? AvatarOptions.Default;

        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{size}\" height=\"{size}\" viewBox=\"0 0 64 64\">");

        // The layers, back-to-front, matching the original z-index order (getZIndex).
        // Eyes and mouth use a fixed colour; the nose is filled with the skin colour.
        // (The original's "Head" layer at z 40 has no template, so there is none here.)
        Layer(sb, "Background",  "#" + o.BgColor);          // z 20
        Layer(sb, o.Skin,        "#" + o.SkinColor);        // z 30
        Layer(sb, o.Hair,        "#" + o.HairColor);        // z 70
        Layer(sb, o.Body,        "#" + o.BodyColor);        // z 75
        Layer(sb, o.Mouth,       "#000000");                // z 80
        Layer(sb, o.FacialHair,  "#" + o.FacialHairColor);  // z 90
        Layer(sb, o.Nose,        "#" + o.SkinColor);        // z 100
        Layer(sb, o.Eyes,        "#000000");                // z 110

        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>
    /// Renders one part at the shared 64-unit view-box scale (so layers overlay perfectly
    /// inside the outer SVG) and appends it. Empty parts — an unknown style or an
    /// intentional "None" such as facial hair — contribute nothing.
    /// </summary>
    private static void Layer(StringBuilder sb, string style, string fill)
    {
        var part = AvatarParts.Render(style, fill, "64");
        if (part.Length != 0)
            sb.Append(NestablePart(part));
    }

    /// <summary>
    /// Renders a single avatar part to a standalone SVG, exactly like the original
    /// loader. Returns an empty string for unknown or intentionally-empty styles.
    /// </summary>
    /// <param name="name">Style name, e.g. <c>Bobcut</c> or <c>Sunglasses</c>.</param>
    /// <param name="fill">Fill colour including the leading <c>#</c>, e.g. <c>#B16A5B</c>.</param>
    /// <param name="size">Pixel size for the part's width/height.</param>
    public static string RenderPart(string name, string fill = "#000", int size = 64)
        => AvatarParts.Render(name, fill, size.ToString());

    /// <summary>Strips an XML prolog and surrounding whitespace so a part can be nested in another SVG.</summary>
    private static string NestablePart(string part)
        => XmlProlog().Replace(part, string.Empty).Trim();

    [GeneratedRegex(@"<\?xml.*?\?>", RegexOptions.Singleline)]
    private static partial Regex XmlProlog();
}
