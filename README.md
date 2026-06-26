# Illusionist / 幻术师 — a Slay the Spire 2 character mod

A standalone playable character for **Slay the Spire 2**, built around mirrors, foresight, and
illusion. Fully localized in **English** and **简体中文**.

> Unofficial fan mod. *Slay the Spire 2* and all of its assets are property of **Mega Crit**. This
> mod ships only original code/localization and reuses base-game art as placeholder visuals.

## The character

The Illusionist is a trickster who bends perception. Her kit is three small systems that interlock:

- **Mirror (复制)** — *Copy* summons mirror images that **replay the first card you play each turn**
  and **shatter when you take unblocked damage**. A fragile, fast-snowballing engine
  (Mirror Image, Conscript, Kaleidoscope, Detonate, Siphon, Memory, Dazzle, Siege, Rekindle…).
- **Intent (意图)** — read and turn the enemy's telegraph against them. **Provoke** inflates an
  enemy's attack with temporary Strength; **Counter** reflects its attack intent as damage;
  **Foresight** blocks it. **Encore** recurs your Retain cards so the control suite recurs.
- **Transmute (幻化)** — temporarily reshape your own cards. A *transmute* is two transforms: a card
  changes now and **reverts one layer at the end of each turn** (it even unwinds from the exhaust
  pile). Shifting Blade / Mirror Ward copy themselves onto dead cards; **Myriad Faces** turns your
  hand into copies of one card; **Phantom Blast** costs 0 when it's a copy; **Kindle** and **Summon**
  conjure temporary value from status cards and your draw pile.

Plus custom relics, potions, a starter relic, and **Orobas / Darv (Ancient)** relic & card upgrades.

## Installation

### Steam Workshop (recommended)

Subscribe to the mod on the Steam Workshop, then enable it from the in-game mod menu.

### Manual

Copy the three built files into the game's mod folder:

```
<Slay the Spire 2>/mods/illusionist/
    illusionist.dll
    illusionist.pck
    mod_manifest.json
```

Then enable **Illusionist (幻术师)** in the in-game mod menu and pick the character on the
character-select screen.

## Building from source

Requirements: the **.NET 9 SDK**, a **Godot 4.5.x (.NET)** build, and a local copy of
*Slay the Spire 2*.

```powershell
# from the repo root
./sts2_illusionist/build-illusionist-windows.ps1
```

The script runs `dotnet build`, packs the `.pck`, and installs the result into your game's
`mods/illusionist/` folder. A successful build reports `0 个错误` / `0 个警告`.

## Repository layout

| Path | What |
|---|---|
| `sts2_illusionist/` | The mod source — Godot + .NET project, scripts, localization, build script. |
| `sts2_illusionist/Scripts/` | C# source: `Cards/`, `Powers/`, `Relics/`, `Potions/`, `Patches/`, `Pools/`, `Characters/`. |
| `sts2_illusionist/illusionist/localization/` | `eng/` and `zhs/` loc tables (cards, powers, relics, potions, characters). |
| `illusionist-workshop/` | Steam Workshop upload folder (`workshop.json`, built `content/`). |

## Notes

- The Illusionist is a **standalone character** (it is auto-registered into the roster), not a reskin
  of an existing one — base characters are unaffected.
- It reuses Necrobinder's art/visuals as placeholders via an asset-path redirect; bespoke art is a
  work in progress, so you'll see some missing-sprite warnings in the log (cosmetic only).

## Credits

- Mod design & code: **Airmongsity**
- *Slay the Spire 2* © **Mega Crit** — this is an unofficial, non-commercial fan project.

## License

Released under the [MIT License](LICENSE) (applies to this mod's own code and localization only).
