## Context

Metamorphosis, Curtain Break, Reckoning, Call the Bluff, and Accrue are the Illusionist's principal damage conversion endpoints. Their current shared 2-Energy cost makes them difficult to draft and hold, while several formulas blur otherwise distinct resources. The implementation spans five card models, one visible power, one hidden combat counter, and two localization sets. Stable content IDs and the current rarities must remain intact.

## Goals / Non-Goals

**Goals:**

- Give each finisher a practical hand cost while retaining a meaningful scaling condition.
- Preserve distinct scaling windows: transformations this turn, intent cards this combat, target Block now, and all enemy Block gained this combat.
- Bound Curtain Break to one trigger per owner turn while preserving the value of playing multiple instances.
- Keep displayed computed damage synchronized with the damage actually dealt.
- Keep English and Simplified Chinese descriptions behaviorally equivalent.

**Non-Goals:**

- Changing card or power stable IDs, artwork, rarity, or unrelated Illusionist cards.
- Redesigning what qualifies as an intent card or a card transformation.
- Changing how Block is granted, broken, or removed elsewhere in the game.
- Editing `next-mirror.md` or performing release/version work.

## Decisions

### Track one combat-long enemy Block total

`CombatBlockGainedCount` will retain one combat-local integer and increment it only when the creature gaining Block belongs to the combat's enemy side. The total deliberately includes every enemy rather than only the selected target, and remains cumulative after Block is lost or an enemy dies.

The alternative of deriving history from the combat event log would add repeated history scans to card rendering and depend on event representations that are not currently part of the card contract. Per-target storage is rejected because the confirmed design pools Block gained by all enemies.

### Make computed damage and execution share one helper per target-aware card

Reckoning remains owner-history based. Call the Bluff will use a target-aware computed-damage delegate and snapshot current Block before removing it. Accrue will read the owner's shared hidden enemy-Block counter, independent of its selected target. Each card calls the same formula helper for its display and executed hit so the two cannot drift apart.

### Bound each Curtain Break instance independently

Each `CurtainBreakPower` instance will keep a `TriggeredThisTurn` flag, reset at the start of its owner's turn and consumed before awaiting draw or Energy commands. A card play draws 1 immediately and installs one power instance. On the first qualifying Block break each turn, that instance draws 1 and grants 1 Energy. Retaining instanced power behavior means duplicate plays remain valuable without allowing one instance to trigger repeatedly in a turn.

The alternative of merging all copies into one shared counter would change the existing replay/stacking identity more broadly than requested.

### Express upgrade changes through existing dynamic variables and keywords

Metamorphosis, Reckoning, Call the Bluff, and Accrue will upgrade their printed damage variables rather than their Energy costs. Curtain Break keeps its existing Innate upgrade. Reckoning and Accrue gain the standard Exhaust keyword at base so their efficient combat-long scaling cannot be replayed freely from hand.

## Risks / Trade-offs

- [Allied monsters and pets also have monster models] → Identify enemies through the active combat's enemy collection rather than treating every non-player creature as an enemy.
- [Nested effects could trigger Curtain Break more than once] → Mark the instance used before awaiting either reward.
- [An enemy can die after accumulating Block] → Preserve the accumulated integer because Accrue intentionally counts all enemy Block gained during the entire combat.
- [Target-aware card previews without a target need a stable fallback] → Show base damage when no target is selected, matching other target-aware computed damage cards.

## Migration Plan

No content-ID or save migration is required. Build and install through `sts2_illusionist/build-illusionist-windows.ps1`; rollback consists of reverting the card, power, counter, and localization edits together.

## Open Questions

None. The five card designs and Curtain Break's Innate upgrade are user-confirmed.
