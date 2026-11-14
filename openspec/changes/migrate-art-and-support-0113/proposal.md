## Why

Slay the Spire II is now installed at version 0.113.0, while this checkout targets 0.111.0 and still uses the previous visual assets. The author supplied a complete, checksum-verified art migration package and requested adoption of every visual category except the character-selection page, with a recoverable backup of the current assets.

## What Changes

- Preserve the current art, scenes, visual-loading source, and build configuration in a local ZIP backup before replacing resources.
- Adopt the supplied runtime card, power, relic, potion, UI, event, multiplayer, affliction, character-body, Spine, shader, rest, and merchant resources.
- Preserve the current character-selection scene, background variants, portrait variants, and character-selection asset configuration byte-for-byte.
- Selectively merge the package's required visual controllers, scene integration, animation probes, and import/packing support without replacing gameplay or localization changes already present in this checkout.
- Update compilation dependencies and native API integrations as required for the installed game version 0.113.0.
- Build, pack, and install the resulting DLL, PCK, and manifest through the supported Windows workflow; validate source assets, packed resources, and representative runtime visuals.

## Capabilities

### New Capabilities

- `art-migration-with-select-preservation`: Defines recoverable asset migration, preservation of the existing character-selection artwork, visual runtime integration, and 0.113.0 build/install verification.

### Modified Capabilities

None.

## Impact

- Local ignored art and scene resources under `sts2_illusionist/illusionist/`.
- Character visual asset profiles, body rendering, combat FX, rest, shop, and event integration.
- `sts2_illusionist/build-illusionist-windows.ps1`, its PCK helper, project dependencies, and compatibility metadata.
- New local backup under `art-backups/`; no source art is staged or published.
- Existing content stable IDs and user-confirmed card behavior remain authoritative. Version release, Git push, and Workshop upload are outside this request.
