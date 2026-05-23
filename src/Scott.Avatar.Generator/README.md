# Scott.Avatar.Generator

A faithful C# port of the [Draftbit **Personas**](https://personas.draftbit.com) avatar
generator. Produces inclusive, layered **SVG** avatars as plain strings — no native
dependencies, no I/O, safe to call from anywhere (web, desktop, functions).

Targets **.NET 8.0**, so it can be consumed from any .NET 8+ project.

## Install

```sh
dotnet add package Scott.Avatar.Generator
```

## Usage

```csharp
using Scott.Avatar.Generator;

// The default avatar (matches the original app's defaults)
string svg = AvatarGenerator.Generate();

// Fully customised
var options = new AvatarOptions
{
    Hair = "Mohawk", HairColor = "5AC4D4",
    Eyes = "Wink", Mouth = "Smile",
    FacialHair = "None",
    BodyColor = "456DFF", BgColor = "A9E775",
};
string custom = AvatarGenerator.Generate(options, size: 256);

// Random (seed for reproducibility)
var random = AvatarGenerator.Generate(AvatarOptions.Random(random: new Random(42)));

// A single part on its own
string mohawk = AvatarGenerator.RenderPart("Mohawk", "#5AC4D4", size: 128);

// Discover valid styles & palettes
AvatarConfig config = AvatarConfig.Default;
```

Colours are hex strings **without** a leading `#` (e.g. `"5AC4D4"`), matching the
original configuration. See `AvatarConfig.Default` for every available style and palette.
