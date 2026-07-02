# Effect Cookbook (verified)

Every snippet here is copied from working Illusionist cards. Use these **verbatim**. If your effect is not listed, do not improvise — escalate (see SKILL.md).

Conventions used below:
- `choiceContext` and `cardPlay` are the two `OnPlay` parameters.
- `base.Owner` = the **Player**. `base.Owner.Creature` = the player's Creature.
- `cardPlay.Target` = the chosen enemy Creature (only meaningful for `TargetType.AnyEnemy`).
- All numbers are `decimal` — write `6m`, not `6`.

---

## Dynamic card text

Card descriptions (`cards.json`) are **not** plain text — the game runs them through SmartFormat.
Write every number as a `{Var:diff()}` placeholder so it updates on upgrade, turns **green** in the
upgrade preview, and reflects in-combat buffs (Strength on damage, Dexterity on Block). This is how
real STS2 cards read.

**The rule:** every `{...}` placeholder must name a `DynamicVar` you declared in `CanonicalVars`
(see the [DynamicVar reference](#dynamicvar-reference)). The placeholder key **is** the var name. If
a placeholder has no matching var, SmartFormat fails and the card shows the literal text
`{Damage:diff()}` — a visible bug. Add the var, then reference it.

### Placeholder syntax

| Write | Renders | When to use |
|-------|---------|-------------|
| `{Damage:diff()}` | the value, green if higher than printed / red if lower | **every** number — the default |
| `{HpLoss:inverseDiff()}` | same, but colors **inverted** (green when lower) | numbers where *lower is better* (a cost / self HP-loss you reduce on upgrade) |
| `{StrengthPower:abs()}` | absolute value (no minus sign) | a var holding a negative number (e.g. `-1` Strength) |
| `{IfUpgraded:show:upgraded text\|normal text}` | first option if upgraded, second if not (preview greens the first). **Colon after `show`, not parens** — `show(...)` silently renders nothing. One option only (`{IfUpgraded:show:X}`) shows X only when upgraded; empty first option (`{IfUpgraded:show:\|Y}`) shows Y only when NOT upgraded. A literal `\n` inside is fine. | a **non-numeric** clause an upgrade adds/removes/swaps. The `IfUpgraded` var is always available — you do **not** declare it. Numbers never need this; `diff()` already greens them. |

### Var name = placeholder key

`DamageVar`→`{Damage}` · `BlockVar`→`{Block}` · `CardsVar`→`{Cards}` · `EnergyVar`→`{Energy}` ·
`HpLossVar`→`{HpLoss}` · `PowerVar<WeakPower>`→`{WeakPower}` (likewise `{VulnerablePower}`,
`{FrailPower}`, `{StrengthPower}`, `{DexterityPower}`) · a plain `new DynamicVar("Bonus", 4m)`→`{Bonus}`.

A **second var of the same type** needs an explicit name, or `DynamicVarSet` throws on the duplicate
key: `new BlockVar("ExtraBlock", 4m, ValueProp.Move)` → `{ExtraBlock}`.

### Keywords render themselves — never type them

Do **not** write "Exhaust.", "Retain.", "Innate.", "Ethereal." in the description. The engine
auto-renders them in gold from your `CanonicalKeywords`/`AddKeyword`. Typing them yourself makes them
appear **twice**. Keep the rest terse, STS2-style — no parenthetical explanations.

### Keyword color + hover (格挡 / 虚弱 / 易伤 …)

Real STS2 cards color every game term **gold** and give it a hover box. Two halves, both required:

1. **Color the word in the loc text** — wrap the keyword word (not the number) in `[gold]…[/gold]`.
   The `{Var:diff()}` number stays right before it. This is exactly what the base game ships:
   - EN: `Gain {Block:diff()} [gold]Block[/gold].` · `Apply {WeakPower:diff()} [gold]Weak[/gold].`
   - ZH: `获得 {Block:diff()} 点[gold]格挡[/gold]。` · `给予 {WeakPower:diff()} 层[gold]虚弱[/gold]。`
   (the base game writes ZH with no spaces, e.g. `获得{Block:diff()}点[gold]格挡[/gold]。` — either is fine, just be consistent.) Gold all of: 格挡 Block, 虚弱 Weak, 易伤 Vulnerable, 脆弱 Frail,
   力量 Strength, 敏捷 Dexterity, plus our own [gold]-able terms.
2. **Add the hover-tip in the card's `.cs`** (the box that pops beside the card):
   - **Block**: set `public override bool GainsBlock => true;` — the engine then adds the Block tip
     automatically (no `AdditionalHoverTips` entry needed).
   - **Any engine power** (Weak/Vulnerable/Frail/Strength/Dexterity/…): add it explicitly. The
     RitsuLib `IllusionistCard` template **seals `ExtraHoverTips`**, so override
     **`AdditionalHoverTips`** (protected) instead:
     ```csharp
     using MegaCrit.Sts2.Core.HoverTips;
     protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
         new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>() };
     ```
     Multiple powers → list them all in the one array.
   - **Custom mechanics** (Copy / Mirror Image / First Move / Transmute) do **not** go here — they are
     RitsuLib keywords; add them through `CanonicalKeywords` (see
     [Custom mechanics](#custom-mechanics-are-ritsulib-keywords)).

### Worked examples (raw JSON → on card)

Loc keys are `ILLUSIONIST_CARD_<STEM>.<field>` (the `<STEM>` is the `StableEntryStem` from the card's
`[RegisterCard]` attribute):
```json
// damage + debuff   (vars: DamageVar, PowerVar<WeakPower>; card adds FromPower<WeakPower>())
"ILLUSIONIST_CARD_BLIND.description": "Deal {Damage:diff()} damage. Apply {WeakPower:diff()} [gold]Weak[/gold]."
// multi-var conditional   (vars: DamageVar + new DynamicVar("Bonus", 4m))
"ILLUSIONIST_CARD_RIPOSTE.description": "Deal {Damage:diff()} damage. If the enemy intends to attack, deal {Bonus:diff()} extra damage."
// second, named Block var   (vars: BlockVar + BlockVar("ExtraBlock", …); card sets GainsBlock=true)
"ILLUSIONIST_CARD_LAST_STAND.description": "Gain {Block:diff()} [gold]Block[/gold]. If you have no mirror images, gain {ExtraBlock:diff()} extra [gold]Block[/gold]."
// Exhaust card — keyword NOT written, engine appends it in gold (card adds FromPower<VulnerablePower>())
"ILLUSIONIST_CARD_UNVEIL.description": "Apply {VulnerablePower:diff()} [gold]Vulnerable[/gold] to ALL enemies."
// non-numeric upgrade clause
"ILLUSIONIST_CARD_SOME_CARD.description": "Deal {Damage:diff()} damage.{IfUpgraded:show:\nDraw a card.}"
```

See `Scripts/Cards/Blind.cs`, `Riposte.cs`, `Ambush.cs`, `Unveil.cs`, `LastStand.cs`, `Aging.cs` for
the matching card files.

---

## Deal damage (single target)
`TargetType.AnyEnemy`. Needs `new DamageVar(8m, ValueProp.Move)` in `CanonicalVars`.
```csharp
ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
    .WithHitFx("vfx/vfx_attack_slash")
    .Execute(choiceContext);
```

## Deal damage multiple times (e.g. 3×4)
Add `.WithHitCount(n)`:
```csharp
await DamageCmd.Attack(4m).FromCard(this).Targeting(cardPlay.Target)
    .WithHitCount(3)
    .WithHitFx("vfx/vfx_attack_slash")
    .Execute(choiceContext);
```

## Deal damage to ALL enemies (AoE)
Use `TargetType.AllEnemies` in the constructor. Get the combat state and target all opponents:
```csharp
ICombatState? combat = base.Owner.Creature.CombatState;   // using MegaCrit.Sts2.Core.Combat;
if (combat == null || combat.HittableEnemies.Count == 0) return;
await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this)
    .TargetingAllOpponents(combat)
    .WithHitFx("vfx/vfx_attack_slash")
    .Execute(choiceContext);
```
Random enemy instead: `.TargetingRandomOpponents(combat)`.

## Gain Block
Set `public override bool GainsBlock => true;` and add `new BlockVar(8m, ValueProp.Move)` to `CanonicalVars`.
```csharp
await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
```

## Apply a debuff to the target enemy
Add a `using MegaCrit.Sts2.Core.Models.Powers;`. Powers you can apply this way:
- `WeakPower` — target deals 25% less attack damage.
- `VulnerablePower` (易伤) — target takes 50% more damage.
- `FrailPower` (脆弱) — target gains 25% less Block.
```csharp
await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, 2m, base.Owner.Creature, this);
```
With a var for upgradeability, add `new PowerVar<WeakPower>(2m)` to `CanonicalVars` and pass `base.DynamicVars.Weak.BaseValue`. For Vulnerable use `base.DynamicVars.Vulnerable.BaseValue`. For Frail there is no typed accessor — use the indexer `base.DynamicVars["FrailPower"].BaseValue`.

## Buff yourself with Strength / Dexterity
```csharp
await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner.Creature, 2m, base.Owner.Creature, this);
await PowerCmd.Apply<DexterityPower>(choiceContext, base.Owner.Creature, 2m, base.Owner.Creature, this);
```
Negative amounts are allowed (`-1m`) — these powers lower the stat.

## Draw cards
Add `new CardsVar(2)` to `CanonicalVars`.
```csharp
await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
```

## Gain energy
```csharp
await PlayerCmd.GainEnergy(1, base.Owner);
```

## End your turn (drawback)
Not awaited:
```csharp
PlayerCmd.EndTurn(base.Owner, canBackOut: false);
```

## Read the enemy's intent (conditional effects)
`Monster` is null for non-monsters — always null-check.
```csharp
// Does THIS target intend to attack?
bool targetAttacks = cardPlay.Target.Monster != null && cardPlay.Target.Monster.IntendsToAttack;

// Does ANY enemy intend to attack? (needs ICombatState combat, see AoE snippet)
bool anyAttacks = combat != null
    && combat.Enemies.Any(e => e.IsAlive && e.Monster != null && e.Monster.IntendsToAttack);   // using System.Linq;
```

## 先机 / First Move (bonus only on the first card played this turn)
Two halves: the **keyword** (gold word + automatic hover box) and the **runtime check** (the gate).

Add the keyword so the card shows the 先机 tooltip:
```csharp
public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { IllusionistKeywords.FirstMove };
```
Then wrap the bonus part of `OnPlay` in the runtime check:
```csharp
if (FirstMove.IsActive(base.Owner.Creature))
{
    // bonus effect here, e.g. gain block:
    await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
}
```
`FirstMove` (the runtime helper) and `IllusionistKeywords` both live in `Illusionist.Scripts`, so they resolve from a card in `Illusionist.Scripts.Cards` **without** a `using`. `FirstMove.IsActive` is true only when this is the first card the player has played this turn. Still write the term in the card text (`[gold]First Move[/gold]` / `[gold]先机[/gold]`). See `Scripts/Cards/Ambush.cs` for a complete example.

## Top-deck control: move a card from the discard pile to the top of the draw pile
Lets the player pick one card from their discard pile and put it on top of the draw pile (like Ironclad's Headbutt). Add `using System.Linq;`, `using MegaCrit.Sts2.Core.CardSelection;`.
```csharp
CardModel? selected = (await CardSelectCmd.FromCombatPile(
    context: choiceContext,
    pile: PileType.Discard.GetPile(base.Owner),
    player: base.Owner,
    prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1))).FirstOrDefault();
if (selected != null)
{
    await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Top);
}
```
- `selected` is **nullable** (`CardModel?`) — the discard pile may be empty; always null-check, do not declare it as non-nullable `CardModel` (compiler warning + risk).
- Other piles: `PileType.Draw`, `PileType.Hand`, `PileType.Discard`, `PileType.Exhaust`. Other positions: `CardPilePosition.Top`, `CardPilePosition.Bottom`.
- `base.SelectionScreenPrompt` is the prompt shown on the picker. **It is NOT free — using it REQUIRES a loc key** `"ILLUSIONIST_CARD_<STEM>.selectionScreenPrompt"` in `cards.json`, or the card throws `InvalidOperationException: No selection screen prompt` and freezes when played. Add it whenever a card uses `CardSelectCmd.FromHand`/`FromCombatPile` with `base.SelectionScreenPrompt`:
```json
  "ILLUSIONIST_CARD_FATE_LOOM.selectionScreenPrompt": "Choose a card to put on top of your draw pile."
```
See `Scripts/Cards/FateLoom.cs` for a complete example.

## Replay (a card is played one extra time)
**This card replays itself** (permanent, unconditional) — increment its own replay counter:
```csharp
BaseReplayCount += 1;   // playCount = BaseReplayCount + 1, so +1 = "Replay 1"
```
**Give another card Replay** (pick a card in hand and make it replay, Transfigure-style) — add `using MegaCrit.Sts2.Core.CardSelection;`:
```csharp
IEnumerable<CardModel> picked = await CardSelectCmd.FromHand(
    choiceContext, base.Owner, new CardSelectorPrefs(base.SelectionScreenPrompt, 1), null, this);
foreach (CardModel card in picked)
{
    card.BaseReplayCount += 1;
}
```
(`CardSelectCmd.FromHand` uses `base.SelectionScreenPrompt` → you MUST add a `"ILLUSIONIST_CARD_<STEM>.selectionScreenPrompt"` loc key, same as the top-deck-control snippet above, or the card freezes.)

**Put a copy of THIS card into a pile** (e.g. a self-replicating card, like Ironclad's Anger). `CreateClone()` copies this card's full state (cost/upgrade/Replay). **Wrap the add in `CardCmd.PreviewCardPileAdd(result, 2.2f)`** or the copy appears with no visible animation:
```csharp
CardModel copy = CreateClone();
copy.BaseReplayCount = BaseReplayCount + 1;   // optional tweak
CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Discard, base.Owner), 2.2f);
```
The copy lasts only for this combat (generated cards don't persist to the run deck). See `Scripts/Cards/Echo.cs` (and Ironclad's `Anger`).

> **Advanced (escalate):** a CONDITIONAL replay (e.g. "First Move: Replay 1" — only replays when it's the first card that turn) is NOT a one-liner. It needs a custom `EnchantmentModel` overriding `EnchantPlayCount`, applied with `CardCmd.Enchant<T>(card, amount)`, plus an `enchantments.json` loc entry. See `Scripts/Enchantments/FirstMoveReplay.cs` + `Scripts/Cards/Reshape.cs` for the worked example, but do not write a new enchantment yourself — escalate.

---

## Card keywords

### Exhaust (card is removed for the combat after one play)
```csharp
public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };
```
Remove it on upgrade (in `OnUpgrade`): `RemoveKeyword(CardKeyword.Exhaust);`

---

## Custom mechanics are RitsuLib keywords

Our own terms — **Copy**, **Mirror Image** (镜像), **First Move** (先机), **Transmute** (幻化) — are
registered RitsuLib **owned card keywords** (`Scripts/IllusionistKeywords.cs`). They render the gold
word **and** their hover box automatically, just like an engine keyword. You do **not** build
`HoverTip`s by hand, and the old `IllusionHoverTips` file no longer exists.

To give a card a custom keyword, add it to `CanonicalKeywords` via the `IllusionistKeywords` helper
(it lives in `Illusionist.Scripts`, so no `using` is needed from a card in `Illusionist.Scripts.Cards`):
```csharp
public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
{
    IllusionistKeywords.Copy,
    IllusionistKeywords.MirrorImage,
};
```
The four available keywords:

| Helper | 中文 / EN | Use on cards that… |
|--------|-----------|--------------------|
| `IllusionistKeywords.Copy` | 复制 / Copy | say "Copy N" (create mirror images) |
| `IllusionistKeywords.MirrorImage` | 镜像 / Mirror Image | reference mirror images at all (create, read, or destroy) |
| `IllusionistKeywords.FirstMove` | 先机 / First Move | have a first-card-of-the-turn bonus (see the First Move snippet) |
| `IllusionistKeywords.Transmute` | 幻化 / Transmute | transform / 幻化 a card |

A card that references mirror images usually wants **both** the action and the entity tip — a Copy
card returns `{ IllusionistKeywords.Copy, IllusionistKeywords.MirrorImage }` so hovering shows the
**Copy** box and the **镜像 / Mirror Image** box.

Mix custom and engine keywords freely in the one array (engine ones are `CardKeyword.Exhaust` /
`CardKeyword.Retain` / `CardKeyword.Innate`):
```csharp
public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
{
    CardKeyword.Exhaust,
    IllusionistKeywords.Transmute,
};
```

Still gold-wrap the term in the card text (`[gold]Copy[/gold]`, `[gold]First Move[/gold]`,
`[gold]mirror image(s)[/gold]`; ZH `[gold]复制[/gold]` / `[gold]先机[/gold]` / `[gold]镜像[/gold]` /
`[gold]幻化[/gold]`). The ZH mirror-token noun is **镜像** everywhere. Creating a *brand-new* keyword
(a fifth term) means a `[RegisterOwnedCardKeyword]` registration plus `card_keywords.json` loc — that
is **out of scope for this skill; escalate**.

**Replay is built-in — do NOT add a custom keyword or tip for it.** `CardModel.BaseReplayCount` /
`GetEnchantedReplayCount()` make the engine auto-append the "Replay N" card text and its hover-tip.
Express Replay through the card's replay-count / an enchantment, never a hardcoded "Replay 1" string.

For a card that grants Block via a *power* at start of turn (not on play, e.g. Dazzle) don't set
`GainsBlock` — add the Block tip explicitly:
`AdditionalHoverTips => new IHoverTip[] { HoverTipFactory.Static(StaticHoverTip.Block) }`.

**Line breaks:** put each sentence of a card description on its own line — a literal `\n` after every
sentence-ending period (ZH `。`, EN `. `), e.g. `"造成 {Damage:diff()} 点伤害。\n先机：……"`. No `\n`
after the final period, and none in selection prompts.

**Upgrades must be visible:** any number that `OnUpgrade` changes MUST be a `{Var:diff()}` placeholder
(not a hardcoded digit) or the card face won't update when upgraded. Cost-only upgrades show via the
cost pip automatically; effect-swap upgrades (e.g. drop "End your turn.") need `{IfUpgraded:show:A|B}`.

---

## Upgrades (`OnUpgrade`)

```csharp
base.DynamicVars.Damage.UpgradeValueBy(3m);   // +3 damage
base.DynamicVars.Block.UpgradeValueBy(3m);    // +3 block
base.DynamicVars.Weak.UpgradeValueBy(1m);     // +1 Weak stack
base.EnergyCost.UpgradeBy(-1);                // cost -1
AddKeyword(CardKeyword.Innate);               // gain Innate on upgrade ("Innate when upgraded")
RemoveKeyword(CardKeyword.Exhaust);           // drop Exhaust on upgrade
```
`AddKeyword`/`RemoveKeyword` in `OnUpgrade` is how you toggle a keyword only on the upgraded card
(e.g. `Summon`/`Accrue` gain `Innate` when upgraded). Any number `OnUpgrade` changes must be a
`{Var:diff()}` placeholder in the loc text, or the card face won't show the change.

---

## DynamicVar reference

Declare vars in `CanonicalVars` (an array). Common ones:

| Var | Use | Typed accessor | Read value |
|-----|-----|----------------|------------|
| `new DamageVar(8m, ValueProp.Move)` | attack damage | `base.DynamicVars.Damage` | `.BaseValue` |
| `new BlockVar(8m, ValueProp.Move)` | block (needs `GainsBlock => true`) | `base.DynamicVars.Block` | `.BaseValue` |
| `new PowerVar<WeakPower>(2m)` | Weak stacks | `base.DynamicVars.Weak` | `.BaseValue` |
| `new PowerVar<VulnerablePower>(2m)` | Vulnerable stacks | `base.DynamicVars.Vulnerable` | `.BaseValue` |
| `new PowerVar<FrailPower>(1m)` | Frail stacks | *(none — use indexer)* | `base.DynamicVars["FrailPower"].BaseValue` |
| `new PowerVar<StrengthPower>(2m)` | Strength | `base.DynamicVars.Strength` | `.BaseValue` |
| `new PowerVar<DexterityPower>(2m)` | Dexterity | `base.DynamicVars.Dexterity` | `.BaseValue` |
| `new CardsVar(2)` | number of cards (draw) | `base.DynamicVars.Cards` | `.BaseValue` |
| `new DynamicVar("Bonus", 4m)` | any extra number | *(none — use indexer)* | `base.DynamicVars["Bonus"].BaseValue` |

- `.BaseValue` is a `decimal`; `.IntValue` is an `int`.
- A `PowerVar<T>` is keyed by the power's type name (`"WeakPower"`, `"FrailPower"`, …); that's why the indexer key for Frail is `"FrailPower"`.
- You can have **only one** var keyed `"Damage"`. For a second number (e.g. a conditional bonus), use `new DynamicVar("Bonus", 4m)` and read it with the indexer.

---

## Required `using` directives (superset)

Most cards need a subset of these. If the compiler says a name is missing, add the matching line:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;                       // ICombatState
using MegaCrit.Sts2.Core.Commands;                     // DamageCmd, CreatureCmd, PowerCmd, CardPileCmd, PlayerCmd
using MegaCrit.Sts2.Core.Entities.Cards;               // CardType, CardRarity, TargetType, CardKeyword
using MegaCrit.Sts2.Core.Entities.Creatures;           // Creature
using MegaCrit.Sts2.Core.GameActions.Multiplayer;      // PlayerChoiceContext
using MegaCrit.Sts2.Core.Localization.DynamicVars;     // DynamicVar, DamageVar, BlockVar, PowerVar<>, CardsVar
using MegaCrit.Sts2.Core.HoverTips;                    // IHoverTip, HoverTipFactory (for AdditionalHoverTips)
using MegaCrit.Sts2.Core.Models;                       // CardModel, ModelDb
using MegaCrit.Sts2.Core.Models.Powers;                // WeakPower, VulnerablePower, FrailPower, StrengthPower, DexterityPower
using MegaCrit.Sts2.Core.ValueProps;                   // ValueProp
using STS2RitsuLib.Interop.AutoRegistration;           // [RegisterCard] — ALWAYS needed
```
`IllusionistCard`, `IllusionistCardPool`, `IllusionistKeywords`, and the `FirstMove` helper live in
`Illusionist.Scripts` and resolve **without** a `using` (a card's namespace, `Illusionist.Scripts.Cards`,
is nested inside it). You never import `MegaCrit.Sts2.Core.Models.CardPools` — the base class pins the
pool, so `NecrobinderCardPool` and other pool types are gone from card files.
