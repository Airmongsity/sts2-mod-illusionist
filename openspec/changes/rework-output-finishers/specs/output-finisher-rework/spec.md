## ADDED Requirements

### Requirement: Metamorphosis converts current-turn transformations into damage
Metamorphosis SHALL remain an Uncommon Attack with stable ID `METAMORPHOSIS`, cost 1 Energy, and deal 8 damage plus 5 damage for each card transformed this turn. Its upgrade SHALL raise the base damage to 11 and the per-transformation bonus to 6 without changing its cost.

#### Scenario: Base Metamorphosis scales with transformations
- **WHEN** an unupgraded Metamorphosis is played after three cards have transformed this turn
- **THEN** it deals 23 damage before ordinary damage modifiers

#### Scenario: Upgraded Metamorphosis scales with transformations
- **WHEN** an upgraded Metamorphosis is played after three cards have transformed this turn
- **THEN** it deals 29 damage before ordinary damage modifiers

### Requirement: Curtain Break provides bounded Block-break economy
Curtain Break SHALL remain a Rare Power with stable ID `CURTAIN_BREAK`, cost 1 Energy, and draw 1 card when played. Each applied power instance SHALL, on the first qualifying enemy Block break during each owner turn, draw 1 card and grant 1 Energy, and SHALL not trigger again during that turn. Its upgrade SHALL grant Innate without changing its cost.

#### Scenario: Playing Curtain Break establishes immediate value
- **WHEN** Curtain Break is played
- **THEN** the owner draws 1 card and gains one Curtain Break power instance

#### Scenario: First qualifying Block break triggers
- **WHEN** the owner or the owner's pet first breaks an enemy's Block during the owner's turn
- **THEN** each unused Curtain Break instance draws 1 card and grants 1 Energy

#### Scenario: Further Block breaks are bounded
- **WHEN** the same Curtain Break instance has already triggered during the current owner turn
- **THEN** another enemy Block break grants no reward from that instance

#### Scenario: Curtain Break refreshes next turn
- **WHEN** a new owner turn begins after a Curtain Break instance triggered on the previous turn
- **THEN** that instance can trigger once again

#### Scenario: Curtain Break upgrade is Innate
- **WHEN** Curtain Break is upgraded
- **THEN** it gains Innate and continues to cost 1 Energy

### Requirement: Reckoning converts combat-long intent-card plays into damage
Reckoning SHALL remain an Uncommon Attack with stable ID `RECKONING`, cost 1 Energy, and Exhaust. It SHALL deal 12 damage plus 8 damage for each qualifying intent card played by its owner this combat. Its upgrade SHALL raise the base damage to 16 and the per-card bonus to 10 without changing its cost or Exhaust.

#### Scenario: Base Reckoning scales with intent cards
- **WHEN** an unupgraded Reckoning is played after three qualifying intent cards this combat
- **THEN** it deals 36 damage before ordinary damage modifiers and Exhausts after play

#### Scenario: Upgraded Reckoning scales with intent cards
- **WHEN** an upgraded Reckoning is played after three qualifying intent cards this combat
- **THEN** it deals 46 damage before ordinary damage modifiers and Exhausts after play

### Requirement: Call the Bluff converts current target Block into damage
Call the Bluff SHALL remain an Uncommon Attack with stable ID `CALL_THE_BLUFF` and cost 1 Energy. It SHALL snapshot and remove all current Block from its selected enemy, then deal 9 damage plus the removed amount. Its upgrade SHALL raise its base damage to 13 without changing its cost.

#### Scenario: Call the Bluff removes Block before attacking
- **WHEN** an unupgraded Call the Bluff targets an enemy with 20 Block
- **THEN** it removes all 20 Block and then deals a 29-damage attack before ordinary damage modifiers

#### Scenario: Upgraded Call the Bluff raises base damage
- **WHEN** an upgraded Call the Bluff targets an enemy with 20 Block
- **THEN** it removes all 20 Block and then deals a 33-damage attack before ordinary damage modifiers

### Requirement: Accrue converts all enemy Block history into damage
Accrue SHALL remain an Uncommon Attack with stable ID `ACCRUE`, cost 2 Energy, and Exhaust. It SHALL deal 18 damage plus 1 damage for each complete 2 points of Block gained by all enemies during the current combat. Block gained by the player or allied creatures SHALL not affect its damage. Its upgrade SHALL raise its base damage to 24 without changing its cost, ratio, or Exhaust.

#### Scenario: Accrue pools all enemy Block history
- **WHEN** all enemies have collectively gained 21 Block this combat and the player or allies have gained additional Block
- **THEN** an unupgraded Accrue deals 28 damage before ordinary damage modifiers and ignores the player and allied Block history

#### Scenario: Accrue upgrade raises base damage
- **WHEN** all enemies have collectively gained 21 Block this combat and upgraded Accrue is played
- **THEN** it deals 34 damage before ordinary damage modifiers

#### Scenario: Block history survives Block removal
- **WHEN** an enemy previously gained Block that was subsequently lost or removed, or that enemy died
- **THEN** that gained Block still contributes to Accrue for the remainder of the combat

### Requirement: Finisher localization matches behavior
English and Simplified Chinese card and power localization SHALL describe the revised costs, damage resources, trigger limits, and upgrade-relevant dynamic values consistently.

#### Scenario: Localized card descriptions are loaded
- **WHEN** any of the five finishers is viewed in English or Simplified Chinese
- **THEN** its description communicates the same mechanical behavior implemented by its card and power models
