---
name: illusionist-cards
description: Create and maintain cards (and simple card-powers) for the Illusionist STS2 mod by copying proven templates. Use whenever the task is "add a card", "make a new card", "tweak a card's numbers/cost/rarity", "add a Strike/Defend-like card", or otherwise editing files under sts2_illusionist/Scripts/Cards. The mod runs on the RitsuLib base library — cards self-register via a `[RegisterCard]` attribute, not Entry.cs. Built so a small/cheap model can do routine card work end-to-end (write file, localize, build) without inventing engine APIs.
---

# Illusionist Card Authoring (RitsuLib)

This skill lets you add or edit a card in the **Illusionist** Slay the Spire 2 mod by copying a template and filling in numbers. It is deliberately mechanical. **Do not invent engine APIs.** If a card needs an effect that is **not** in the [effect cookbook](./references/effect-cookbook.md), STOP and escalate (see [Escalate](#when-to-stop-and-escalate)).

> **Architecture note (read once).** The mod is built on **RitsuLib**. That changes three things from a raw decompile+patch mod, and this skill bakes them in:
> 1. Cards derive from **`IllusionistCard`** (our RitsuLib base — see `Scripts/IllusionistContent.cs`), *not* `CardModel`. The base already pins the card pool and the art convention, so you never write a `Pool` override.
> 2. A card registers itself with a **`[RegisterCard(...)]` attribute** on the class. There is **no** `Entry.cs` edit and **no** `ModHelper.AddModelToPool` line anymore.
> 3. Content IDs and loc keys are **mod-namespaced**: `ILLUSIONIST_CARD_<STEM>`. You set `<STEM>` once, in the attribute.

## The 4 steps (every card, every time)

1. **Write + register** the card file: `sts2_illusionist/Scripts/Cards/<Name>Illusionist.cs` (the `[RegisterCard]` attribute *is* the registration).
2. **Localize** it: add 2 lines to `sts2_illusionist/illusionist/localization/eng/cards.json` (and, for this mod, the matching `zhs/cards.json`).
3. **Build**: run `sts2_illusionist/build-illusionist-windows.ps1` and confirm `0 个错误` (0 errors).
4. **Hand off to the user to test** (you cannot launch the game). Tell them what to verify.

Get the `[RegisterCard]` attribute wrong (missing, wrong pool type, wrong stem) or skip step 2 and the card won't appear or shows a raw `ILLUSIONIST_CARD_…` key. Follow all 4 every time.

---

## Step 1 — Write + register the card file

Create `sts2_illusionist/Scripts/Cards/<Name>Illusionist.cs`. Pick the closest template below and edit only the numbers/types.

### Naming rule (used by steps 1 & 2)
- **Class name** = `<PascalName>Illusionist` — PascalCase with the **`Illusionist` suffix** (e.g. `FireSlashIllusionist`, `MirrorImageIllusionist`). Every card in the mod follows this; keep it. (The suffix keeps our class names distinct from base-game ones, and for powers/monsters — whose IDs are class-name-derived and cannot be pinned — it is what prevents ID collisions.)
- **STEM** = the PascalName **without** the `Illusionist` suffix, in `UPPER_SNAKE_CASE`: `FireSlashIllusionist` → `FIRE_SLASH`, `ShiftingWaveIllusionist` → `SHIFTING_WAVE`.
  Rule: drop the `Illusionist` suffix, insert `_` before every capital except the first, uppercase everything.
- You put the STEM in the attribute as `StableEntryStem = "<STEM>"`. RitsuLib then derives the content ID and the loc-key prefix as **`ILLUSIONIST_CARD_<STEM>`**. That prefix is what step 2 localizes.
- **Pick a distinctive PascalName** (`Unveil`, not `Expose`) so the STEM/loc key stays readable and unique within the mod.

### Template A — Attack (deal damage)
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>Card name (中文名) — describe effect here.</summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "FIRE_SLASH")]
public sealed class FireSlashIllusionist : IllusionistCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move),
    };

    public FireSlashIllusionist()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
```
> `IllusionistCard`, `IllusionistCardPool`, `IllusionistKeywords` and the `FirstMove` helper all live in the enclosing `Illusionist.Scripts` namespace, so a card in `Illusionist.Scripts.Cards` sees them **without** a `using`. (Existing files sometimes add a redundant `using Illusionist.Scripts;` — harmless.) The one import you must not forget is `using STS2RitsuLib.Interop.AutoRegistration;` for `[RegisterCard]`.

### Template B — Skill (gain Block)
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>Card name (中文名) — describe effect here.</summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "IRON_WALL")]
public sealed class IronWallIllusionist : IllusionistCard
{
    public override bool GainsBlock => true; // REQUIRED for any card that gains Block

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(8m, ValueProp.Move),
    };

    public IronWallIllusionist()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(3m);
    }
}
```

### Template C — Attack + debuff (damage and apply Weak/Vulnerable/Frail)
Start from Template A, add `using MegaCrit.Sts2.Core.Models.Powers;` and `using MegaCrit.Sts2.Core.HoverTips;`, add a power var, apply it, and add the hover tip.
```csharp
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
        new PowerVar<WeakPower>(2m),       // or VulnerablePower / FrailPower
    };

    // Give the [gold]Weak[/gold] in the card text its hover box (ExtraHoverTips is sealed by the
    // template — override AdditionalHoverTips instead):
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>() };

    // ...inside OnPlay, after the DamageCmd line:
        await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, base.DynamicVars.Weak.BaseValue, base.Owner.Creature, this);

    // ...in OnUpgrade:
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars.Weak.UpgradeValueBy(1m);
```

For every other effect (draw, energy, multi-hit, AoE, gain Strength, custom keywords, etc.) copy the exact snippet from the **[effect cookbook](./references/effect-cookbook.md)**.

### Constructor reference
`base(energyCost, CardType, CardRarity, TargetType)` — same signature the RitsuLib `IllusionistCard` base forwards.
- **energyCost**: `0`, `1`, `2`, `3` …
- **CardType**: `CardType.Attack`, `CardType.Skill`, `CardType.Power`
- **CardRarity**: `CardRarity.Basic` (starter, not offered as reward), `Common`, `Uncommon`, `Rare`
- **TargetType**: `TargetType.AnyEnemy` (you pick one enemy; use `cardPlay.Target`), `TargetType.Self`, `TargetType.AllEnemies`, `TargetType.None`

### Card art (optional — the user supplies it)
The `IllusionistCard` base auto-resolves the portrait by convention: drop an imported PNG at
`sts2_illusionist/illusionist/art/cards/<stem-lower>.png`, where `<stem-lower>` is the class name with
the `Illusionist` affix stripped and lowercased (`ShiftingWaveIllusionist` → `shiftingwave.png`,
`FireSlashIllusionist` → `fireslash.png`). If the file is absent the card falls back to the engine
default art — the card still works. Art is gitignored and ships in the PCK; **you do not wire any
portrait path in code**, and creating the image itself is the user's job unless they ask otherwise.

---

## Step 2 — Localize the card

Open `sts2_illusionist/illusionist/localization/eng/cards.json`. Add two entries (mind the trailing comma on the line before yours; the last entry in the file has no comma):
```json
  "ILLUSIONIST_CARD_FIRE_SLASH.title": "Fire Slash",
  "ILLUSIONIST_CARD_FIRE_SLASH.description": "Deal {Damage:diff()} damage."
```
- Key prefix = **`ILLUSIONIST_CARD_<STEM>`**, where `<STEM>` is the `StableEntryStem` you set in step 1.
- **Use dynamic placeholders, not hard-coded numbers** — write `{Damage:diff()}`, never `8`. The
  game renders card text with SmartFormat, and `{Var:diff()}` shows the live value: it updates on
  upgrade (turns **green** in the upgrade preview) and reflects in-combat buffs (Strength on damage,
  Dexterity on Block). This is how real STS2 cards work.
- **Every placeholder must match a `DynamicVar` you declared in `CanonicalVars`** (step 1). The var
  name is the placeholder key: `DamageVar`→`{Damage}`, `BlockVar`→`{Block}`, `PowerVar<WeakPower>`→
  `{WeakPower}` (and `{VulnerablePower}`/`{FrailPower}`/`{StrengthPower}`/`{DexterityPower}`). If a
  placeholder has no matching var, the card shows the literal `{Damage:diff()}` text — a visible bug.
- **Never write engine keyword words** ("Exhaust.", "Retain.", "Innate.") in the description. The
  engine auto-renders them in gold from `CanonicalKeywords`. Writing them yourself makes them appear
  **twice**.
- **Color + hover every game term** like real STS2: wrap the keyword word in `[gold]…[/gold]`
  (`Apply {WeakPower:diff()} [gold]Weak[/gold].`, ZH `给予 {WeakPower:diff()} 层[gold]虚弱[/gold]。`)
  **and** give it a hover-tip in the `.cs` — `GainsBlock => true` auto-adds the Block tip; every engine
  power needs `AdditionalHoverTips => new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>() }`. Our
  **custom** mechanics (Copy / Mirror Image / First Move / Transmute) are real RitsuLib keywords now —
  you add them via `CanonicalKeywords`, and the tooltip is automatic. See the cookbook's
  *[Custom mechanics](./references/effect-cookbook.md#custom-mechanics-are-ritsulib-keywords)* section.
- **Keep text terse, STS2-style** — no parenthetical explanations.
- See the cookbook's **[Dynamic card text](./references/effect-cookbook.md#dynamic-card-text)**
  section for the full formatter list and worked examples.

If your card applies a custom **power** you created, also add `.title`/`.description` to
`illusionist/localization/eng/powers.json` (key = `ILLUSIONIST_POWER_<POWER_STEM>`).

### Chinese (this mod ships 简体中文)

The Illusionist is fully localized in English **and** Simplified Chinese, so add the same two keys to
`illusionist/localization/zhs/cards.json` too. English is the fallback, so a missing `zhs` entry shows
English rather than crashing — but for this mod, ship both.

Translate **only the prose**. Copy these verbatim, never translate them:
- the `{Damage:diff()}` / `{Block:diff()}` … placeholders (and any `:diff()`/`:show:` formatter);
- any `[gold]…[/gold]`-style BBCode tags;
- the `.title` is the card name (translate it), the placeholder *keys* are not.

Mirror the English entry's structure exactly — if English uses a `{…}` placeholder, the translation
must use the identical token, or that language shows raw `{…}` text. Keyword words still auto-render
per language, so don't write "Exhaust"/"消耗" yourself there either. Save the file as **UTF-8**. See
`localization/zhs/cards.json` for a complete worked example. (Other languages fall back to English;
the build packs any `localization/<lang>/` folder automatically.)

---

## Step 3 — Build

Run from the repo root (PowerShell):
```
sts2_illusionist/build-illusionist-windows.ps1
```
Success looks like `0 个警告` / `0 个错误`, then `PCK built:` and `Installed to:`. If you see compile errors, read them:
- "未能找到类型或命名空间名" / "does not contain a definition" → you used an API not in the cookbook, or forgot `using STS2RitsuLib.Interop.AutoRegistration;` for `[RegisterCard]`. Fix it to a cookbook snippet, or **escalate**.
- A missing `using` → add the `using` shown in the template you copied.
- **`Copy-Item … IOException` / file "正被另一个进程使用"** at the very end, *after* `PCK built` and `0 个错误` → the compile succeeded; only the copy into the game folder failed because **Slay the Spire 2 is running**. Ask the user to fully quit the game, then re-run. This is not a code error.
- JSON build is fine even with a bad comma, but the game won't show your text — re-check step 2.

---

## Step 4 — Hand off to test

You cannot run the game. Tell the user to start an Illusionist run and verify the new/edited card: its text, cost, rarity, that the effect and the upgraded values work, and that no raw `ILLUSIONIST_CARD_…` key shows on the face (= a missed/mismatched loc key). Ask them to send any `[illusionist]` log lines if something is off.

---

## Balance defaults (when the user doesn't give numbers)

Rough single-target baselines at base / upgraded, 1 energy:
- Common attack: ~6–9 dmg → +3. Common block: ~6–8 → +3.
- Apply Weak/Vulnerable/Frail: 1–2 stacks; upgrade +1.
- Higher rarity or a downside (Exhaust, end turn) buys ~30–50% more value.
- Prefer **no randomness** in this mod unless the user explicitly asks for it.

If the user gives numbers, use theirs exactly.

---

## When to STOP and escalate

This skill covers **routine** cards built from known snippets. Stop and ask the user (or a stronger model / the `sts2-ritsulib-mod` skill) when the card needs any of:
- An effect with **no snippet** in the cookbook (e.g. summon an entity, manipulate the draw pile, copy/replay cards, custom triggered behavior).
- A **brand-new keyword** (registering one is a `[RegisterOwnedCardKeyword]` + loc job — see `Scripts/IllusionistKeywords.cs`; reusing the four existing keywords is in scope, creating a new one is not).
- A **new custom Power class** with its own combat hooks (only simple "apply an existing power" is in scope here).
- Touching the card pool, relics, characters, patches, the RitsuLib registration plumbing, or build scripts.

Do not guess at engine method names. A wrong API name compiles into a crash the user has to discover. When unsure, escalate.

## Resources

- [references/effect-cookbook.md](./references/effect-cookbook.md) — verified copy-paste snippets for every supported effect (damage variants, block, debuffs, buffs, draw, energy, exhaust, custom keywords, reading enemy intent) and the DynamicVar reference. **Always pull effect code from here.**
- `Scripts/IllusionistContent.cs` — the `IllusionistCard` base class (and the art-path convention).
- `Scripts/IllusionistKeywords.cs` — the four custom keywords (Copy / Mirror Image / First Move / Transmute).
- The existing cards in `sts2_illusionist/Scripts/Cards/` are the best worked examples — copy the closest one (e.g. `Ambush.cs` for First Move, `Unveil.cs` for an Exhaust AoE debuff, `Accrue.cs` for a self-transmuting attack).
- For anything beyond a single card — a new keyword, power, relic, the pool, the character — use the **`sts2-ritsulib-mod`** skill.
