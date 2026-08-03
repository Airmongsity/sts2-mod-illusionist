## Why

The Illusionist rare attack pool contains cards whose primary value reads more like a Skill, while its transmutation and duplicate-card mechanics still lack distinct attack-centric payoffs. This change sharpens card-type identity and adds three rare attacks that reward deep transmutation, repeated identities, and the enemy-Block package.

## What Changes

- Remove the rare attacks Mirror Offering (镜前献技) and Break Character (破相).
- Rename Body Double's Chinese title from 替身戏法 to 替身 and change it from an Attack to a Skill while preserving its effect and stable model identity.
- Add Layered Murder (层层杀机), a rare attack that fully unwinds one transmuted hand card and executes Exhaust copies of the Attack forms revealed along the way.
- Add Afterimage Stack (叠影), a rare attack whose damage grows with prior same-name plays during the current turn.
- Add Sugar-Coated Bullet (糖衣炮弹), a high-efficiency rare attack that grants its target Block after dealing damage.
- Fix Dim Lamp's upgrade so it gains 2 Energy instead of 1 while continuing to draw 2 cards.
- Promote False Refuge and Catalyze to Rare; move Solidify Time and Forewarn to Uncommon, with Forewarn costing 3 Energy in both upgrade states.
- Rework Encore into a 1-cost Uncommon Skill that selects a discard-pile card and adds an Extinguished Lamp transmuted from a copy of it to hand; its upgrade reduces the cost to 0.
- Make mirror firing decide whether an initially Exhaust card is spent from its post-play Exhaust keyword, so cards that remove Exhaust during play remain loaded without card-specific checks.
- Make mirror-fired cards visually fly directly to their post-play destination: still-Exhaust cards fly to the exhaust pile without an extra exhaust effect, while surviving cards fly to the mirror pile.
- Make Catalyze advance the monster state machine even when the following turn resolves to the same MoveState instance.
- Add Cameo (客串), a 0-cost Uncommon Skill that chooses another character and transmutes one random hand card into a random card from that character; upgrading Cameo lets the player choose the hand card.
- Add Mesmerizing Array (迷魂阵), a 1-cost Rare Power whose upgrade grants Innate; once per player turn, the first successful direct change to an enemy's intent copies and draws cards.
- Route successful player-driven intent changes through one generic notification point instead of coupling the new power to specific card model types.
- Rebalance Trick Barrage and Accrue, simplify Shield Tax's wording, and make Cut In reduce both the visible attack intent and the executed hit count.
- Add Misdirection (障眼法), a 0-cost Uncommon Skill that grants Block now and again for every card transformation this turn, including each transmutation-revert layer.
- Add Underhand Strike (暗手), a 1-cost Common Exhaust Attack that deals 11 damage and upgrades to 13, while retaining the existing `SLEEVE_BLADE` stable identity.
- Add Transposition (移花接木), a 1-cost Uncommon Exhaust Skill that exhausts a selected draw-pile card and adds an Ethereal, Exhaust clone of it to hand; upgrading removes Transposition's own Exhaust.
- Add One Step Ahead (魔高一丈), retaining the `UNINVITED` stable identity as a 0-cost Uncommon Exhaust Skill that draws 1 and returns itself from any combat pile to hand whenever an enemy gains Block; upgrading increases its draw to 2.
- Rename Transmute to Shapeshift (幻形) while retaining `TRANSMUTE`; its upgrade now removes Exhaust instead of increasing the exhaust-pile candidate count.
- Rework Foresight into a 1-cost Uncommon Skill that selects 3 cards across the draw and discard piles, puts them on top of the draw pile, and grants 1 Energy next turn; upgrading increases the selection to 5.
- Use the requested concise Simplified Chinese card text for Cameo, Misdirection, Transposition, and Foresight without changing their underlying effects beyond the stated maintenance.
- Increase Undying Flame's damage to 21 before upgrade and 26 after upgrade.
- Rework Superimpose into Freeze Frame, a 1-cost Uncommon Skill that Freezes one hand card and upgrades to 0 cost while preserving the `SUPERIMPOSE` stable identity.
- Promote Solidify Time to a 2-cost Rare Skill that Freezes every card in the hand, draw, discard, exhaust, and mirror piles and upgrades to 1 cost.
- Rework Mirror Realm Expansion into a 1-cost Rare Power that raises the mirror cap by 1 and Copies 1; its upgrade grants Innate.
- Apply Frozen without an additional card-preview animation, document the nine-mirror base cap in the Mirror keyword, and split Layered Murder's Simplified Chinese description after its opening damage clause.
- Limit Provoke and Cameo upgrade-preview highlighting to the value or phrase that actually changes, raise Silver Lining's Block from 5 to 7, and lower Kindle's base Block from 7 to 5.
- Update English and Simplified Chinese localization and the design inventory to match the new pool.

## Capabilities

### New Capabilities

- `rare-attack-pool`: Defines the revised Illusionist rare attack roster, card behaviors, transmutation-cascade safety rules, and localization requirements.

### Modified Capabilities

None.

## Impact

- Card model files and auto-registration entries under `sts2_illusionist/Scripts/Cards`.
- Transmutation runtime APIs in `TransmutePower` for safely inspecting and fully reverting one chain.
- English and Simplified Chinese card localization.
- Existing decks or saves containing removed uncommitted card IDs are not guaranteed to preserve those two cards.
- Build and installation verification must use `sts2_illusionist/build-illusionist-windows.ps1`.
- The obsolete Encore power model and localization are removed.
- Cameo and Grand Masquerade share the same deterministic other-character selection and unlocked-card-pool rules.
- Reversal, Bluff, Catalyze, and Cut In notify the shared intent-change entry point only after their structural intent modification succeeds.
- Mirror result visuals continue to use the generic post-play Exhaust check and do not special-case Undying Flame.
