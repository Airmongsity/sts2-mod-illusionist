## Why

Illusionist currently has several damage finishers, but all five of its major conversion attacks and powers cost 2 Energy. That shared hand friction makes players reluctant to draft or hold the cards that are supposed to turn the character's accumulated resources into decisive damage, so otherwise functional archetypes struggle to close combats with satisfying large numbers.

## What Changes

- Rework Metamorphosis into a 1-cost Uncommon Attack that deals 8 damage plus 5 for each card transformed this turn; upgrading raises those values to 11 and 6.
- Rework Curtain Break into a 1-cost Rare Power that draws 1 card when played and, once per turn, draws 1 card and grants 1 Energy the first time the player breaks an enemy's Block; upgrading grants Innate.
- Rework Reckoning into a 1-cost Uncommon Exhaust Attack that deals 12 damage plus 8 for each intent card played this combat; upgrading raises those values to 16 and 10.
- Rework Call the Bluff into a 1-cost Uncommon Attack that removes all of the target's current Block, then deals 9 damage plus the amount removed; upgrading raises its base damage to 13.
- Rework Accrue into a 2-cost Uncommon Exhaust Attack that deals 18 damage plus 1 for every 2 Block gained by all enemies this combat; upgrading raises its base damage to 24.
- Track the combat-long total Block gained by enemies without including players or allied creatures.
- Update English and Simplified Chinese localization to describe the revised behavior consistently.

## Capabilities

### New Capabilities

- `output-finisher-rework`: Defines the revised costs, scaling resources, upgrade paths, and all-enemy Block history required by the Illusionist's five damage finishers.

### Modified Capabilities

None.

## Impact

- Card models for Metamorphosis, Curtain Break, Reckoning, Call the Bluff, and Accrue.
- Curtain Break's associated power model and once-per-turn state.
- The hidden combat enemy Block-gained counter and its consumers.
- English and Simplified Chinese card localization.
- Stable card and power IDs remain unchanged.
- Build and installation verification must use `sts2_illusionist/build-illusionist-windows.ps1`.
