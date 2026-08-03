## ADDED Requirements

### Requirement: Removed rare attacks are absent
The mod SHALL no longer register or localize Mirror Offering (镜前献技) or Break Character (破相).

#### Scenario: Card pool registration
- **WHEN** the Illusionist card pool is constructed
- **THEN** neither removed card model is present

### Requirement: Body Double is a Skill
Body Double SHALL retain its stable model identity and existing enemy HP-loss and damage-redirection behavior, SHALL use the Simplified Chinese title 替身, and SHALL be typed as a Rare Skill.

#### Scenario: Body Double is played
- **WHEN** the player plays Body Double on a living enemy
- **THEN** the enemy loses the configured HP and redirects qualifying unblocked powered attack damage from the player to itself for that enemy turn

### Requirement: Layered Murder fully unwinds one transmuted hand card
Layered Murder SHALL be a 2-cost Rare Attack that deals 6 damage, allows selection of one transmuted hand card, and reverts that card one layer at a time until it has no predecessor. Its upgrade SHALL reduce its cost to 1.

#### Scenario: Multi-layer chain
- **WHEN** the player selects a hand card with multiple registered transmutation predecessors
- **THEN** every predecessor is restored in order until the original form is current

#### Scenario: No transmuted card
- **WHEN** Layered Murder is played with no transmuted card in hand
- **THEN** it deals its base damage and completes without presenting an invalid selection

### Requirement: Layered Murder executes Attack history safely
For each layer revealed as a playable Attack, Layered Murder SHALL snapshot an Exhaust copy, finish the selected card's full unwind, and then auto-play the snapshots in reveal order at seeded random hittable enemies. It SHALL NOT auto-play non-Attack forms, SHALL NOT store generated execution copies in mirrors, and a cascade-generated Layered Murder copy SHALL NOT start a nested cascade.

#### Scenario: Mixed-form chain
- **WHEN** a selected chain reveals Attack and non-Attack predecessors
- **THEN** only the Attack forms are executed and every layer still reverts

#### Scenario: Mirror slots are empty
- **WHEN** a generated execution copy Exhausts while the player has an empty mirror
- **THEN** that generated copy is not stored in the mirror

#### Scenario: Combat ends during cascade
- **WHEN** an executed Attack ends combat
- **THEN** no later Attack copy is auto-played and the operation exits safely

### Requirement: Superimpose scales with prior same-name plays
Superimpose (叠影) SHALL be a 1-cost Rare Attack that deals 7 damage plus 7 for each prior finished play of the same card model by its owner during the current turn. Its upgrade SHALL change both values to 9.

#### Scenario: Repeated plays
- **WHEN** three Superimpose cards are played sequentially in one turn
- **THEN** they deal 7, 14, and 21 base damage before ordinary damage modifiers

#### Scenario: New turn
- **WHEN** Superimpose is first played in a later turn
- **THEN** prior-turn plays do not increase its damage

### Requirement: Sugar-Coated Bullet trades future Block for damage
Sugar-Coated Bullet SHALL be a 1-cost Rare Attack that deals 18 damage and then grants a surviving target 12 unpowered Block. Its upgrade SHALL increase damage to 24 without changing granted Block.

#### Scenario: Target survives
- **WHEN** Sugar-Coated Bullet damages but does not kill its target
- **THEN** the target gains 12 Block after damage resolves

#### Scenario: Target dies
- **WHEN** Sugar-Coated Bullet kills its target
- **THEN** no Block is granted to the dead target

### Requirement: New and changed cards are localized
English and Simplified Chinese localization SHALL contain titles, descriptions, and selection prompts needed by Layered Murder, Superimpose, Sugar-Coated Bullet, and Body Double, and SHALL contain no stale entries for removed cards.

#### Scenario: Card library display
- **WHEN** the card library is opened in English or Simplified Chinese
- **THEN** every retained or newly added card displays localized text matching its implemented behavior

### Requirement: Dim Lamp has an Energy upgrade
Dim Lamp SHALL gain 1 Energy and draw 2 cards when unupgraded, and SHALL gain 2 Energy and draw 2 cards when upgraded. Its localized Energy value SHALL reflect the upgrade.

#### Scenario: Unupgraded Dim Lamp is played
- **WHEN** the player plays an unupgraded Dim Lamp
- **THEN** the player gains 1 Energy and draws 2 cards

#### Scenario: Upgraded Dim Lamp is played
- **WHEN** the player plays an upgraded Dim Lamp
- **THEN** the player gains 2 Energy and draws 2 cards

### Requirement: Rarities and Forewarn cost match the maintained pool
False Refuge and Catalyze SHALL be Rare cards. Solidify Time and Forewarn SHALL be Uncommon cards. Forewarn SHALL cost 3 Energy both before and after upgrade.

#### Scenario: Card library metadata
- **WHEN** the four maintained cards are inspected in the card library
- **THEN** their rarities and Forewarn's cost match the maintained pool

### Requirement: Encore creates a discard-sourced transmuted Lamp
Encore SHALL be a 1-cost Uncommon Skill that selects one card from the owner's discard pile, leaves the selected card there, and adds an Extinguished Lamp transmuted into a clone of the selected card to the owner's hand. Its upgrade SHALL reduce its cost to 0.

#### Scenario: A discard card is selected
- **WHEN** Encore resolves and the player selects a card in the discard pile
- **THEN** the selected card remains in the discard pile and a transmuted copy with Extinguished Lamp as its predecessor is added to hand

#### Scenario: The discard pile is empty
- **WHEN** Encore resolves with no cards in the discard pile
- **THEN** it completes without opening an invalid selection

### Requirement: Mirror firing respects post-play Exhaust removal
For a playable Attack, Skill, or Status that enters a mirror shot with Exhaust, the mirror SHALL spend it only if it still has Exhaust after its play resolves. A card that removes Exhaust during that play SHALL remain stored without Exhaust. This behavior SHALL be generic and SHALL NOT identify a specific card model.

#### Scenario: Stored card removes Exhaust
- **WHEN** a mirror fires an Exhaust card whose play effect removes Exhaust
- **THEN** the card remains loaded in the same mirror without Exhaust

#### Scenario: Stored card retains Exhaust
- **WHEN** a mirror fires an Exhaust card that still has Exhaust after resolving
- **THEN** the card is spent to the exhaust pile and the mirror becomes empty

### Requirement: Mirror result visuals match the post-play destination
A playable card fired by a mirror SHALL show one result animation after resolving. A card that still has Exhaust SHALL fly directly to the exhaust pile without displaying the native burn-away Exhaust effect, while a card that does not have Exhaust SHALL fly directly to the mirror pile.

#### Scenario: Exhaust remains after play
- **WHEN** a mirror-fired card still has Exhaust after its effect resolves
- **THEN** it flies directly to the exhaust pile, fires ordinary Exhaust hooks, and does not first fly to the mirror or display a second Exhaust effect

#### Scenario: Exhaust is removed during play
- **WHEN** a mirror-fired card removes Exhaust while resolving
- **THEN** it flies directly back to the mirror pile and remains loaded

### Requirement: Catalyze advances repeated moves
Catalyze SHALL double damage for the current turn and SHALL advance a living target only when its current intent list is non-empty and contains exclusively Attack and/or Defend intents. A next turn that resolves to the same MoveState instance SHALL still count as an advancement and update state-machine history.

#### Scenario: One-time self-buff intent
- **WHEN** the target's intent includes a Buff or other non-Attack/non-Defend intent
- **THEN** Catalyze does not advance the target

#### Scenario: Next move is the same object
- **WHEN** an eligible target's next state resolves to the current MoveState instance
- **THEN** Catalyze forces the state transition and records the advanced state

### Requirement: Cameo transmutes one hand card from another character
Cameo SHALL be a 0-cost Uncommon Skill that chooses another playable character and transmutes one eligible hand card into a random unlocked card from that character. The unupgraded card SHALL choose the hand card randomly; the upgraded card SHALL let the player choose it. Both random choices SHALL use the owner's seeded combat-card-selection RNG.

#### Scenario: Unupgraded Cameo
- **WHEN** unupgraded Cameo resolves with at least one transformable hand card
- **THEN** the player chooses another character and one random eligible hand card is transmuted into a random unlocked card from that character

#### Scenario: Upgraded Cameo
- **WHEN** upgraded Cameo resolves with at least one transformable hand card
- **THEN** the player chooses another character, chooses one eligible hand card, and that card is transmuted into a random unlocked card from the character

#### Scenario: No eligible hand card
- **WHEN** Cameo resolves without a transformable card in hand
- **THEN** it completes without opening either selection interface

### Requirement: Mesmerizing Array rewards the first structural intent change
Mesmerizing Array SHALL be a 1-cost Rare Power whose upgrade grants Innate. The first time each owner turn that the player directly and successfully changes an enemy's intent structure, it SHALL Copy and draw a number of cards equal to its stack count.

#### Scenario: First successful change
- **WHEN** the player successfully changes an enemy intent with Reversal, Bluff, Catalyze, or the multi-attack branch of Cut In
- **THEN** one stack of Mesmerizing Array Copies 1 and draws 1 card

#### Scenario: Additional change in the same turn
- **WHEN** Mesmerizing Array has already triggered this player turn and another enemy intent is changed
- **THEN** it grants no additional Copy or draw

#### Scenario: Multiple power stacks
- **WHEN** two stacks of Mesmerizing Array receive their first qualifying notification of the turn
- **THEN** the power Copies 2 and draws 2 cards in one trigger

#### Scenario: Numeric intent refresh
- **WHEN** Weak, Strength, or another numeric modifier changes only the values displayed by an enemy intent
- **THEN** Mesmerizing Array does not trigger

#### Scenario: Natural enemy transition
- **WHEN** an enemy advances to its next move without a player-driven structural modification
- **THEN** Mesmerizing Array does not trigger

### Requirement: Maintained card values match the balance pass
Trick Barrage SHALL deal 4 damage per card in hand and SHALL upgrade to 6 damage per card. Accrue SHALL deal 11 base damage and gain 1 additional damage for every 5 Block gained by any creature this combat. Shield Tax SHALL retain its cumulative 10-Block threshold while stating that threshold directly in English and Simplified Chinese. Undying Flame SHALL deal 21 damage and SHALL upgrade to 26 damage.

#### Scenario: Trick Barrage is upgraded
- **WHEN** an upgraded Trick Barrage resolves with cards in hand
- **THEN** each hit has a base damage of 6

#### Scenario: Accrue reads accumulated Block
- **WHEN** creatures have gained 14 Block this combat
- **THEN** Accrue's displayed base damage is 13 before ordinary damage modifiers

#### Scenario: Undying Flame is upgraded
- **WHEN** an upgraded Undying Flame resolves
- **THEN** its base damage is 26 before ordinary damage modifiers

### Requirement: Cut In reduces the executed hit count
When Cut In takes its multi-attack branch, the target SHALL receive a one-hit reduction that is consumed by its actual attack command. The target's current visible intent SHALL also show one fewer Attack repeat while preserving the original move effect and follow-up state. The implementation SHALL log the selected branch and any executed hit-count reduction.

#### Scenario: Multi-attack command executes
- **WHEN** a target carrying one unspent Cut In stack executes an attack command with 3 hits
- **THEN** the command executes 2 hits and the reduction is recorded in the log

#### Scenario: Intent preview is reduced
- **WHEN** Cut In applies its execution-time reduction to a multi-attack intent
- **THEN** the visible intent label shows one fewer attack and the original move sequence remains unchanged

#### Scenario: Attack command reaches the native hit-count boundary
- **WHEN** the attacker's command reaches `Hook.ModifyAttackHitCount`
- **THEN** the native hit-count patch is the sole Cut In execution path and consumes the reduction at most once for that command

#### Scenario: A power-amount modifier increases Cut In
- **WHEN** a successful Cut In application is increased by a generic power-amount modifier
- **THEN** that card still adds exactly one hit reduction, matching its one-repeat visible intent change

#### Scenario: A second Cut In targets an intent reduced to one attack
- **WHEN** a prior Cut In has reduced the target's visible intent from two attacks to one
- **THEN** the next Cut In applies Weak instead of adding another hit reduction, and the move retains one attack

### Requirement: Misdirection rewards every card change this turn
Misdirection (障眼法) SHALL be a 0-cost Uncommon Skill that gains 4 Block and applies a turn-limited effect that gains 4 Block whenever one of the owner's cards changes. Its upgrade SHALL increase both values to 5. Forward transformations and every individual transmutation-revert layer SHALL each count as one change.

#### Scenario: Forward transformation
- **WHEN** the owner transforms one card after playing Misdirection
- **THEN** the owner gains the configured per-change Block once

#### Scenario: Multi-layer revert
- **WHEN** one card reverts three transmutation layers while Misdirection is active
- **THEN** the owner gains the configured per-change Block three times

#### Scenario: Turn ends
- **WHEN** the owner's side turn ends
- **THEN** Misdirection's per-change effect is removed

### Requirement: Underhand Strike is efficient expendable damage
Underhand Strike (暗手) SHALL retain the `SLEEVE_BLADE` stable model identity and SHALL be a 1-cost Common Attack with Exhaust that deals 11 damage. Its upgrade SHALL increase its damage to 13.

#### Scenario: Underhand Strike is played
- **WHEN** Underhand Strike resolves against an enemy
- **THEN** it deals its configured damage and Exhausts through the ordinary card result pipeline

### Requirement: Transposition converts a draw-pile card into a fleeting hand copy
Transposition (移花接木) SHALL be a 1-cost Uncommon Skill with Exhaust. It SHALL select one card from the owner's draw pile, clone that card's current combat state, Exhaust the selected original through the ordinary Exhaust pipeline, add Ethereal and Exhaust to the clone, and add the generated clone to the owner's hand. Its upgrade SHALL remove Exhaust from Transposition itself without removing either keyword from the generated clone.

#### Scenario: A draw-pile card is selected
- **WHEN** Transposition resolves and the player selects a card in the draw pile
- **THEN** the original card Exhausts and an Ethereal, Exhaust clone with the same upgrade and combat state is added to hand

#### Scenario: The draw pile is empty
- **WHEN** Transposition resolves with no cards in the draw pile
- **THEN** it completes without opening an invalid selection or generating a card

#### Scenario: Upgraded Transposition is played
- **WHEN** upgraded Transposition resolves
- **THEN** Transposition itself does not Exhaust, while its generated clone still has both Ethereal and Exhaust

### Requirement: One Step Ahead turns enemy Block into recurring draw
One Step Ahead (魔高一丈) SHALL retain the `UNINVITED` stable identity and SHALL be a 0-cost Uncommon Skill with Exhaust that draws 1 card. Whenever an enemy of its owner gains a positive amount of Block, this exact card SHALL move from its current combat pile to its owner's hand without counting as a draw. It SHALL be active without first being played, SHALL do nothing when already in hand, and SHALL upgrade to draw 2 cards.

#### Scenario: Enemy gains Block while One Step Ahead is outside the hand
- **WHEN** an enemy gains positive Block while One Step Ahead is in the draw, discard, exhaust, or mirror pile
- **THEN** that One Step Ahead instance moves to its owner's hand

#### Scenario: Multiple enemies gain Block sequentially
- **WHEN** one enemy's Block gain returns One Step Ahead and another enemy then gains Block
- **THEN** One Step Ahead remains in hand without being moved or duplicated again

#### Scenario: Ally gains Block
- **WHEN** the owner or one of the owner's allies gains Block
- **THEN** One Step Ahead does not move

#### Scenario: One Step Ahead is upgraded
- **WHEN** One Step Ahead is upgraded
- **THEN** it draws 2 cards while its 0 cost, Exhaust, and return behavior remain unchanged

### Requirement: Shapeshift upgrades by removing Exhaust
Shapeshift (幻形) SHALL retain the `TRANSMUTE` stable identity and SHALL be a 1-cost Uncommon Skill with Exhaust that selects from its configured random exhaust-pile candidates and transmutes one hand card into a clone of the selected card. Its upgrade SHALL remove Exhaust from Shapeshift itself without increasing the candidate count.

#### Scenario: Upgraded Shapeshift resolves
- **WHEN** upgraded Shapeshift transmutes a hand card
- **THEN** its candidate count matches the unupgraded card and Shapeshift does not Exhaust

### Requirement: Foresight top-decks from draw and discard
Foresight SHALL be a 1-cost Uncommon Skill that selects 3 actual card instances from the owner's combined draw and discard piles, moves the selected cards to the top of the draw pile, and applies 1 Energy for the start of the owner's next turn. Its upgrade SHALL increase the selection count from 3 to 5.

#### Scenario: Cards are selected from both piles
- **WHEN** Foresight selects cards that currently occupy both the draw and discard piles
- **THEN** every selection is moved to the top of the draw pile and no selected card remains in the discard pile

#### Scenario: Fewer cards are available
- **WHEN** fewer than the configured selection count exist across the draw and discard piles
- **THEN** Foresight offers all available cards and still grants 1 Energy next turn

### Requirement: Requested Simplified Chinese descriptions are preserved
The Simplified Chinese descriptions for Cameo, Transposition, and Foresight SHALL reproduce the user-provided wording without additional keyword markup, line breaks, or explanatory clauses. Misdirection SHALL omit the transmutation-revert explanation while retaining its implementation behavior.

#### Scenario: Upgraded Cameo is viewed
- **WHEN** upgraded Cameo is viewed in Simplified Chinese
- **THEN** its description reads `选择一名其他角色，选择一张手牌幻化为该角色的随机牌`

#### Scenario: Transposition is viewed
- **WHEN** Transposition is viewed in Simplified Chinese
- **THEN** its description reads `消耗抽牌堆的一张牌，将它的虚无消耗复制品加入你的手牌`

### Requirement: Freeze Frame freezes one hand card
Freeze Frame SHALL retain the `SUPERIMPOSE` stable model identity and SHALL be a 1-cost Uncommon Skill that selects one eligible hand card and applies Frozen without an additional card-preview animation. Its upgrade SHALL reduce its cost to 0.

#### Scenario: A hand card is selected
- **WHEN** Freeze Frame resolves and the player selects an eligible hand card
- **THEN** that card gains Frozen and no separate card-preview animation is shown

### Requirement: Solidify Time freezes all combat-pile cards
Solidify Time SHALL be a 2-cost Rare Skill with Exhaust that applies Frozen without an additional card-preview animation to every eligible card in its owner's hand, draw, discard, exhaust, and mirror piles. Its upgrade SHALL reduce its cost to 1.

#### Scenario: Cards occupy every supported pile
- **WHEN** Solidify Time resolves while eligible cards occupy the hand, draw, discard, exhaust, and mirror piles
- **THEN** every such card gains Frozen without a separate card-preview animation

### Requirement: Mirror Realm Expansion is a mirror-cap Power
Mirror Realm Expansion SHALL be a 1-cost Rare Power that raises the owner's mirror cap by 1 before Copying 1. Its upgrade SHALL grant Innate.

#### Scenario: The owner is at the previous mirror cap
- **WHEN** Mirror Realm Expansion resolves
- **THEN** the mirror-cap increase is applied before Copy 1 is resolved

### Requirement: Mirror and Layered Murder text receive maintenance
The Mirror keyword description SHALL end by stating that the player can have at most 9 mirrors. The Simplified Chinese Layered Murder description SHALL contain a line break immediately after `造成12点伤害`.

#### Scenario: Simplified Chinese hover and card text
- **WHEN** the player views the Mirror keyword or Layered Murder in Simplified Chinese
- **THEN** the nine-mirror limit and requested Layered Murder line break are visible

### Requirement: Upgrade previews highlight only changed content
Provoke SHALL use its Energy dynamic value so its 1-to-2 Energy upgrade highlights only that number. Cameo SHALL share its unchanged description text between upgrade states and SHALL conditionally replace only the hand-card selection clause.

#### Scenario: Upgrade previews are viewed
- **WHEN** Provoke or Cameo is viewed in upgrade-preview mode
- **THEN** unchanged description text is not replaced or highlighted as upgraded content

### Requirement: Silver Lining and Kindle use revised Block values
Silver Lining SHALL gain 7 base Block and retain its existing Retain upgrade. Kindle SHALL gain 5 base Block and retain its existing `+3` Block upgrade, gaining 8 Block when upgraded.

#### Scenario: Revised cards are upgraded
- **WHEN** upgraded Silver Lining and Kindle are played
- **THEN** Silver Lining gains 7 base Block plus transferred enemy Block and Kindle gains 8 base Block
