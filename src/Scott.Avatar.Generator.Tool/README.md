# Scott.Avatar.Generator.Tool

Command-line SVG avatar generator (ships the [Personas](https://personas.draftbit.com) set),
built on [`Scott.Avatar.Generator`](https://www.nuget.org/packages/Scott.Avatar.Generator).

## Install

```sh
dotnet tool install --global Scott.Avatar.Generator.Tool
```

Or build a standalone binary that needs no installed .NET runtime — pass a RID to get a self-contained, trimmed, single-file `avatar` executable:

```sh
dotnet publish -c Release -r linux-x64   # or win-x64, osx-arm64, ...
```

## Usage

```sh
avatar                                        # default avatar -> stdout
avatar --random                               # random avatar
avatar --seed 42 -o avatar.svg                # reproducible random -> file
avatar --key alice@example.com                # deterministic: same key -> same avatar
avatar --hair Mohawk --hair-color 5AC4D4 \
         --eyes Wink --mouth Smile --size 256   # customised
avatar list                                   # every style and colour
avatar --help
```

### Options

| Option | Description |
| --- | --- |
| `-o, --output <file>` | Write SVG to a file (default: stdout) |
| `--size <px>` | Output width/height in pixels (default: 512) |
| `--random` | Randomize, then apply any overrides |
| `--seed <int>` | Seed the randomizer (implies `--random`) |
| `--key <string>` | Deterministic avatar from a key (e.g. username/e-mail); same key → same avatar |
| `--skin`, `--skin-color` | Skin style / colour |
| `--hair`, `--hair-color` | Hair (or hat) style / colour |
| `--facial-hair`, `--facial-hair-color` | Facial-hair style / colour |
| `--body`, `--body-color` | Body style / colour |
| `--eyes`, `--mouth`, `--nose` | Eye / mouth / nose style |
| `--bg-color` | Background colour |

Colours are hex without a leading `#`, e.g. `--hair-color 5AC4D4`.
Run `avatar list` for valid style and colour values.
