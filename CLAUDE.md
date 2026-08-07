# Balatron — working notes for AI sessions

WPF (`net6.0-windows`) companion app for Balatro. Two halves:

- **Savegame Editor** — decompresses `save.jkr`, parses the Lua table, edits and rewrites it.
- **Live Peek** — watches the save and *predicts* upcoming shop rerolls, pack contents,
  vouchers, tags and consumable outcomes by reimplementing Balatro's RNG bit-exactly.

Solution: `Balatron/Balatron.sln` → `Balatron/Balatron.csproj`.

```bash
dotnet build Balatron/Balatron.csproj -c Debug
```

Building the `.sln` works too. If the running app holds `bin\Debug\...\Balatron.exe`
open, build `-c Release` instead of killing their process.

`Properties/PublishProfiles/FolderProfile.pubxml` produces a self-contained single-file
exe; Rider surfaces it as a "Balatron: FolderProfile" run configuration.

---

## Verification protocol

**Predictions must be verified against the user's real save before being reported as
working.** A prediction that compiles is worth nothing — the failure mode is silently
wrong cards, which the user only discovers mid-run.

### The harness

Create a throwaway console app in the scratchpad that **project-references the real
app**, so it exercises the actual `PredictionEngine` / `GameStateSnapshot` / view models
rather than a copy:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>          <!-- lets it construct + render WPF controls -->
    <Nullable>disable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="C:\Users\Laurids\Development\Balatron\Balatron\Balatron.csproj" />
  </ItemGroup>
</Project>
```

Run with `dotnet run --project <dir> -c Release` and print results.

### Feed it the real save

`%AppData%\Balatro\<profile>\save.jkr` is raw DEFLATE (no gzip header):

```powershell
$in = [System.IO.File]::OpenRead($src)
$ds = New-Object System.IO.Compression.DeflateStream($in, [System.IO.Compression.CompressionMode]::Decompress)
$sr = New-Object System.IO.StreamReader($ds, [System.Text.Encoding]::ASCII)
[System.IO.File]::WriteAllText($dst, $sr.ReadToEnd(), [System.Text.Encoding]::ASCII)
```

**Re-copy it every time.** A stale fixture once made a redeemed voucher look absent and
sent a diagnosis down the wrong path.

### Deriving hypothetical states

`GameStateSnapshot` is a `record` specifically so tests can branch off real state:

```csharp
var s = snap with { UsedJokers = withNeptune, ShowmanOwned = true };
new PredictionEngine(s, null).PredictPackContents("p_celestial_jumbo_1");
```

Real seed, real counters, one field changed. Use this to prove conditional logic —
and **construct the case that forces the question**. "Same output with and without
Showman" proves nothing if that RNG stream never drew a duplicate.

### Rendering UI offscreen

Because the harness is `UseWPF`, controls can be laid out and rendered to PNG on an STA
thread (`Measure`/`Arrange`/`UpdateLayout` → `RenderTargetBitmap` → `PngBitmapEncoder`),
then read back as an image. Instantiate the app for its resources:

```csharp
var app = new Balatron.App();
app.InitializeComponent();   // not new Application() — App.xaml owns the styles
```

This caught real bugs that compiled fine: an "active" border style that never applied,
and playing cards rendering as blank faces. Note a bare `ContentControl` doesn't paint
its `Background`, so tooltip renders look washed out — a harness artifact, not a bug.

### Sanity anchors that should keep passing

- `pseudohash(seed)` reproduces the save's `hashed_seed` to ~14 digits.
- Every counter in `GAME.pseudorandom` is reachable by iterating the counter-advance
  chain from `pseudohash(key..seed)`.
- Same snapshot ⇒ identical predictions (determinism).

---

## Never guess game mechanics — read the source

The Steam build is a fused LÖVE binary: the game's zip (all Lua + GLSL) is appended to
`Balatro.exe`. .NET's zip reader fails on the shifted offsets, so carve it first — find
the End-Of-Central-Directory record, compute `delta = (eocd - cdSize) - cdOffset`, and
write out everything from `delta` onward. That yields `card.lua`, `game.lua`,
`functions_*.lua`, `tag.lua`, `resources/shaders/*.fs`, etc.

`SpectralPack/Immolate` on GitHub is a validated reimplementation — good for pool
ordering and RNG key names, but the game's own Lua is the authority.

Mechanics established this way (all verified in-source, don't re-derive):

- **Every effect owns a named RNG stream** (`pseudoseed(key)`), so predictions for one
  effect are unaffected by unrelated shop actions.
- **`pseudorandom_element` sorts candidates by `sort_id`** before indexing — picks are
  independent of on-screen order and reproducible from the save.
- **A `forced_key` in `create_card` skips pool selection entirely ⇒ consumes no RNG.**
  This is why Telescope shifts every *other* card in a Celestial pack, not just slot 1.
- **Creating a card writes `used_jokers[key] = true`**, which is what suppresses
  duplicates inside one pack — and **Showman bypasses every such check**.
- **Pool order is load-bearing**: items are chosen by index, so the joker/tarot/planet
  lists in `BalatroItems` must stay in game order.
- Not everything is predictable: **To Do List** builds its candidate list by iterating a
  Lua hash table, so the pick depends on LuaJIT's internal ordering. Omit rather than
  guess.

---

## WPF gotchas already paid for

- **A local attribute value beats a Style trigger.** `BorderBrush="{Binding Accent}"` on
  the element makes a `DataTrigger` setter for `BorderBrush` silently do nothing — move
  the default into the Style as a `Setter`.
- **An inline `<Style TargetType="TextBlock">` replaces the implicit app style**, losing
  the Balatro font. Always add `BasedOn="{StaticResource {x:Type TextBlock}}"`.
- **Layered windows hit-test by pixel alpha.** With `AllowsTransparency="True"`, a fully
  transparent element is clicked *through*. Resize grips use `Fill="#01000000"` (alpha 1).
- **`ToolTipService` keeps only one tooltip alive**, so nested tooltips are impossible.
  `Views/HoverPopup.cs` replaces it with depth-tracked `Popup`s and a prune pass.
- **`ShaderEffect` needs `ps_3_0` bytecode**; `.hlsl` sources and compiled `.ps` blobs
  live in `Shaders/` and are committed, so `fxc` isn't a build dependency. There is no
  software fallback — `RenderCapability` is checked before applying.

### Pixel art scaling

Sprite sheets ship as 1x and 2x pairs. Base tile is **71×95**; the joker sheet is the 2x
set (**142×190** tiles). A **142×190 display box is therefore 1:1 for jokers and exactly
2x for every other sheet** — that's the sweet spot for the full-size card. Compact cards
use a 71×95 box and the 1x sheets.

`Views/PixelArt.cs` (`PixelArt.Scale="1|2"`) sizes boxes in *device* pixels via
`VisualTreeHelper.GetDpi`, so the ratio stays integral at non-96 DPI. Fractional scaling
is what makes nearest-neighbour art look mushy. Downscaled thumbnails are the exception:
use `HighQuality`, since nearest-neighbour just drops pixels.

---

## Conventions

- Predictions answer "if you did this **right now**" — always computed from a fresh copy
  of the save's counters.
- The Live Peek side is strictly read-only; the game overwrites `save.jkr` constantly.
- No tutorial text in the UI. Assume the app works; don't narrate it on screen.
- Smallest legible font is **14** (joker names); step up 16 / 18 for emphasis and headers.
