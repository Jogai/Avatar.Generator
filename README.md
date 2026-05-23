# Scott.Avatar.Generator

A C# (.NET 10 build, .NET 8 library) engine that generates inclusive, layered **SVG** avatars. The engine is generic; the artwork lives in **asset sets**. The first (and currently bundled) set is **Personas**, a faithful port of the [Draftbit **Personas**](https://personas.draftbit.com) generator (originally Gatsby + ReScript).

The engine lives in [`src/`](src/) and the Personas set lives in [`personas/`](personas/) (its SVG artwork plus the `config.json` catalogue, which carries an `attribution` block crediting the original). Adding a new set later means adding a sibling folder with the same layout.

## What's in the box

| Project | Type | Purpose |
| --- | --- | --- |
| `Scott.Avatar.Generator` | Library (net8.0) | The core: turns options into an SVG string. Consumable from any .NET 8+ project. Published to **nuget.org**. |
| `Scott.Avatar.Generator.Api` | Minimal API (net10.0) | HTTP service exposing the generator. Containerised, published to **ghcr.io**. |
| `Scott.Avatar.Generator.Tool` | dotnet tool (net10.0) | CLI (`avatar`) callable from any machine. Published to **nuget.org**. |
| `Scott.Avatar.Generator.Tests` | xUnit (net10.0) | Verifies every original option renders valid SVG, plus combinations. |
| `personas/` | Artwork | One `.svg` per part, grouped by category. The single source of truth for the artwork; embedded into the library at build time. |

Namespace: `Scott.Avatar.Generator`.

## For designers — editing the artwork

Every avatar part is a plain SVG file under [`personas/`](personas/), one folder per category:

```
personas/
  Background/  Skin/  Hair/  FacialHair/  Body/  Eyes/  Mouth/  Nose/
```

To tweak a part, edit its file (e.g. `personas/Hair/Mohawk.svg`) in any editor — **no code changes required**. The build embeds these files into the library automatically.

Two simple rules:

- **Keep the placeholders.** `$fill` is replaced with the chosen colour and `$size` with the pixel size at render time. Leave them where they are (e.g. `fill="$fill"`, `width="$size"`). Parts that should ignore the colour simply don't use `$fill`.
- **The file name is the style name.** `personas/Hair/Mohawk.svg` is selected as `Mohawk`. To add a *brand-new* style, drop in the file **and** add its name to the matching list in [`personas/config.json`](personas/config.json) so it appears in the palettes and the randomizer. (The catalogue and the artwork live together in `personas/`.)

The artwork was migrated, verbatim, out of the original ReScript source; the `personas/` files are now the single source of truth.

## Quick start

```sh
cd src
dotnet build          # build everything
dotnet test           # 54 tests — every part + combinations render valid XML
```

### Library

```csharp
using Scott.Avatar.Generator;

string svg = AvatarGenerator.Generate();                       // default avatar
string r   = AvatarGenerator.Generate(AvatarOptions.Random()); // random
string d   = AvatarGenerator.Generate(AvatarOptions.FromString("alice@example.com")); // deterministic
var opts   = new AvatarOptions { Hair = "Mohawk", HairColor = "5AC4D4", Eyes = "Wink" };
string c   = AvatarGenerator.Generate(opts, size: 256);        // customised
string one = AvatarGenerator.RenderPart("Sunglasses", "#000", 128); // a single part
```

`AvatarOptions.FromString(key)` is the placeholder workhorse: the same key (username, e-mail, id) always maps to the same avatar — stable across processes, machines and versions.

### CLI

```sh
dotnet tool install --global Scott.Avatar.Generator.Tool
avatar --seed 42 -o avatar.svg
avatar --key alice@example.com -o alice.svg   # deterministic: same key -> same avatar
avatar --hair Hat --hair-color 456DFF --eyes Sunglasses --facial-hair BeardMustache
avatar list      # all styles & colours
```

Prefer a standalone binary with no installed .NET runtime? Publish a self-contained, trimmed, single-file `avatar` by passing a RID:

```sh
dotnet publish src/Scott.Avatar.Generator.Tool -c Release -r linux-x64   # or win-x64, osx-arm64, ...
# -> bin/Release/net10.0/linux-x64/publish/avatar  (a single ~13 MB executable)
```

### API

```sh
# Run locally
dotnet run --project src/Scott.Avatar.Generator.Api

# Or containerised — a Native AOT image (small native binary, no .NET runtime).
# Build context is the repo root, so personas/ is included.
podman build -f src/Scott.Avatar.Generator.Api/Dockerfile -t avatar-api .
podman run --rm -p 8080:8080 avatar-api
```

The API is **set-scoped** so it serves any number of avatar sets (today: `personas`).

| Endpoint | Description |
| --- | --- |
| `GET /{set}` | An avatar for the set, e.g. `GET /personas`. `?key=<string>` for a deterministic avatar (same key → same avatar), `?seed=N` for reproducible random, neither for fresh random. Per-part overrides still apply: `?hair=Mohawk&hairColor=5AC4D4&size=256`. |
| `GET /{set}/config` | The set's catalogue — styles, colour palettes and `attribution`. |
| `GET /health` | Health probe; returns the list of available sets. |

`GET /{set}` returns `image/svg+xml`. Responses carry a strong **ETag** (so `If-None-Match` yields `304`); deterministic results (`key` or `seed`) are served `public, max-age=31536000, immutable`, plain-random ones `no-store`. An unknown `{set}` returns `404` with the list of available sets.

## How an avatar is composed

Each avatar is eight layers, drawn back-to-front by the original's z-index (`getZIndex`): **Background → Skin → Hair → Body → Mouth → Facial hair → Nose → Eyes**. (The original also had a no-op `Head` layer with no template; it's omitted.)

Every layer is one of ~49 SVG fragments living under [`personas/`](personas/). At runtime only the fill colour (`$fill`) and pixel size (`$size`) are substituted (exactly as the original `j` template strings did). Fill assignment matches the original UI: skin/hair/body/background use their own colour, the **nose uses the skin colour**, and **eyes/mouth use a fixed colour** (`#000000`).

To turn the separately-positioned layers into one standalone SVG, each fragment is rendered at the shared `0 0 64 64` view box and nested inside an outer `<svg>` whose `width`/`height` carry the requested pixel size.

## Design decisions

- **Library targets net8.0, apps target net10.0.** The brief asked for a core consumable by "any dotnet 8+ project," so the library uses the lowest supported TFM for maximum reach, while the API and tool use net10.0.
- **Artwork is data, not code.** The SVG fragments live as editable files under [`personas/`](personas/) (one folder per category) so designers own them directly. They are embedded into the library at build time, and [`AvatarParts.cs`](src/Scott.Avatar.Generator/AvatarParts.cs) loads them by name and `String.Replace`s the `$fill`/`$size` placeholders at call time. The files were extracted **verbatim** from the original ReScript `SvgLoader.res`, preserving byte-fidelity with the original.
- **`personas/` lives at the repo root.** It is embedded via a repo-relative `EmbeddedResource` glob, so the published NuGet package and container are self-contained. Consequently the container build context is the **repository root**, not `src/`.
- **One upstream typo fixed.** The original `getHat` template carries a stray, unmatched `</g>` (0 opening `<g>`, 1 closing). Browsers silently tolerate it, but it makes the SVG malformed XML, which breaks strict parsers/sanitizers in a server context. The tag is purely spurious, so removing it left the rendered result identical — `personas/Hair/Hat.svg` is already well-formed.
- **Faithful quirks kept.** The original eye styles `Happy`/`Open` ignore the fill argument and set `fill="$size"` (an upstream bug that yields an invalid colour → SVG default black). This is preserved exactly; the rendered eyes are black either way.
- **Colours are hex without `#`**, matching the original config; the `#` is added when filling.
- **Single composite SVG** (nested fragments) rather than the original's stack of absolutely positioned DOM elements, so the output is one portable, self-contained document. Element `id`s within fragments are left as-is; collisions can't occur because conflicting `id`s only appear within a single category (e.g. two hair styles) and an avatar uses one per category.
- **No image rasterization.** The original exported PNG client-side via `html2canvas`. The port returns SVG only; callers can rasterize with their renderer of choice. See ideas below.
- **`config.json` is embedded** in the library and exposed via `AvatarConfig.Default`, so the catalogue stays the single source of truth and ships with the package. It also carries an `attribution` block (origin, author, licence) surfaced as `AvatarConfig.Attribution` and the API's `/{set}/config`.
- **Set-scoped API, single-set engine (for now).** Every API route is shaped around `/{set}` so the service is future-proof for multiple sets, and `/health` advertises which sets exist. The library itself currently bundles exactly one set (`personas`); making the engine load parts per-set is the main piece of work to add a second set (see ideas below).
- **Native AOT API, single-file CLI.** The library is reflection-free (catalogue JSON uses a source-generated serializer; parts load from embedded resources), so it is `IsAotCompatible` and the trim/AOT analyzers run clean. The API uses the slim host with source-generated response JSON and publishes as **Native AOT** — the container ships a small native binary on a glibc base with no .NET runtime. The CLI publishes as a **self-contained, trimmed, single-file** executable when given a RID, while still packing as a framework-dependent `dotnet tool` by default.
- **Deterministic avatars from a key.** `AvatarOptions.FromString` hashes the key (SHA-256) per part rather than seeding `System.Random`, so the mapping is stable across processes, machines and library versions — a hard requirement for persistent profile-photo placeholders. The API (`?key=`) and CLI (`--key`) expose it.
- **Caching lives in HTTP, not the app.** An avatar is a pure function of its query, so the API attaches a strong **ETag** (content hash, with `304` on `If-None-Match`) and a `Cache-Control` that reflects determinism: keyed/seeded responses are `immutable` for a year, plain-random ones are `no-store`. Server-side `OutputCache` covers the static-ish `/{set}/config`. This pushes caching to browsers/CDNs/proxies — the right layer for a stateless generator — and stays AOT-clean.

## Continuous delivery

- [`.github/workflows/ci.yml`](.github/workflows/ci.yml) — build + test on every push/PR; installs the Native AOT toolchain (`clang`, `zlib1g-dev`) and verifies the API's AOT publish and the CLI's self-contained single-file publish.
- [`.github/workflows/publish-nuget.yml`](.github/workflows/publish-nuget.yml) — packs the library and tool and pushes them to nuget.org on a `v*` tag (or manual run). Uses NuGet [trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) (OIDC) — a short-lived key is minted per run, so there's **no long-lived `NUGET_API_KEY` secret**. One-time setup: add a trusted-publishing policy on nuget.org (Repository Owner, Repository, Workflow File `publish-nuget.yml`, no environment) and a `NUGET_USER` secret (your nuget.org username).
- [`.github/workflows/publish-container.yml`](.github/workflows/publish-container.yml) — builds the API image and pushes to `ghcr.io` on `main` and `v*` tags, using the built-in `GITHUB_TOKEN`.

Cut a release by tagging: `git tag v1.0.0 && git push --tags`.

## Verifying the port

Tests render **every** style in every category and assert valid XML, exhaustively combine hair×facial-hair and eyes×mouth×nose×body, and check seeded randomness is reproducible. Sample avatars were generated by the CLI and rasterized with `resvg` during development to confirm visual fidelity against the original.

## Possible ideas

1. **A genuinely multi-set engine.** Make `AvatarParts`/`AvatarGenerator` set-aware (load each set's SVGs into its own keyed collection rather than one flat embedded namespace), add an `AvatarSet` type bundling a set's parts + `AvatarConfig`, and register sets by folder. The API and CLI are already shaped for this; today the engine bundles only `personas`.
2. **More palettes & a brightness/contrast knob**, and per-part colour overrides for eyes/mouth that the original hard-coded.
3. **`id` namespacing per layer** to make composites robust even if future fragments collide.
4. **OpenAPI document + Scalar/Swagger UI** for the API, and content negotiation (`Accept: image/svg+xml`).
5. **Property-based & snapshot tests** (e.g. Verify) to lock byte-level output and guard against accidental artwork regressions.
