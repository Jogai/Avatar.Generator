using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Scott.Avatar.Generator;

// CreateSlimBuilder is the trimming/Native-AOT-friendly host: minimal default
// features, no reflection-based startup.
var builder = WebApplication.CreateSlimBuilder(args);

// Register source-generated JSON metadata so responses serialise without reflection.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonContext.Default));

// Server-side response caching for the static-ish endpoints (the set catalogue).
builder.Services.AddOutputCache();

var app = builder.Build();
app.UseOutputCache();

const string SvgContentType = "image/svg+xml";

// The avatar sets this service can serve, keyed by set name. The engine currently
// bundles a single set ("personas"); the routing is shaped around {set} so additional
// sets slot in here without changing the API surface.
var sets = new Dictionary<string, AvatarConfig>(StringComparer.OrdinalIgnoreCase)
{
    ["personas"] = AvatarConfig.Default,
};

string[] SetNames() => sets.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();

// Build an AvatarOptions from query-string overrides, starting from a base avatar.
static AvatarOptions Apply(AvatarOptions a, IQueryCollection q)
{
    string? V(string key) => q.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v.ToString() : null;
    return a with
    {
        Skin = V("skin") ?? a.Skin,
        SkinColor = V("skinColor") ?? a.SkinColor,
        Hair = V("hair") ?? a.Hair,
        HairColor = V("hairColor") ?? a.HairColor,
        FacialHair = V("facialHair") ?? a.FacialHair,
        FacialHairColor = V("facialHairColor") ?? a.FacialHairColor,
        Body = V("body") ?? a.Body,
        BodyColor = V("bodyColor") ?? a.BodyColor,
        Eyes = V("eyes") ?? a.Eyes,
        Mouth = V("mouth") ?? a.Mouth,
        Nose = V("nose") ?? a.Nose,
        BgColor = V("bgColor") ?? a.BgColor,
    };
}

static int Size(IQueryCollection q) =>
    q.TryGetValue("size", out var v) && int.TryParse(v, out var s) && s is > 0 and <= 4096 ? s : 512;

IResult UnknownSet(string set) =>
    Results.NotFound(new ErrorResponse($"Unknown set '{set}'", SetNames()));

// Returns an SVG with a strong ETag (handling If-None-Match -> 304). Deterministic
// results (a key or seed was given) are immutable and cache forever; a plain random
// avatar differs every call, so it is marked no-store.
static IResult Svg(HttpContext ctx, string svg, bool deterministic)
{
    var etag = $"\"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(svg)))}\"";
    ctx.Response.Headers.ETag = etag;
    ctx.Response.Headers.CacheControl = deterministic
        ? "public, max-age=31536000, immutable"
        : "no-store";

    if (ctx.Request.Headers.IfNoneMatch.ToString() == etag)
        return Results.StatusCode(StatusCodes.Status304NotModified);
    return Results.Text(svg, SvgContentType);
}

// Health probe that doubles as set discovery: lists the available sets.
app.MapGet("/health", (HttpContext ctx) =>
{
    ctx.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new HealthResponse("ok", SetNames()));
});

// An avatar for the set, as an inline SVG document.
//   ?key=<string>  -> a deterministic avatar (same key always yields the same avatar)
//   ?seed=<int>    -> a reproducible random avatar
//   (neither)      -> a fresh random avatar each call
// Per-part overrides (?hair=Mohawk&hairColor=5AC4D4&size=256 ...) apply on top.
app.MapGet("/{set}", (string set, HttpContext ctx) =>
{
    var q = ctx.Request.Query;
    if (!sets.TryGetValue(set, out var config))
        return UnknownSet(set);

    var key = q.TryGetValue("key", out var k) && !string.IsNullOrEmpty(k) ? k.ToString() : null;
    var seed = q.TryGetValue("seed", out var s) && int.TryParse(s, out var sv) ? sv : (int?)null;

    var baseAvatar =
        key is not null ? AvatarOptions.FromString(key, config)
        : seed is not null ? AvatarOptions.Random(config, new Random(seed.Value))
        : AvatarOptions.Random(config);

    var svg = AvatarGenerator.Generate(Apply(baseAvatar, q), Size(q));
    return Svg(ctx, svg, deterministic: key is not null || seed is not null);
});

// The catalogue (styles, palettes, attribution) for a particular set.
app.MapGet("/{set}/config", (string set, HttpContext ctx) =>
{
    if (!sets.TryGetValue(set, out var config))
        return UnknownSet(set);
    ctx.Response.Headers.CacheControl = "public, max-age=3600";
    return Results.Ok(config);
}).CacheOutput(p => p.Expire(TimeSpan.FromHours(1)));

app.Run();

/// <summary>Health probe payload: service status and the available avatar sets.</summary>
internal sealed record HealthResponse(string Status, string[] Sets);

/// <summary>Error payload for an unknown set, listing the sets that do exist.</summary>
internal sealed record ErrorResponse(string Error, string[] Available);

/// <summary>Source-generated JSON metadata for the API's response types (AOT/trim safe).</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(AvatarConfig))]
internal sealed partial class ApiJsonContext : JsonSerializerContext;

// Exposed for integration testing.
public partial class Program;
