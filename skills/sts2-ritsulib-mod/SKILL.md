---
name: sts2-ritsulib-mod
description: Build a Slay the Spire 2 content mod (character, cards, relics, potions, powers, keywords) on the RitsuLib base library instead of raw decompile+Harmony patches. Use when creating a new STS2 mod, adding a playable character, registering content, or migrating an existing patch-based mod to RitsuLib. Trigger on mentions of RitsuLib, STS2-RitsuLib, ModCharacterTemplate, ModCardTemplate, RegisterCard, or "STS2 character mod".
---

# STS2 Mod Development with RitsuLib

Build Slay the Spire 2 mods on **RitsuLib** (github.com/BAKAOLC/STS2-RitsuLib), the community base
library. RitsuLib owns the fragile glue — character roster injection, asset fallbacks, epoch/unlock
guards, content registration, keyword hover tips, per-patch failure isolation — so your mod contains
almost only game content. A hand-rolled decompile+patch character mod needs ~18 Harmony patches;
the same mod on RitsuLib needs single-digit patches, and survives game updates far better.

**Ground rule: patch the game with Harmony only as a last resort.** Before writing any patch, check
whether RitsuLib already covers it (§9). This skill was distilled from a real migration (the
Illusionist mod, 18 patches → 8) plus the tutorials at `tutorials.sts2modding.com` and RitsuLib's
own `docs/pages/guide/*.md`.

Detailed class/attribute tables live in [references/api-map.md](./references/api-map.md).

## 1. Prerequisites

- Game version ≥ 0.107.x (check `<game>/release_info.json`); RitsuLib latest targets the current API.
- .NET 9 SDK, **Godot 4.5.1 Mono** (the game cannot load PCKs from 4.6.x).
- Compile-time: NuGet package `STS2.RitsuLib` (or `STS2.RitsuLib.Compat.<api-version>` for older
  game branches). Runtime: the **STS2-RitsuLib mod** must be installed — Steam Workshop item
  **3747602295**, or auto-deployed locally via `RitsuLibDeployDir` (below).

## 2. Project scaffold

`<mod>.csproj`:

```xml
<Project Sdk="Godot.NET.Sdk/4.5.1">
  <PropertyGroup>
    <AssemblyName>mymod</AssemblyName>
    <TargetFramework>net9.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <Nullable>enable</Nullable>
    <Sts2Root Condition="'$(Sts2Root)' == ''">C:\...\Slay the Spire 2</Sts2Root>
    <Sts2DataDir>$(Sts2Root)\data_sts2_windows_x86_64</Sts2DataDir>
    <!-- The NuGet package auto-installs the RitsuLib MOD here for local testing.
         Players get it via the Workshop dependency instead. -->
    <RitsuLibDeployDir>$(Sts2Root)\mods\STS2-RitsuLib\</RitsuLibDeployDir>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="sts2"><HintPath>$(Sts2DataDir)\sts2.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="0Harmony"><HintPath>$(Sts2DataDir)\0Harmony.dll</HintPath><Private>false</Private></Reference>
    <PackageReference Include="STS2.RitsuLib" Version="0.4.46" />
  </ItemGroup>
</Project>
```

`mod_manifest.json` (ships next to the dll/pck in `<game>/mods/<id>/`):

```json
{
  "id": "mymod",
  "name": "My Mod",
  "author": "you",
  "version": "0.1.0",
  "min_game_version": "0.107.1",
  "dependencies": [{ "id": "STS2-RitsuLib", "min_version": "0.4.46" }],
  "has_pck": true,
  "has_dll": true,
  "affects_gameplay": true
}
```

Steam Workshop is separate: in the uploader's `workshop.json`, `"dependencies": [3747602295]`
(numeric Workshop IDs) so subscribers get prompted to install RitsuLib.

## 3. Entry point

```csharp
[ModInitializer(nameof(Init))]
public static class Entry
{
    public const string ModId = "mymod";
    public static Logger Logger { get; private set; } = null!;

    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        Logger = RitsuLibFramework.CreateLogger(ModId);

        // REQUIRED for all [Register*] attribute auto-registration:
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        // Only if your .tscn scenes carry C# scripts:
        // RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);

        // Only if you have Harmony patches (see §9):
        // var patcher = RitsuLibFramework.CreatePatcher(ModId, "main");
        // patcher.RegisterPatch<MyPatch>();
        // patcher.PatchAll();
    }
}
```

Key namespaces: `STS2RitsuLib`, `STS2RitsuLib.Interop`, `STS2RitsuLib.Interop.AutoRegistration`,
`STS2RitsuLib.Scaffolding.Content`, `STS2RitsuLib.Scaffolding.Characters`,
`STS2RitsuLib.Patching.{Core,Models}`, `STS2RitsuLib.Keywords`, `STS2RitsuLib.Utils`.

## 4. Content IDs and localization keys — decide this FIRST

Every RitsuLib-registered model gets the entry `{MOD}_{CATEGORY}_{TYPENAME}` (all slugged upper
snake_case), e.g. mod `mymod` + card class `FireStrike` → `MYMOD_CARD_FIRE_STRIKE`. **This entry IS
the localization key stem and the save-file identity.** Localization files live at
`<pck>/<modid>/localization/{eng,zhs,...}/{cards,relics,potions,powers,characters,card_keywords,...}.json`
with flat keys like `"MYMOD_CARD_FIRE_STRIKE.title"`. The engine auto-loads them; `eng` is the
mandatory fallback.

- On `[RegisterCard/RegisterRelic/RegisterPotion]` you can override:
  `StableEntryStem = "FIRE_STRIKE"` (→ `MYMOD_CARD_FIRE_STRIKE`) or `FullPublicEntry = "..."`
  (verbatim — use to preserve legacy IDs when migrating an existing mod without breaking saves).
- **Characters and powers have NO id override** — their entry is always derived from the class
  name. Name those classes exactly what you want the ID to be.
- Changing IDs later invalidates players' discovered-cards records and in-run saves (harmless
  `Unknown card ID` warnings, but progress resets). Decide before first release.

## 5. A playable character = 3 pools + 1 character class

Pools (one file each; content registers *itself* into them, never list types in the pool):

```csharp
public sealed class MyCardPool : TypeListCardPoolModel
{
    public override string Title => "mymod";
    public override string EnergyColorName => "mymod";        // or borrow e.g. "necrobinder"
    public override Color DeckEntryCardColor => new("CD4EED");
    public override Material? PoolFrameMaterial =>             // card frame tint, pick ONE:
        MaterialUtils.CreateHsvShaderMaterial(0.025f, 0.5f, 0.42f);   // vanilla frame recolor
    public override string? TextEnergyIconPath => "res://mymod/art/energy.webp"; // 24px, in-text
    public override string? BigEnergyIconPath  => "res://mymod/art/energy.webp"; // 74px, cost orb
}
public sealed class MyRelicPool : TypeListRelicPoolModel { public override string EnergyColorName => "mymod"; }
public sealed class MyPotionPool : TypeListPotionPoolModel { public override string EnergyColorName => "mymod"; }
```

Character:

```csharp
[RegisterCharacter]
public sealed class MyChar : ModCharacterTemplate<MyCardPool, MyRelicPool, MyPotionPool>
{
    public override int StartingHp => 70;
    public override int StartingGold => 99;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override Color NameColor => new("4FC3F7");

    // No timeline story? Set false — RitsuLib then guards ALL the per-character epoch/unlock
    // code paths the base game hardcodes for its own five characters.
    public override bool RequiresEpochAndTimeline => false;

    // THE killer feature: any asset you don't declare falls back per-field to this vanilla
    // character (scenes, icons, sfx, energy counter, merchant/rest bodies, transitions...).
    public override string? PlaceholderCharacterId => "ironclad"; // or silent/defect/regent/necrobinder

    public override CharacterAssetProfile AssetProfile => new(
        Ui: new CharacterUiAssetSet(
            IconTexturePath: "res://mymod/art/avatar_small.png",       // top bar (texture)
            IconPath: "res://mymod/scenes/my_icon.tscn",               // in-run HUD icon (SCENE)
            CharacterSelectBgPath: "res://mymod/scenes/my_bg.tscn",    // select backdrop (SCENE)
            CharacterSelectIconPath: "res://mymod/art/avatar_med.png"));// select button portrait
}
```

Starter deck/relics go ON the content classes, not the character (the character-side
`StartingDeck*` hooks are `[Obsolete]`):

```csharp
[RegisterCard(typeof(MyCardPool))]
[RegisterCharacterStarterCard(typeof(MyChar), 4)]   // 4 copies
public sealed class MyStrike : ModCardTemplate { ... }

[RegisterRelic(typeof(MyRelicPool))]
[RegisterCharacterStarterRelic(typeof(MyChar))]
public sealed class MyStarterRelic : ModRelicTemplate { ... }
```

Scene requirements (node names are load-bearing) and Spine/animation options: see the tutorial's
`04-14-add-new-character` and `04-15-2-character-animation` pages; override points on the template
are `TryCreateCreatureVisuals()`, `SetupCustomCreatureAnimator(MegaSprite)`,
`SetupCustomCombatAnimationStateMachine(...)`, `SetupCustomMerchantAnimationStateMachine(...)`.

## 6. Content classes

All content = a template subclass + a `[Register*]` attribute. No manual pool lists, no Entry
registration code.

```csharp
[RegisterCard(typeof(MyCardPool))]
public sealed class FireStrike : ModCardTemplate
{
    public FireStrike() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
    };

    public override CardAssetProfile AssetProfile =>
        new(PortraitPath: "res://mymod/art/cards/fire_strike.png");

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .Targeting(play.Target!).Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
```

- Relics: `ModRelicTemplate` + `[RegisterRelic(typeof(Pool))]`; icons via `CustomIconPath` /
  `CustomIconOutlinePath` / `CustomBigIconPath` (or `RelicAssetProfile`).
- Potions: `ModPotionTemplate` + `[RegisterPotion(typeof(Pool))]`; `CustomImagePath`.
- Powers: `ModPowerTemplate` + `[RegisterPower]`; `CustomIconPath` / `CustomBigIconPath`.
- Monsters/enchantments/orbs/events/acts: `ModMonsterTemplate`, `ModEnchantmentTemplate`, ... with
  the matching `[Register*]` attribute (see api-map).
- **Scale trick**: with many cards, add one abstract intermediate base (`MyCard : ModCardTemplate`)
  that derives `CustomPortraitPath` from `GetType().Name` by convention and pins the pool — each
  card is then just a class + attribute.

### Template gotchas (compile-time traps)

- Card/relic/power templates **seal `ExtraHoverTips`** — override `AdditionalHoverTips` instead,
  and match its visibility (`protected`, even where the game's original member was `public`).
- A power that must subclass a *game* power (e.g. `TemporaryStrengthPower`) can't use the template;
  implement `IModPowerAssetOverrides` directly for its icon.
- `TypeList*PoolModel.CardTypes`/`RelicTypes` are `[Obsolete]` legacy hooks — listing types there
  **duplicates** the attribute registration. Leave them alone.
- Vanilla integrations are declarative — never patch these: `[RegisterTouchOfOrobasRefinement(typeof(UpgradedRelic))]`
  on your starter relic, `[RegisterArchaicToothTranscendence(typeof(AncientCard))]` on your starter
  card, `[RegisterDustyTomeCard(typeof(MyChar))]` on your Ancient-rarity card.

## 7. Keywords (custom mechanic hover tips)

```csharp
[RegisterOwnedCardKeyword("burning")]          // → id MYMOD_KEYWORD_BURNING
public sealed class MyKeywordRegistrations { }
```

Text goes in `localization/<lang>/card_keywords.json`
(`"MYMOD_KEYWORD_BURNING.title"` / `.description`). Attach to content:

- Cards: `public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { "MYMOD_KEYWORD_BURNING".GetModCardKeyword() };`
  (cache the id in a static helper; the card-template `RegisteredKeywordIds` is obsolete).
- Relics/potions/powers: `protected override IEnumerable<string> RegisteredKeywordIds => new[] { "MYMOD_KEYWORD_BURNING" };`
- Runtime: `card.AddModKeyword(...)` / `HasModKeyword(...)`.
- Default `CardDescriptionPlacement.None` = hover tip only; the term stays hand-written in your
  card text.

## 8. Assets & PCK — the #1 runtime trap

A mod ships `dll + pck + mod_manifest.json`. Two ways bytes get into the PCK, and they are NOT
interchangeable:

- **Raw files** (default when packing a folder): loadable only via `FileAccess`. `ResourceLoader`
  CANNOT see them.
- **Imported resources** (`.import` sidecar + compiled `.ctex` from a `godot --headless --import`
  pass): loadable via `ResourceLoader`.

**Everything RitsuLib loads for you — asset-profile paths, `Custom*Path`, keyword icons, pool
energy icons, `.tscn` `ExtResource` textures — goes through `ResourceLoader`, so those textures
must ship IMPORTED.** Symptoms of getting this wrong: art works on your machine (editor-imported
project) but is blank/NOPE for subscribers. Either export the PCK through Godot's export dialog
(imports everything), or if you pack with a custom `PCKPacker` script, explicitly include the
`.import` + `res://.godot/imported/*.ctex` chain for every ResourceLoader-loaded texture.
Localization JSON and `.tscn` text scenes pack fine as raw files.

## 9. Patching — last resort only

RitsuLib already handles (do NOT patch): character roster/select screen, asset path redirection,
epoch & unlock guards (`RequiresEpochAndTimeline`), card-library compendium placement, DustyTome
safety, Orobas/ArchaicTooth maps, keyword hover-tip plumbing, mod-content ID identity.

For the rest, use RitsuLib's wrapper — per-patch isolation, version-tolerant targets:

```csharp
public sealed class MyPatch : IPatchMethod
{
    public static string PatchId => "mymod_thing";
    public static string Description => "One line of what & why";
    public static bool IsCritical => false;        // false = failure logs & skips, mod keeps running
    public static ModPatchTarget[] GetTargets() => new ModPatchTarget[]
    {
        new(typeof(SomeGameType), "SomeMethod"),                          // any overload
        new(typeof(SomeGameType), "Prop", MethodType.Getter),             // property getter
        new(typeof(SomeGameType), "MaybeGone", ignoreIfMissing: true),    // optional per game version
    };
    private static void Postfix(SomeGameType __instance /* , ref T __result */) { ... }
}
```

Register in `Init` via `patcher.RegisterPatch<MyPatch>(); patcher.PatchAll();`. The log prints a
per-patcher report: `Patch application complete: N applied, X ignored, Y failed`.

## 10. Build, run, verify

1. `dotnet build` (restores NuGet; RitsuLib auto-deploys to `mods/STS2-RitsuLib/` via
   `RitsuLibDeployDir`).
2. Import + pack the PCK (Godot 4.5.1 headless), copy `dll + pck + mod_manifest.json` to
   `<game>/mods/<id>/`.
3. Launch and read the log (`%APPDATA%`-side godot log). Healthy markers:
   - `Calling initializer method ... STS2RitsuLib.RitsuLibFramework` **before** your mod
     (dependency ordering works),
   - your `[Content] Registered card: X (id=MYMOD_CARD_X) -> MyCardPool` lines,
   - `[Patcher - ...] All patches applied successfully`.
4. In-game sweep: character select (portrait/bg/name), embark (starter deck+relic), one combat
   (body, energy orb+counter, card frames, portraits, power icons), rest site, shop, card library,
   keyword hover tips, **both languages**. Raw `MYMOD_CARD_...` text on screen = missing loc key.
5. Old-save noise is benign: `Unknown card ID` / `Unknown character ID` progress-parse warnings
   just mean records from before an ID change; they are preserved, not fatal.

## Reference material

- `tutorials.sts2modding.com` (repo-local mirror): `01-env-setup`, `03-choose-base-library`,
  `04-ritsulib/*` (esp. `04-14-add-new-character`, `04-24-patch-system`, `04-27-content-registry`),
  `05-variable-and-description` (SmartFormat `{Damage:diff()}` etc.), `08-migration-baselib-to-ritsulib`,
  `11-upload-workshop`.
- RitsuLib source + bilingual guides: `STS2-RitsuLib/docs/pages/guide/*.md` (getting-started,
  content-authoring-toolkit, asset-profiles-and-fallbacks, patching-guide,
  localization-and-keywords, character-and-unlock-scaffolding).
- Worked example: the Illusionist mod (`sts2_illusionist/`) — a full standalone character on
  RitsuLib (57 cards, 6 relics, 18 powers, custom keywords, 8 residual patches).
- [references/api-map.md](./references/api-map.md) — compact tables: template classes, attributes,
  ID formats, loc files.
