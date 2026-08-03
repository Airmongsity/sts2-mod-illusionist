## Context

The current rare attack pool has eight cards. Mirror Offering is primarily a hand-keyword enabler, Body Double is primarily defensive redirection, and Break Character overlaps the common Unveil card's global one-layer revert. The transmutation runtime stores predecessor forms privately and only exposes the immediate revert target, so Layered Murder needs a safe one-chain full-revert operation.

The game is difficult to exercise automatically and arbitrary auto-played cards can move between piles, Exhaust, prompt for choices, or modify the transmutation system. The implementation therefore needs deterministic ownership and pile rules rather than relying on a selected card remaining in the hand.

## Goals / Non-Goals

**Goals:**

- Remove Mirror Offering and Break Character from registration and localization.
- Preserve Body Double's stable model ID and behavior while making it a Skill titled 替身 in Simplified Chinese.
- Add three damage-centered rare attacks with distinct build roles.
- Fully unwind one selected transmutation chain without corrupting other chains.
- Execute temporary Exhaust copies of Attack forms revealed by Layered Murder without feeding those copies back into mirrors.
- Keep all transform notifications consistent with ordinary one-layer reverts.

**Non-Goals:**

- Add finished card art for the new cards; the existing default-art fallback remains valid.
- Rebalance unrelated cards or change the normal turn-start revert behavior.
- Add a general-purpose public transmutation scripting API beyond what the new card requires.
- Automate full gameplay validation.

## Decisions

### Layered Murder selects only a transmuted card in hand

The card deals 6 damage, then presents a one-card hand selection filtered by `TransmutePower.GetRevertTarget`. Restricting selection to the hand keeps the interaction understandable and avoids a cross-pile picker.

The selected chain is reverted one layer at a time until no predecessor remains. Every successful layer continues to call `Transmutation.NotifyTransformed`, so existing transform counters and powers remain consistent.

Alternative considered: count the deepest chain and deal repeated fixed damage. This is simpler but loses the requested history-of-forms performance.

### Reverted Attack forms are cloned and executed

After each layer, if the revealed form is a playable Attack, Layered Murder snapshots an Exhaust copy. After the selected card has fully returned to its original form, the snapshots are auto-played in reveal order at seeded random hittable enemies. Completing the unwind before any snapshot executes prevents an Attack side effect such as Shifting Blade from rewriting the chain that is still being traversed.

Generated execution copies use a no-mirror-restore guard so their Exhaust does not load an empty mirror. This prevents a single cascade from both gaining free plays and manufacturing future free plays.

Alternative considered: auto-play the actual reverted card. This was rejected because its result pile and side effects can invalidate the chain before the next layer.

### Full revert is a focused TransmutePower operation

`TransmutePower` gains an internal focused full-revert method that returns snapshots of the forms revealed by each layer. It reuses the same resurrection, transform, chain-update, mirror-pointer, affliction, notification, and empty-power cleanup rules as existing reverts. It does not modify unrelated chains.

The method stops safely if the current form is removed from combat or cannot be transformed.

### Same-name scaling uses finished card-play history

Superimpose (叠影) deals 7 damage plus 7 for each prior finished play of the same model ID by the same owner during the current turn. Because the current play is not yet finished during `OnPlay`, the first copy deals 7, the second 14, and so on. Mirror auto-plays and Replay count when they create normal finished-play history.

### Sugar-Coated Bullet grants unpowered Block after damage

Sugar-Coated Bullet costs 1, deals 18 damage, then grants the surviving target 12 unpowered Block. Its upgrade changes only damage to 24. Applying Block after damage preserves the intended high-efficiency-now/drawback-later profile and avoids player Dexterity modifying enemy Block.

### Removed cards lose their localization entries

Mirror Offering and Break Character are uncommitted additions, so their source files and English/Simplified Chinese localization keys are removed rather than left as hidden registered models.

Body Double retains `BODY_DOUBLE` and its class/file name to avoid unnecessary model identity churn; only its card type and localized Chinese title change.

### Dim Lamp's upgrade increases Energy

Dim Lamp uses an `EnergyVar` with a base value of 1 and an upgraded value of 2. Its effect and localized description read that variable, while its draw remains fixed at 2 cards. This makes the upgrade visible in the card preview and keeps the displayed value synchronized with gameplay.

### Rarity maintenance preserves card identity

False Refuge and Catalyze move from Uncommon to Rare. Solidify Time and Forewarn move from Rare to Uncommon, and Forewarn costs 3 Energy whether upgraded or not. These changes retain every card's stable model ID and existing upgrade effect.

### Encore creates a transmutation chain from the discard pile

Encore becomes a 1-cost Uncommon Skill. It selects one card from the discard pile, leaves that card in place, creates an Extinguished Lamp in hand, and uses the ordinary transmutation path to transform the lamp into a clone of the selection. The visible card therefore has Extinguished Lamp as its registered predecessor and reverts through the existing end-of-turn system. The upgrade reduces Encore's cost to 0.

This deliberately follows Summon's established implementation rather than introducing a second way to register transmutation history. The obsolete end-of-turn Retain recursion power is removed.

### Mirror firing checks Exhaust after the card resolves

Every playable Attack, Skill, or Status fired by a mirror receives the mirror result-pile capability before auto-play, including cards that already had Exhaust. After play, the mirror checks the card's current keyword state:

- an initially Exhaust card that still has Exhaust is spent;
- an initially Exhaust card that removed Exhaust remains stored and is not modified;
- a card that entered without Exhaust remains stored and gains Exhaust as before.

Mirror destruction during the play still takes priority, and Powers retain their normal one-shot behavior. The rule is generic and contains no card-type check for The Show Goes On.

### Mirror result visuals use the post-play destination

The engine captures a card's result location before its effect resolves, but mirror behavior must inspect Exhaust after resolution so a card such as Undying Flame can remove Exhaust and remain loaded. The mirror therefore keeps its provisional mirror result pile, then intercepts only the result animation after the effect has finished:

- if the card still has Exhaust, its single result animation flies directly from the play area to the exhaust pile, while the real Exhaust move and hooks resolve without the native burn-away effect;
- if the card no longer has Exhaust, the normal custom-pile animation flies directly back to the mirror pile.

This preserves the existing post-play semantics without the misleading sequence of flying back to the mirror and then displaying a second Exhaust animation. If the direct-flight visual cannot be created, the normal engine visuals remain the fallback.

### Catalyze advances repeated MoveState instances

Catalyze retains its safety filter: the target's current intent list must be non-empty and consist only of Attack and/or Defend intents. After resolving the next move-state path, it forces the transition even when the resulting MoveState is reference-equal to the current one. The state machine's visible-state log is updated consistently so repetition and cooldown branches observe a skipped turn.

### Cameo is a zero-cost one-card Grand Masquerade

Cameo is a 0-cost Uncommon Skill. It first chooses another playable character using the same signature-starter selection used by Grand Masquerade. It then transmutes one eligible hand card into a random unlocked card from that character. The unupgraded card chooses the hand card with the owner's seeded combat-card-selection RNG; the upgraded card presents a one-card hand picker instead.

The random result also uses the seeded combat-card-selection RNG, and the ordinary `Transmutation.TransmuteCards` path registers the predecessor, dispatches transform payoffs, and restores the original card normally. Cameo is not Exhaust and its upgrade changes control rather than cost.

Character-choice and unlocked-pool construction are shared with Grand Masquerade so the two cards cannot diverge in multiplayer constraints or character availability.

### Mesmerizing Array listens to successful structural intent changes

Mesmerizing Array is a 1-cost Rare Power whose upgrade grants Innate. It has one shared trigger per owner turn. When that trigger is available and the player directly and successfully changes an enemy's intended move, it consumes the trigger, creates mirror images with Copy, and draws cards. Each stack increases both rewards, so two stacks still trigger only once per turn but Copy 2 and draw 2.

Intent-changing cards notify a generic runtime entry point after their modification succeeds. Reversal and Bluff notify after replacing or wrapping the current MoveState, Catalyze notifies after advancing the state machine, and Cut In notifies after applying the hit-count modification. The notification point looks up the owner's power and contains no checks for those card model types, allowing future intent-changing effects to opt into the same contract.

Only direct structural changes count. Strength, Weak, and similar numeric modifiers that merely refresh a displayed intent value do not count, nor do natural enemy state transitions. The per-turn flag resets in the regular player-turn-start phase, before late-phase mirror firing, so a stored intent-changing card fired by a mirror can consume the new turn's trigger.

### Maintenance balance and Cut In diagnostics

Trick Barrage deals 4 damage per hand card and upgrades to 6. Accrue starts at 11 damage and gains 1 damage for every 5 Block gained by any creature this combat. Shield Tax keeps its cumulative 10-Block threshold and 1-to-0-cost upgrade, but its card text states the threshold directly. Undying Flame deals 21 damage and upgrades to 26, while retaining its 3-cost post-play Exhaust-removal role.

Cut In wraps the target's current `MoveState` using the same sequence-preserving pattern as Bluff. The wrapper exposes an intent list with one Attack repeat removed, invokes the original move unchanged, and preserves its `FollowUpState`/`FollowUpStateId`. Its `CutInPower` still removes the corresponding executed hit.

Repeated gameplay logs showed the card consistently entering the multi-attack branch without ever recording the power's synchronous model-hook callback. A postfix on the native `Hook.ModifyAttackHitCount` is therefore the sole execution-time reduction path: it asks the attacker's `CutInPower` to consume remaining reductions after ordinary listeners have run. `CutInPower` deliberately does not override the ordinary virtual hook, and the patch marks each `AttackCommand` after its first visit, so the same command cannot consume the reduction twice.

The generic power-application pipeline can multiply a debuff's amount even though Cut In's structural intent wrapper always removes exactly one visible repeat. After a successful, non-blocked application, Cut In therefore normalizes its power to the previous amount plus one. This preserves Artifact and ordinary application hooks while preventing amount multipliers from making the execution reduction exceed the visible reduction or reduce a multi-attack move to zero.

### Misdirection uses the shared transformation notification

Misdirection is a 0-cost Uncommon Skill that gains 4 Block immediately and applies a temporary 4-Block-per-change power. Its upgrade changes both values to 5. The temporary power stacks additively when multiple copies are played and expires when the owner participates in its side-turn end.

The payoff is dispatched from `Transmutation.NotifyTransformed`, the existing choke point shared by forward transformations, ordinary one-layer reverts, Unveil's batch revert, and every layer of Layered Murder's full revert. A three-layer revert therefore grants Block three times. Exhaust is not a transformation and does not trigger the power.

### Underhand Strike is expendable Common damage

Underhand Strike (暗手) is the new displayed title for the existing `SLEEVE_BLADE` card model. Its class, stable registration stem, cost, damage, Exhaust behavior, and upgrade remain unchanged so existing combat references and saves do not churn solely because its original title resembled the Silent's 袖里藏刀.

It remains a 1-cost Common Attack that deals 11 damage and Exhausts, upgrading to 13 damage. Its efficiency is paid for by being single-use, while the existing mirror system can deliberately turn that Exhaust into future ammunition without requiring any card-specific behavior.

### Transposition exhausts the source through the normal pipeline

Transposition (移花接木) is a 1-cost Uncommon Skill with Exhaust. It selects any card in the owner's draw pile using the native synchronized combat-pile selector. Before moving the source, it snapshots the card with `CreateClone`, preserving upgrades and current combat-local state. It then Exhausts the real source through `CardCmd.Exhaust`, adds Ethereal and Exhaust to the clone, and adds that generated clone to hand.

Using ordinary Exhaust is intentional: the source can fill a mirror or trigger any other Exhaust payoff. Generating a clone is not a transformation and does not call `Transmutation.NotifyTransformed`. The upgrade removes only Transposition's own Exhaust; the fleeting clone always retains both Ethereal and Exhaust.

### One Step Ahead turns enemy defense into recurring draw

One Step Ahead (魔高一丈) retains the `UNINVITED` stable identity as a 0-cost Uncommon Exhaust Skill that draws 1 card and upgrades to draw 2. Its `AfterBlockGained` override is active while the card occupies any combat pile; it does not require the card to have been played. A positive Block gain by a creature in the owner's hittable-enemy list moves that exact card instance to hand with `CardPileCmd.Add`.

The trigger deliberately moves rather than draws the card, so draw hooks do not fire. If the card is already in hand, later Block gains do nothing. Consequently, an effect that gives several enemies Block in sequence returns each One Step Ahead instance at most once, while separate copies independently return. Its own Exhaust creates the intended play-to-exhaust-to-return loop. Moving from the mirror pile unloads that mirror through the ordinary pile system, and moving from the exhaust pile is allowed by the literal “put this card into your hand” wording.

### Shapeshift upgrades by becoming reusable

Shapeshift (幻形) retains the `TRANSMUTE` stable identity, 1-cost Uncommon Skill metadata, candidate-count value, and exhaust-pile-to-hand transmutation effect. It still has Exhaust when unupgraded. Its upgrade no longer increases the number of random exhaust-pile candidates and instead removes Exhaust from Shapeshift itself.

### Foresight selects from two live piles

Foresight remains a 1-cost Uncommon Skill. It presents the actual cards from both the owner's draw and discard piles in one synchronized simple-grid selection, selecting 3 cards before upgrade or 5 after upgrade. When fewer cards exist across those piles, all available cards are offered. The selected cards are moved through `CardPileCmd.Add` to the top of the draw pile, preserving their card instances and removing discard selections from the discard pile.

The next-turn Energy effect resolves independently of whether either source pile contains cards. The old Retain upgrade is removed and replaced by the 3-to-5 selection-count upgrade.

### Concise card text does not narrow transformation semantics

Misdirection's Simplified Chinese description no longer includes the explanatory sentence about transmutation reverts, but its shared transformation notification and per-layer revert triggers remain unchanged. Transposition's Simplified Chinese description is replaced with the exact requested concise sentence; its ordinary Exhaust pipeline, clone keywords, and upgrade behavior are unchanged.

### Frozen cards split into focused and global setup

Superimpose retains its stable registration identity but becomes Freeze Frame, a 1-cost Uncommon Skill that Freezes one selected hand card and upgrades to 0 cost. Solidify Time moves to Rare at 2 cost, upgrades to 1 cost, retains Exhaust, and Freezes every eligible card currently in the hand, draw, discard, exhaust, and mirror piles.

Both cards use the ordinary affliction command without `AfflictAndPreview`. The Frozen overlay still appears on each affected card through RitsuLib's affliction asset profile, but the effect does not open or animate a separate card preview. Solidify Time snapshots the five piles in a fixed order before applying Frozen so callbacks cannot change which cards belong to the resolution.

### Mirror Realm Expansion directly establishes capacity

Mirror Realm Expansion becomes a 1-cost Rare Power. It first applies one stack of the existing mirror-cap buff and then Copies 1, allowing the newly raised cap to admit the created mirror even when the owner was already at the old limit. Its upgrade grants Innate rather than changing cost. The former draw and escalating self-copy chain are removed.

The Mirror keyword explicitly states the base maximum of nine mirrors. Mirror-cap buffs remain capable of raising that limit during combat.

### Localization-only readability maintenance

Layered Murder's Simplified Chinese description inserts a line break immediately after its opening `造成12点伤害` clause. This does not change its damage or revert behavior.

### Upgrade previews isolate the actual change

Provoke already stores its Energy refund in an `EnergyVar`, so its shared description uses `{Energy:diff()}` instead of replacing the entire sentence with `IfUpgraded`. Cameo keeps its existing implementation and scopes `IfUpgraded` only to the clause that changes from a random hand card to a player-selected hand card. Unchanged text therefore remains unhighlighted in both languages.

Silver Lining's base Block changes from 5 to 7 without changing its Retain upgrade. Kindle's base Block changes from 7 to 5 while retaining its existing `+3` Block upgrade, producing 8 Block when upgraded.

## Risks / Trade-offs

- [Arbitrary Attack auto-play may contain choices or special conditions] → Execute only clones whose Attack type is not Unplayable, use normal `CardCmd.AutoPlay`, and stop execution if combat is ending.
- [A reverted Layered Murder form can recursively start another cascade] → Mark cascade-generated copies and suppress the selection/cascade portion when such a copy is auto-played; it still deals its base damage.
- [Transform triggers can move the real reverted card] → Snapshot the revealed form before notifications, complete the full unwind before executing snapshots, and re-resolve the live chain object before each next layer.
- [Executed Exhaust copies could load mirrors] → Register them with an explicit no-restore scope for the duration of auto-play/exhaust resolution.
- [Random target behavior can desync multiplayer] → Use the owner's seeded combat-target RNG, matching native auto-play patterns.
- [No automated gameplay harness exists] → Compile/install with the mandated script and document manual checks for selection, multi-layer chains, no-target endings, mirrors, Improvise, and multiplayer synchronization.
