# Compatibility Notes

This document records confirmed interoperability problems between Illusionist and other MODs. It is
not a general bug tracker: add an entry only when logs identify a cross-MOD boundary or when the same
feature behaves differently in a mixed-MOD environment.

## CuteAncientIllustrationFZ-Cards: card-selection screen freeze

| Field | Value |
|---|---|
| Status | Mitigated in Illusionist after v0.5.3; the upstream cache bug still exists |
| Observed | 2026-08-05 |
| Affected third-party version | `CuteAncientIllustrationFZ-Cards 1.0.0` |
| User-visible symptom | A deck upgrade or transformation selection screen fails to finish opening and appears frozen |
| Confirmed trigger | Opening a card grid after an asset-unload boundary, including Whispering Hollow's deck transformation flow |
| Exception | `ObjectDisposedException` for `Godot.CompressedTexture2D` |
| Faulting frame | `CuteAncientIllustrationFZ_Cards.PortraitPatch.RestoreDefaultState` |

### Evidence and cause

The captured run completed combat and entered Whispering Hollow normally. When
`NDeckTransformSelectScreen` initialized its card grid, the third-party portrait postfix called
`RestoreDefaultState`, assigned a disposed `CompressedTexture2D` to a `TextureRect`, and faulted the
screen initialization task. The same failure also appears from deck-upgrade grids.

The portrait MOD retains managed references to textures that it previously read from card nodes.
STS2 may unload the underlying imported Godot resources between rooms. A retained C# wrapper can
therefore remain present after its native texture has been disposed; assigning that stale wrapper is
invalid. This is a lifetime bug in the third-party portrait cache, not an Illusionist combat loop or
an Illusionist portrait-loading failure.

Primary captured evidence:

- `logs/StS2TestCardMod-runtime-logs/sessions/20260805-202826-823/godot2026-08-05T20.28.16.log`, starting at line 5146.
- The exception repeats through both `PortraitPatch.EnterTreePostfix` and
  `PortraitPatch.UpdateVisualsPostfix`.
- No Illusionist method occurs in the faulting exception stack.

### Illusionist mitigation

`Scripts/Patches/CuteAncientPortraitCompatibilityPatch.cs` installs a RitsuLib-managed Harmony
finalizer on `NCard._EnterTree` and `NCard.UpdateVisuals`. It suppresses an exception only when every
condition below is true:

1. The exception is an `ObjectDisposedException` for `Godot.CompressedTexture2D`.
2. Its stack identifies `CuteAncientIllustrationFZ_Cards.PortraitPatch`.
3. The card is an Illusionist card or belongs to an Illusionist player.

The native `NCard.UpdateVisuals` work has already run before the failing third-party postfix, so the
current native/Illusionist portrait remains usable. All other exceptions continue to propagate. The
guard logs its first suppression once per process to preserve diagnostics without flooding the log.

This is deliberately a compatibility guard, not an upstream repair. The portrait MOD should
eventually invalidate its cache on resource unload, verify `GodotObject.IsInstanceValid`, or reload a
texture from its resource path instead of retaining a `Texture2D` wrapper indefinitely. Disabling or
updating that MOD remains the fallback if another call site exposes the same upstream bug.

### Regression test

With both MODs enabled and an Illusionist run active:

1. Complete a combat and cross a room boundary so the asset loader has an opportunity to unload room
   resources.
2. Open a rest-site card-upgrade grid and select a card for preview.
3. Enter Whispering Hollow and choose the option that opens the deck-transformation grid.
4. Confirm both grids open, update portraits, accept/cancel normally, and do not log the disposed
   texture exception. If the guard activates, confirm the single Illusionist compatibility warning.

## Illusionist card-art integration audit

Audited against RitsuLib `0.4.48` on 2026-08-05. The current card-art injection path is appropriate:

1. Every registered Illusionist card derives from `IllusionistCard`.
2. `IllusionistCard` derives from RitsuLib's `ModCardTemplate` and overrides only
   `CustomPortraitPath`.
3. `IllusionistArtPaths.CardPortrait` returns a stable lowercase
   `res://illusionist/art/cards/<name>.png` path after verifying it with `ResourceLoader.Exists`.
4. RitsuLib's `CardPortraitPathPatch`, `CardPortraitAvailabilityPatch`, and
   `CardAllPortraitPathsPatch` consume `IModCardAssetOverrides.CustomPortraitPath`. RitsuLib therefore
   owns the base-game getter integration and adds the portrait to the normal preload list.
5. `tools/build_pck.gd` treats `illusionist/art/cards` as an imported texture directory and packs each
   `.import` sidecar plus its compiled `.ctex` resource. The supported build script performs Godot's
   import pass before producing the PCK.

Audit counts: 94 registered card classes all use `IllusionistCard`; 95 card PNG files are present,
all 95 have `.import` sidecars, and every referenced compiled `.ctex` was present at audit time. The
extra image is valid because not every shipped card portrait must correspond one-to-one with a
registered reward card.

Illusionist does **not** patch `CardModel.PortraitPath`, `CardModel.HasPortrait`, or the card portrait
`TextureRect`, and it does not keep card `Texture2D` objects in a static cache. Its only direct card
node patches concern unrelated mirror-result animation and the narrowly scoped compatibility guard
documented above.
