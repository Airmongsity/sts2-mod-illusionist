# RitsuLib API map (v0.4.46, game API 0.107.1)

Compact lookup tables. Verified against RitsuLib source (`STS2-RitsuLib/src/`) during a real
character-mod migration in 2026-07. Class names differ from BaseLib — the BaseLib column is only
for translating older tutorials.

## Template base classes

| Content | Game base | RitsuLib template | BaseLib name (for old docs) |
|---|---|---|---|
| Card | `CardModel` | `ModCardTemplate` | `CustomCardModel` |
| Relic | `RelicModel` | `ModRelicTemplate` | `CustomRelicModel` |
| Potion | `PotionModel` | `ModPotionTemplate` | `CustomPotionModel` |
| Power | `PowerModel` | `ModPowerTemplate` | `CustomPowerModel` |
| Enchantment | `EnchantmentModel` | `ModEnchantmentTemplate` | `CustomEnchantmentModel` |
| Monster | `MonsterModel` | `ModMonsterTemplate` | — |
| Orb | `OrbModel` | `ModOrbTemplate` | — |
| Event / Ancient | `EventModel` | `ModEventTemplate` / `ModAncientEventTemplate` | `CustomAncientModel` |
| Encounter / Act | — | `ModEncounterTemplate` / `ModActTemplate` | `CustomEncounterModel` |
| Character | `CharacterModel` | `ModCharacterTemplate<TCardPool,TRelicPool,TPotionPool>` | `PlaceholderCharacterModel` |
| Card pool | `CardPoolModel` | `TypeListCardPoolModel` | `CustomCardPoolModel` |
| Relic pool | `RelicPoolModel` | `TypeListRelicPoolModel` | `CustomRelicPoolModel` |
| Potion pool | `PotionPoolModel` | `TypeListPotionPoolModel` | `CustomPotionPoolModel` |

Namespaces: templates in `STS2RitsuLib.Scaffolding.Content`; character scaffolding in
`STS2RitsuLib.Scaffolding.Characters`; asset-override interfaces in
`STS2RitsuLib.Scaffolding.Content.Patches`.

## Registration attributes (`STS2RitsuLib.Interop.AutoRegistration`)

Require `ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly)` in the initializer.

| Attribute | On | ID override? | Notes |
|---|---|---|---|
| `[RegisterCard(typeof(TPool))]` | card class | `StableEntryStem`, `FullPublicEntry` | also `Order`, `Inherit` |
| `[RegisterRelic(typeof(TPool))]` | relic class | same | |
| `[RegisterPotion(typeof(TPool))]` | potion class | same | |
| `[RegisterCharacter]` | character class | **no** — class name IS the id | |
| `[RegisterPower]` / `[RegisterMonster]` / `[RegisterEnchantment]` / `[RegisterOrb]` / `[RegisterAct]` | class | **no** | standalone models |
| `[RegisterCharacterStarterCard(typeof(TChar), count)]` | card class | — | starter deck (character-side `StartingDeck*` are Obsolete) |
| `[RegisterCharacterStarterRelic/Potion(typeof(TChar))]` | relic/potion class | — | |
| `[RegisterOwnedCardKeyword("stem")]` | any class | — | id = `MOD_KEYWORD_STEM`; `IconPath`, `CardDescriptionPlacement`, `IncludeInCardHoverTip` |
| `[RegisterOwnedCardTag("stem")]` | any class | — | custom `CardTag` |
| `[RegisterTouchOfOrobasRefinement(typeof(UpgradedRelic))]` | starter relic | — | Orobas ancient relic upgrade map |
| `[RegisterArchaicToothTranscendence(typeof(AncientCard))]` | starter card | — | Archaic Tooth map |
| `[RegisterDustyTomeCard(typeof(TChar))]` | ancient card | — | Darv's Dusty Tome candidate |
| `[RegisterSharedCardPool]` etc. | pool class | — | multi-class shared pools |
| `[RegisterSharedEvent]` / `[RegisterActEvent(typeof(Act))]` / `[RegisterSharedAncient]` / `[RegisterActAncient(...)]` / `[RegisterActEncounter(...)]` | event/encounter | — | |
| Timeline/epoch: `[RegisterEpoch]`, `[RegisterStory]`, `[AutoTimelineSlot*]`, `[UnlockEpochAfter*]`, `[RequireEpoch(...)]` | epoch/content | — | only if you build a timeline story |

Alternative styles for everything above: fluent
`RitsuLibFramework.CreateContentPack(ModId)....Apply()` or direct
`RitsuLibFramework.GetContentRegistry(ModId).RegisterCard<TPool,TCard>(...)`.

## ID / localization-key formats

| Thing | Format | Example |
|---|---|---|
| Model entry (default) | `{MOD}_{CATEGORY}_{TYPENAME}` | `MYMOD_CARD_FIRE_STRIKE` |
| Categories | root model class minus `Model` | CARD, RELIC, POTION, POWER, CHARACTER, MONSTER, ENCHANTMENT |
| With `StableEntryStem="X"` | `{MOD}_{CATEGORY}_X` | `MYMOD_CARD_FIRE` |
| With `FullPublicEntry="X"` | `X` verbatim (normalized) | legacy-id preservation |
| Keyword | `{MOD}_KEYWORD_{STEM}` | `MYMOD_KEYWORD_BURNING` |
| Loc key | `{entry}.{field}` | `MYMOD_CARD_FIRE_STRIKE.title` |

Normalization: non-alphanumerics → `_`, camel/acronym boundaries split, uppercased.
Unregistered classes keep the vanilla derivation `Slugify(typeName)` (no mod prefix).

## Localization files (`<pck>/<modid>/localization/<lang>/`)

`cards.json`, `relics.json`, `potions.json`, `powers.json`, `characters.json`,
`card_keywords.json`, `enchantments.json`, `events.json`, `ancients.json`, `epochs.json`,
`static_hover_tips.json`, `card_library.json`. Languages: `eng` (mandatory fallback), `zhs`, `jpn`, …
Powers support `description` (static), `smartDescription` (dynamic vars), `remoteDescription`
(multiplayer). Character files also need pronoun keys; ancient dialogue lives in `ancients.json`
under `{ANCIENT}.talk.{CHARACTER_ENTRY}.*`.

Card text SmartFormat quick list (see tutorial `05-variable-and-description`):
`{Damage:diff()}`, `{Block:diff()}`, `{Cost:inverseDiff()}`, `{X:abs()}`,
`{IfUpgraded:show(upgraded|normal)}`; var names come from `DynamicVar` types
(`DamageVar`→`{Damage}`, `PowerVar<WeakPower>`→`{WeakPower}`, second same-type var needs an
explicit name). Engine auto-renders keywords like Exhaust/Retain — don't write them in JSON.

## Asset override surfaces

| Model | Override members | Profile record |
|---|---|---|
| Card | `CustomPortraitPath`, `CustomFramePath`, `CustomEnergyIconPath`, `CustomBannerTexturePath`, ancient variants, materials | `CardAssetProfile` |
| Relic | `CustomIconPath`, `CustomIconOutlinePath`, `CustomBigIconPath` | `RelicAssetProfile` |
| Potion | `CustomImagePath`, `CustomOutlinePath` | `PotionAssetProfile` |
| Power | `CustomIconPath`, `CustomBigIconPath` (interface: `IModPowerAssetOverrides`) | `PowerAssetProfile` |
| Character | `AssetProfile` (Scenes / Ui / Vfx / Spine / Audio / Multiplayer / VisualCues / WorldProceduralVisuals / per-id vanilla overrides) + `PlaceholderCharacterId` fallback merge | `CharacterAssetProfile` |
| Pool | `PoolFrameMaterial` (or `CardFrameMaterialPath`), `TextEnergyIconPath`, `BigEnergyIconPath` | — |

All these paths are loaded via **ResourceLoader** → textures must ship **imported** in the PCK
(`.import` + `.ctex`), not as raw bytes. `MaterialUtils` (in `STS2RitsuLib.Utils`):
`CreateReplaceHueShaderMaterial(r,g,b)`, `CreateHsvShaderMaterial(h,s,v)`,
`CreateUnmodulatedHsvShaderMaterial()`.

## Character template override points

| Member | Purpose |
|---|---|
| `PlaceholderCharacterId` | vanilla character whose assets fill every null profile field (`ironclad`/`silent`/`defect`/`regent`/`necrobinder`) |
| `AssetProfile` | your own asset paths (per-field, merge-with-placeholder) |
| `RequiresEpochAndTimeline => false` | no timeline story; RitsuLib guards vanilla per-character epoch/unlock code |
| `HideFromVanillaCharacterSelect`, `AllowInVanillaRandomCharacterSelect`, `HideInCardLibraryCompendium`, `CardLibraryCompendiumPlacementRules` | visibility |
| `TryCreateCreatureVisuals()` | build/instantiate the combat visuals scene yourself (e.g. load the placeholder scene, swap the Spine skeleton) |
| `SetupCustomCreatureAnimator(MegaSprite)` | full Spine animator graph |
| `SetupCustomCombatAnimationStateMachine(Node, CharacterModel)` | non-Spine / custom state machine |
| `SetupCustomMerchantAnimationStateMachine(...)`, `WorldProceduralVisuals` | shop / rest-site bodies |
| `UnlocksAfterRunAsType` | unlock prerequisite (`{Prerequisite}` in unlockText) |

Sealed by the template (don't try to override): `CardPool`, `RelicPool`, `PotionPool`,
`StartingDeck`, `StartingRelics`, `StartingPotions`, `UnlocksAfterRunAs`.

## Patch system (`STS2RitsuLib.Patching`)

- `IPatchMethod`: static `PatchId`, `Description`, `IsCritical` (default true), `GetTargets()`;
  patch methods named `Prefix`/`Postfix`/`Transpiler`/`Finalizer` (any visibility, static).
- `ModPatchTarget(type, name [, paramTypes] [, ignoreIfMissing] [, MethodType])` —
  `MethodType.Getter` for property getters, `ignoreIfMissing: true` for version-optional targets.
- `ModPatcher` via `RitsuLibFramework.CreatePatcher(modId, name)`; `RegisterPatch<T>()` (extension
  in `STS2RitsuLib.Patching.Core`), `RegisterPatches<TGroup>()` (`IModPatches`), `PatchAll()`;
  `RitsuLibFramework.ApplyRequiredPatcher(patcher, disableModCallback)` for must-succeed sets.
- `PrivateAccess` wraps `AccessTools` with required-member checks; `HarmonyIl*` helpers for
  transpilers; `DynamicPatchBuilder` for runtime-discovered targets.

## Other subsystems (exist — reach for them before hand-rolling)

Settings UI (`RegisterModSettings`), persistence (`GetDataStore`, `GetRunSavedDataStore`,
Steam-cloud slots), lifecycle events (`SubscribeLifecycle<TEvent>` — `MainMenuReadyEvent`,
`CombatStartingEvent`, `CardPlayedEvent`, `RunSavedEvent`, ...), update checker
(`RegisterModUpdateCheck`), FMOD audio, top-bar buttons, custom card piles, custom targeting,
max-hand-size, toasts, card PNG / compendium exporters, browser debug-log viewer
(`http://127.0.0.1:<port>` printed in the log).

## Known compile-time traps

1. Templates seal `ExtraHoverTips` → override `AdditionalHoverTips`; it is `protected` (even where
   the game member was `public` — adjust visibility when migrating).
2. Card-template `RegisteredKeywordIds` is Obsolete → use `CanonicalKeywords` +
   `"ID".GetModCardKeyword()` (`STS2RitsuLib.Keywords`). Relic/potion/power templates still use
   `RegisteredKeywordIds` (string ids) — those are fine.
3. `TypeList*PoolModel` legacy `CardTypes`/`RelicTypes`/`PotionTypes` hooks duplicate attribute
   registration — never use both.
4. Powers subclassing game powers can't use `ModPowerTemplate` → implement
   `IModPowerAssetOverrides` (3 members: `AssetProfile`, `CustomIconPath`, `CustomBigIconPath`;
   `PowerAssetProfile` lives in `STS2RitsuLib.Scaffolding.Content`).
5. `RegisterCharacter`/`RegisterPower` cannot pin legacy IDs — plan class names first; migrating
   an old mod means re-keying those loc files no matter what.
6. `min_game_version` is required in `mod_manifest.json` on current loaders; dependency is the
   object form `{ "id": "STS2-RitsuLib", "min_version": "..." }` (old branches used plain strings).
