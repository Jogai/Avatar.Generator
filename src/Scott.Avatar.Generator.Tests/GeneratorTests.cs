using System.Xml.Linq;
using Scott.Avatar.Generator;
using Xunit;

namespace Scott.Avatar.Generator.Tests;

public class GeneratorTests
{
    private static readonly AvatarConfig Config = AvatarConfig.Default;

    /// <summary>Every distinct style from every category. (Hair lists "Hat" twice in the original config.)</summary>
    public static IEnumerable<object[]> AllStyles() =>
        new[]
            {
                Config.SkinStyles, Config.HairStyles, Config.FacialHairStyles, Config.BodyStyles,
                Config.EyeStyles, Config.MouthStyles, Config.NoseStyles, Config.BgStyles,
            }
            .SelectMany(list => list)
            .Distinct(StringComparer.Ordinal)
            .Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(AllStyles))]
    public void EveryStyleRendersValidXml(string style)
    {
        var svg = AvatarGenerator.RenderPart(style, "#123456", 150);

        if (style == "None")
        {
            Assert.Equal(string.Empty, svg); // facial-hair "None" is intentionally empty
            return;
        }

        Assert.NotEqual(string.Empty, svg);
        // A couple of original templates carry a cosmetic leading newline before an
        // <?xml?> prolog; trim it so the strict XML parser accepts the standalone part.
        var doc = XDocument.Parse(svg.TrimStart()); // throws if malformed
        Assert.Equal("svg", doc.Root!.Name.LocalName);
    }

    [Fact]
    public void DefaultAvatarGeneratesValidComposite()
    {
        var svg = AvatarGenerator.Generate(AvatarOptions.Default);
        var doc = XDocument.Parse(svg);
        Assert.Equal("svg", doc.Root!.Name.LocalName);
        Assert.Contains("viewBox=\"0 0 64 64\"", svg);
    }

    [Fact]
    public void SizeControlsOuterDimensionsOnly()
    {
        var svg = AvatarGenerator.Generate(AvatarOptions.Default, size: 256);
        var root = XDocument.Parse(svg).Root!;
        Assert.Equal("256", root.Attribute("width")!.Value);
        Assert.Equal("256", root.Attribute("height")!.Value);
        Assert.Equal("0 0 64 64", root.Attribute("viewBox")!.Value);
    }

    [Fact]
    public void EveryConfigCombinationProducesValidComposite()
    {
        // Exhaustively exercise every style in every slot against valid XML.
        foreach (var hair in Config.HairStyles)
        foreach (var facial in Config.FacialHairStyles)
        {
            var options = AvatarOptions.Default with { Hair = hair, FacialHair = facial };
            var svg = AvatarGenerator.Generate(options);
            XDocument.Parse(svg);
        }

        foreach (var eyes in Config.EyeStyles)
        foreach (var mouth in Config.MouthStyles)
        foreach (var nose in Config.NoseStyles)
        foreach (var body in Config.BodyStyles)
        {
            var options = AvatarOptions.Default with
            {
                Eyes = eyes, Mouth = mouth, Nose = nose, Body = body,
            };
            XDocument.Parse(AvatarGenerator.Generate(options));
        }
    }

    [Fact]
    public void RandomIsReproducibleWithSeed()
    {
        var a = AvatarOptions.Random(Config, new Random(42));
        var b = AvatarOptions.Random(Config, new Random(42));
        Assert.Equal(a, b);
    }

    [Fact]
    public void FillSubstitutionAppliesToParts()
    {
        var svg = AvatarGenerator.RenderPart("Skin", "#ABCDEF", 64);
        Assert.Contains("#ABCDEF", svg);
        Assert.DoesNotContain("$fill", svg);
        Assert.DoesNotContain("$size", svg);
    }

    [Fact]
    public void FromStringIsDeterministic()
    {
        // Same key -> identical avatar, every time (the placeholder guarantee).
        var a = AvatarOptions.FromString("alice@example.com", Config);
        var b = AvatarOptions.FromString("alice@example.com", Config);
        Assert.Equal(a, b);
    }

    [Fact]
    public void FromStringVariesByKey()
    {
        // Across many distinct keys, avatars should overwhelmingly differ.
        var avatars = Enumerable.Range(0, 100)
            .Select(i => AvatarOptions.FromString($"user{i}", Config))
            .ToHashSet();
        Assert.True(avatars.Count > 90, $"expected high variety, got {avatars.Count}/100 distinct");
    }

    [Fact]
    public void FromStringChoosesOnlyCatalogueValues()
    {
        var a = AvatarOptions.FromString("someone", Config);
        Assert.Contains(a.Hair, Config.HairStyles);
        Assert.Contains(a.Eyes, Config.EyeStyles);
        Assert.Contains(a.HairColor, Config.HairColors);
        XDocument.Parse(AvatarGenerator.Generate(a)); // composes to valid SVG
    }

    [Fact]
    public void FromStringNullKeyIsTreatedAsEmpty()
    {
        Assert.Equal(AvatarOptions.FromString("", Config), AvatarOptions.FromString(null!, Config));
    }
}
