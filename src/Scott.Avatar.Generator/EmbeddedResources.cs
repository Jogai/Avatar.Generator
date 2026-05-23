using System.Reflection;

namespace Scott.Avatar.Generator;

/// <summary>
/// Helpers for reading the avatar set files (SVG fragments and <c>config.json</c>)
/// embedded into this assembly at build time.
/// </summary>
internal static class EmbeddedResources
{
    private static readonly Assembly Assembly = typeof(EmbeddedResources).Assembly;

    /// <summary>The manifest names of every embedded resource.</summary>
    public static string[] Names => Assembly.GetManifestResourceNames();

    /// <summary>Reads an embedded resource (by manifest name) as text.</summary>
    public static string Read(string name)
    {
        using var stream = Assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
