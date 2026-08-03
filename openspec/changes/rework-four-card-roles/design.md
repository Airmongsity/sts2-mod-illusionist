## Context

Four cards currently miss their intended roles. Solidify Time freezes the whole transmutation engine through a power, Layered Murder resolves a long historical-card cascade, Provoke grants permanent Dexterity while making an attack intent substantially more dangerous, and Silver Lining is primarily an energy card whose Block-transfer condition is difficult to satisfy.

The rework introduces a card-scoped Frozen affliction. Frozen must remain visible on the affected card, survive while the card stays in combat piles, block both forward transmutation and automatic/manual reversion, and disappear when the card is played.

## Goals / Non-Goals

**Goals**

- Give Solidify Time a precise hand-management role through Retain plus Frozen.
- Make Layered Murder a predictable damage payoff for consuming transmutation depth.
- Make Provoke a zero-cost, attack-intent-dependent temporary trade with an energy payoff.
- Make Silver Lining useful without its condition while preserving enemy Block transfer as its payoff.
- Keep the confirmed Chinese card and Frozen wording verbatim.

**Non-Goals**

- Redesign the general transmutation chain or its other payoffs.
- Add a second affliction slot to cards.
- Change the rarity or upgrade paths that were already agreed and not superseded.

## Decisions

### Frozen is a registered affliction with its own RitsuLib asset-profile overlay

The overlay scene follows the vanilla affliction-overlay coordinate contract: the card center is the local origin, so its root rectangle spans `(-150, -211)` to `(150, 211)` with center anchors. These logical coordinates inherit the card and canvas transforms, keeping the overlay aligned independently of display resolution.

Frozen is implemented as a RitsuLib-registered affliction with extra card text. Its `AfflictionAssetProfile` points to a mod-owned `TextureRect` scene that displays `frozen_overlay.webp` at the card's 300×422 logical size. The WebP import sidecar and compiled texture are packaged through the mod's imported-texture directory list. Playing the card clears Frozen.

### Frozen blocks transformation and Illusionist reversion at explicit choke points

A patch on `CardModel.IsTransformable` returns false for Frozen cards as a general signal. Because the runtime can inline this small getter and bypass a Harmony postfix, both forward-transmutation entry points and the final execution loop also reject Frozen cards explicitly. The transmutation revert code skips Frozen chains instead of deleting them, so a frozen card can resume unwinding after Frozen is removed by play.

### Solidify Time selects only cards that can accept Frozen

Solidify Time selects one eligible card in hand, applies Retain, then applies Frozen. Cards carrying a different affliction are excluded because the engine permits only one affliction per card. The card Exhausts; its upgrade changes cost from 1 to 0.

### Layered Murder pays once per reverted layer

Layered Murder first deals 12 damage, then lets the player choose any hand card. If that card has a transmutation chain, it fully reverts and deals one additional 7-damage hit per successfully reverted layer. Frozen cards remain selectable but yield no reversion damage because they cannot change. The upgrade changes cost from 2 to 1.

### Provoke uses two temporary powers

Provoke always grants 2 temporary Dexterity to the player. If the targeted enemy has any attack intent, it receives 3 temporary Strength and the player gains 1 Energy, upgraded to 2 Energy. Both stat changes expire at the end of the affected creature's side turn.

### Silver Lining has an unconditional defensive floor

Silver Lining costs 1 and always grants 5 Block. If its target has Block, all of that Block is removed and granted to the player with `Unpowered` value properties so Dexterity does not amplify the transferred amount. Its upgrade adds Retain.

## Risks / Trade-offs

- A card with an existing non-Frozen affliction cannot be selected by Solidify Time due to the engine's single-affliction model.
- `CardModel.IsTransformable` can be inlined at runtime, so the property patch is defense-in-depth rather than the sole enforcement mechanism.
- The custom overlay depends on its WebP, `.import` remap, compiled `.ctex`, and wrapper scene all being present in the PCK; the full packaging script validates this chain.
- Frozen chains remain tracked while blocked. This is intentional, but long-lived Frozen cards keep the hidden reversion power alive until played or removed.
- Layered Murder can produce several sequential damage actions for a deep chain, trading some animation time for a literal per-layer payoff.
