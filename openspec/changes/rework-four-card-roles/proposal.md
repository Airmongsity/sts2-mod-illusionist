## Why

Solidify Time and Layered Murder currently have low practical payoff because global revert suppression conflicts with ordinary transmutation flow and Layered Murder requires rare Attack-only history chains. Provoke and Silver Lining also need reliable baseline value without letting their unconditional effects overshadow their intent and enemy-Block roles.

## What Changes

- Add 【冻结】 as a visible card effect similar to 【魂缚】 with the rule 【除非将其打出，否则这张牌无法变化】.
- Rework 【凝固时间】 to read 【选择一张手牌获得保留，这张牌获得冻结】, add Exhaust, and keep its existing 1-cost base and 0-cost upgrade.
- Rework 【层层杀机, 2c】 to read 【造成12点伤害选择一张手牌逐层回退，每回退一层额外造成7点伤害】. It no longer executes historical Attack-form copies and keeps its existing upgrade to 1 cost.
- Rework 【挑衅，升级后获得的能量1->2】 to read 【本回合获得2点敏捷，如果敌人的意图有攻击，其本回合获得3点力量，你获得1点能量】 while keeping its existing 0 cost and Uncommon rarity.
- Rework 【一线生机】 to read 【获得5点格挡，如果该敌人有格挡，将其转移给你】, change its base cost to 1, and retain its existing upgrade that grants Retain.
- Update English and Simplified Chinese localization for the four cards and the new card effect.

## Capabilities

### New Capabilities

- `four-card-role-rework`: Defines the Frozen card effect and the revised behavior, costs, upgrades, and exact Simplified Chinese wording for Solidify Time, Layered Murder, Provoke, and Silver Lining.

### Modified Capabilities

None.

## Impact

- Card models for Solidify Time, Layered Murder, Provoke, and Silver Lining.
- Transmutation and transformation guards that must respect Frozen.
- Card-effect registration, display, and localization using the same presentation mechanism as Soulbound.
- English and Simplified Chinese card/card-effect localization.
- Existing combat behavior for the four stable card IDs changes without changing their model identities.
