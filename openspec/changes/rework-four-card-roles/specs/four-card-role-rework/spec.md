## ADDED Requirements

### Requirement: Frozen prevents card changes until play

The system SHALL provide a visible Frozen affliction whose effect text is 【除非将其打出，否则这张牌无法变化】. A Frozen card SHALL be ineligible for transformation and SHALL not automatically or manually revert through its transmutation chain. Playing the card SHALL clear Frozen so later instances of that card can change normally.

#### Scenario: Frozen card reaches a turn-start reversion

- **WHEN** a Frozen card has at least one stored transmutation predecessor at the start of its owner's turn
- **THEN** the card does not revert and its transmutation chain remains intact

#### Scenario: Frozen card is played

- **WHEN** the owner plays a Frozen card
- **THEN** Frozen is cleared from that card

#### Scenario: Frozen card is rendered at different display resolutions

- **WHEN** a Frozen card is rendered at any supported display resolution or card scale
- **THEN** the Frozen overlay remains aligned to the card's 300 by 422 logical bounds

### Requirement: Solidify Time retains and freezes one card

Solidify Time SHALL cost 1, Exhaust, and read 【选择一张手牌获得保留，这张牌获得冻结】. It SHALL select one hand card able to receive Frozen, add Retain to it, and apply Frozen. Its upgrade SHALL reduce its cost to 0.

#### Scenario: Solidify Time affects an eligible hand card

- **WHEN** the player uses Solidify Time and chooses an eligible card in hand
- **THEN** the chosen card gains Retain and Frozen

### Requirement: Layered Murder converts reversion depth into damage

Layered Murder SHALL cost 2 and read 【造成12点伤害选择一张手牌逐层回退，每回退一层额外造成7点伤害】. It SHALL first deal 12 damage, then select a hand card, fully revert each available layer of that card, and deal an additional 7 damage for every successfully reverted layer. It SHALL not replay historical Attack forms. Its upgrade SHALL reduce its cost to 1.

#### Scenario: Selected card has three reversion layers

- **WHEN** Layered Murder selects a hand card with three available transmutation predecessors
- **THEN** the card fully reverts and the target receives three additional 7-damage hits after the initial 12 damage

#### Scenario: Selected card has no reversion layer

- **WHEN** Layered Murder selects a hand card without an available transmutation predecessor
- **THEN** no additional damage is dealt after the initial 12 damage

### Requirement: Provoke offers a temporary attack-intent trade

Provoke SHALL cost 0 and read 【本回合获得2点敏捷，如果敌人的意图有攻击，其本回合获得3点力量，你获得1点能量】. It SHALL grant 2 temporary Dexterity to the player. If the target enemy has an attack intent, it SHALL grant that enemy 3 temporary Strength and grant the player 1 Energy. Its upgrade SHALL increase the granted Energy from 1 to 2 without changing the stat amounts.

#### Scenario: Target intends to attack

- **WHEN** Provoke targets an enemy whose next move contains an attack intent
- **THEN** the player gains 2 temporary Dexterity and 1 Energy, and the enemy gains 3 temporary Strength

#### Scenario: Target does not intend to attack

- **WHEN** Provoke targets an enemy whose next move contains no attack intent
- **THEN** the player gains 2 temporary Dexterity and neither conditional effect occurs

### Requirement: Silver Lining has reliable Block and conditional transfer

Silver Lining SHALL cost 1 and read 【获得5点格挡，如果该敌人有格挡，将其转移给你】. It SHALL always grant 5 Block. If the target enemy has Block, it SHALL remove all of that Block and grant the same amount to the player without reapplying Dexterity to the transferred amount. Its upgrade SHALL add Retain.

#### Scenario: Target has Block

- **WHEN** Silver Lining targets an enemy with Block
- **THEN** the player gains 5 Block plus exactly the enemy's prior Block and the enemy's Block becomes zero

#### Scenario: Target has no Block

- **WHEN** Silver Lining targets an enemy without Block
- **THEN** the player still gains 5 Block

### Requirement: Localized text preserves the confirmed Chinese wording

The Simplified Chinese localization SHALL reproduce each user-confirmed bracketed expression verbatim for Frozen, Solidify Time, Layered Murder, Provoke, and Silver Lining. English localization SHALL describe the same mechanics and upgrade differences.

#### Scenario: Simplified Chinese descriptions are loaded

- **WHEN** the five affected descriptions are displayed in Simplified Chinese
- **THEN** their visible wording matches the confirmed expressions without paraphrasing
