using Scott.Avatar.Generator;

// A small, dependency-free CLI over the core generator.
// Usage examples:
//   avatar                                  # random avatar -> stdout
//   avatar --seed 42 -o avatar.svg          # reproducible random -> file
//   avatar --hair Mohawk --hair-color 5AC4D4 --eyes Wink --size 256
//   avatar list                             # show every style and palette

var config = AvatarConfig.Default;

if (args.Length > 0 && args[0] is "list")
{
    PrintCatalogue(config);
    return 0;
}

if (args.Contains("--help") || args.Contains("-h"))
{
    PrintHelp();
    return 0;
}

var opts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
string? output = null;
int size = 512;
int? seed = null;
bool random = false;
string? key = null;

for (var i = 0; i < args.Length; i++)
{
    var a = args[i];
    string Next(string name) =>
        i + 1 < args.Length ? args[++i] : throw new ArgumentException($"Missing value for {name}");

    switch (a)
    {
        case "--random": random = true; break;
        case "--key": key = Next(a); break;
        case "-o" or "--output": output = Next(a); break;
        case "--size": size = int.Parse(Next(a)); break;
        case "--seed": seed = int.Parse(Next(a)); random = true; break;

        case "--skin": opts["skin"] = Next(a); break;
        case "--skin-color": opts["skinColor"] = Next(a); break;
        case "--hair": opts["hair"] = Next(a); break;
        case "--hair-color": opts["hairColor"] = Next(a); break;
        case "--facial-hair": opts["facialHair"] = Next(a); break;
        case "--facial-hair-color": opts["facialHairColor"] = Next(a); break;
        case "--body": opts["body"] = Next(a); break;
        case "--body-color": opts["bodyColor"] = Next(a); break;
        case "--eyes": opts["eyes"] = Next(a); break;
        case "--mouth": opts["mouth"] = Next(a); break;
        case "--nose": opts["nose"] = Next(a); break;
        case "--bg-color": opts["bgColor"] = Next(a); break;

        default:
            Console.Error.WriteLine($"Unknown argument: {a}");
            PrintHelp();
            return 1;
    }
}

// Pick the base avatar: a deterministic key wins, then random (optionally seeded),
// otherwise the default. Explicit overrides are applied on top.
var avatar =
    key is not null ? AvatarOptions.FromString(key, config)
    : random ? AvatarOptions.Random(config, seed is { } s ? new Random(s) : null)
    : AvatarOptions.Default;

avatar = avatar with
{
    Skin = opts.GetValueOrDefault("skin", avatar.Skin),
    SkinColor = opts.GetValueOrDefault("skinColor", avatar.SkinColor),
    Hair = opts.GetValueOrDefault("hair", avatar.Hair),
    HairColor = opts.GetValueOrDefault("hairColor", avatar.HairColor),
    FacialHair = opts.GetValueOrDefault("facialHair", avatar.FacialHair),
    FacialHairColor = opts.GetValueOrDefault("facialHairColor", avatar.FacialHairColor),
    Body = opts.GetValueOrDefault("body", avatar.Body),
    BodyColor = opts.GetValueOrDefault("bodyColor", avatar.BodyColor),
    Eyes = opts.GetValueOrDefault("eyes", avatar.Eyes),
    Mouth = opts.GetValueOrDefault("mouth", avatar.Mouth),
    Nose = opts.GetValueOrDefault("nose", avatar.Nose),
    BgColor = opts.GetValueOrDefault("bgColor", avatar.BgColor),
};

var svg = AvatarGenerator.Generate(avatar, size);

if (output is null)
{
    Console.WriteLine(svg);
}
else
{
    File.WriteAllText(output, svg);
    Console.Error.WriteLine($"Wrote {output} ({svg.Length} bytes)");
}

return 0;

static void PrintHelp()
{
    Console.WriteLine(
        """
        avatar — generate Personas SVG avatars

        USAGE:
          avatar [options]            Generate an avatar (default styles unless overridden)
          avatar --random [options]   Randomize, then apply any overrides
          avatar --key <string>       A deterministic avatar from a key (same key -> same avatar)
          avatar list                 List every available style and colour

        OUTPUT:
          -o, --output <file>   Write SVG to a file (default: stdout)
          --size <px>           Output width/height in pixels (default: 512)
          --seed <int>          Seed the randomizer for reproducible output (implies --random)
          --key <string>        Derive a stable avatar from a key, e.g. a username or e-mail

        STYLE OVERRIDES (see `avatar list` for valid values):
          --skin, --skin-color
          --hair, --hair-color
          --facial-hair, --facial-hair-color
          --body, --body-color
          --eyes
          --mouth
          --nose
          --bg-color

        Colours are hex without a leading '#', e.g. --hair-color 5AC4D4
        """);
}

static void PrintCatalogue(AvatarConfig c)
{
    void Section(string title, IReadOnlyList<string> items) =>
        Console.WriteLine($"{title,-16} {string.Join(", ", items)}");

    Console.WriteLine("STYLES");
    Section("  skin", c.SkinStyles);
    Section("  hair", c.HairStyles);
    Section("  facial-hair", c.FacialHairStyles);
    Section("  body", c.BodyStyles);
    Section("  eyes", c.EyeStyles);
    Section("  mouth", c.MouthStyles);
    Section("  nose", c.NoseStyles);
    Console.WriteLine();
    Console.WriteLine("COLOURS (hex, no '#')");
    Section("  skin", c.SkinColors);
    Section("  hair", c.HairColors);
    Section("  facial-hair", c.FacialHairColors);
    Section("  body", c.BodyColors);
    Section("  background", c.BgColors);
}
