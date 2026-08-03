## 1. Existing Card Pool

- [x] 1.1 Remove Mirror Offering and Break Character card models
- [x] 1.2 Change Body Double to a Rare Skill while preserving its stable ID and behavior

## 2. Transmutation Cascade

- [x] 2.1 Add a focused full-chain revert operation to TransmutePower
- [x] 2.2 Implement Layered Murder selection, cascade execution, target selection, and recursion/mirror guards

## 3. New Rare Attacks

- [x] 3.1 Implement Superimpose with current-turn same-name scaling
- [x] 3.2 Implement Sugar-Coated Bullet with post-damage enemy Block

## 4. Content and Verification

- [x] 4.1 Update English and Simplified Chinese card localization and remove stale keys
- [x] 4.2 Update the design inventory for the revised rare attack pool
- [x] 4.3 Validate the OpenSpec change
- [x] 4.4 Add Dim Lamp's +1 Energy upgrade and update localization
- [x] 4.5 Build and install with build-illusionist-windows.ps1 and review diagnostics

## 5. Card Pool Maintenance

- [x] 5.1 Adjust False Refuge, Catalyze, Solidify Time, and Forewarn rarities and Forewarn's cost
- [x] 5.2 Rework Encore as a discard-selection Skill and remove the obsolete Encore power
- [x] 5.3 Make mirror firing inspect post-play Exhaust without card-specific checks
- [x] 5.4 Make Catalyze advance and record same-MoveState next turns
- [x] 5.5 Update English/Chinese localization and the design inventory
- [x] 5.6 Validate the change, build/install with build-illusionist-windows.ps1, and review diagnostics

## 6. Cameo

- [x] 6.1 Share other-character pool selection with Grand Masquerade and implement Cameo
- [x] 6.2 Add English/Chinese localization and update the design inventory
- [x] 6.3 Validate the change, build/install with build-illusionist-windows.ps1, and review diagnostics

## 7. Mesmerizing Array

- [x] 7.1 Add a generic player intent-change notification and connect existing structural intent-changing cards
- [x] 7.2 Implement Mesmerizing Array as a stack-scaled, once-per-turn Rare Power
- [x] 7.3 Add English/Chinese localization and update the design inventory
- [x] 7.4 Validate the change, build/install with build-illusionist-windows.ps1, and review diagnostics

## 8. Mirror Result Visuals

- [x] 8.1 Route mirror-fired cards directly to the visual destination selected by their post-play Exhaust state
- [x] 8.2 Update the design inventory and specification for the result-animation rule
- [x] 8.3 Validate the change, build/install with build-illusionist-windows.ps1, and review diagnostics

## 9. Balance and Cut In Diagnostics

- [x] 9.1 Reduce Trick Barrage and Accrue values and update Accrue localization
- [x] 9.2 Simplify Shield Tax wording while preserving its cumulative threshold
- [x] 9.3 Inspect Cut In against the native attack pipeline and add targeted runtime diagnostics
- [x] 9.4 Update the design inventory and specification
- [x] 9.5 Validate the change, build/install with build-illusionist-windows.ps1, and review diagnostics

## 10. Cut In Repair

- [x] 10.1 Wrap the current MoveState so Cut In removes one visible Attack repeat without changing the move sequence
- [x] 10.2 Add a native hit-count fallback that consumes Cut In exactly once
- [x] 10.3 Update the design inventory and validate the revised requirement
- [x] 10.4 Build/install with build-illusionist-windows.ps1 and review diagnostics

## 11. Cut In Single-Consumption Repair

- [x] 11.1 Make the native hit-count patch the sole execution path and guard each AttackCommand against duplicate consumption
- [x] 11.2 Validate the change, build/install with build-illusionist-windows.ps1, and review diagnostics

## 12. Cut In Minimum-Attack Repair

- [x] 12.1 Normalize each successful Cut In application to exactly one reduction after generic power-amount modifiers
- [x] 12.2 Document the minimum-one-attack behavior and its regression scenarios
- [x] 12.3 Validate the change, build/install with build-illusionist-windows.ps1, and review diagnostics

## 13. Pool Completion — First Two Cards

- [x] 13.1 Implement Misdirection and its turn-limited transformation Block power
- [x] 13.2 Implement Sleeve Blade as an Exhaust Common Attack
- [x] 13.3 Add English/Chinese localization and update the design inventory
- [x] 13.4 Validate the change, build/install with build-illusionist-windows.ps1, and review diagnostics

## 14. Pool Completion — Draw-Pile Transposition

- [x] 14.1 Rename Sleeve Blade's displayed title while preserving its stable model identity
- [x] 14.2 Implement Transposition as a draw-pile Exhaust-and-copy Uncommon Skill
- [x] 14.3 Add English/Chinese localization and update the design inventory
- [x] 14.4 Validate the change, build/install with build-illusionist-windows.ps1, and review diagnostics

## 15. Pool Completion — Uninvited

- [x] 15.1 Implement Uninvited as a draw Skill that returns to hand when an enemy gains Block
- [x] 15.2 Add English/Chinese localization and update the design inventory
- [x] 15.3 Validate the final 91-card pool, build/install with build-illusionist-windows.ps1, and review diagnostics

## 16. One Step Ahead Balance Revision

- [x] 16.1 Rename Uninvited while preserving its stable identity, make it a 0-cost Exhaust Skill, and move its upgrade to draw
- [x] 16.2 Update English/Chinese localization, the design inventory, and the behavioral specification
- [x] 16.3 Validate the 91-card pool, build/install with build-illusionist-windows.ps1, and review diagnostics

## 17. Uncommon Skill Maintenance

- [x] 17.1 Rebalance Cameo and Shapeshift upgrades while preserving their stable identities
- [x] 17.2 Rework Foresight to top-deck 3 cards from draw and discard, upgrading to 5
- [x] 17.3 Apply the exact requested Simplified Chinese descriptions and update English localization and design records
- [x] 17.4 Validate the 91-card pool, build/install with build-illusionist-windows.ps1, and review diagnostics

## 18. Undying Flame Balance

- [x] 18.1 Increase Undying Flame's base and upgraded damage to 21 and 26
- [x] 18.2 Validate the change and run the full Windows build-and-install script

## 19. Freeze and Rare-Type Rework

- [x] 19.1 Rework Superimpose into Freeze Frame and Solidify Time into the all-pile Rare Freeze Skill
- [x] 19.2 Rework Mirror Realm Expansion into the mirror-cap Rare Power
- [x] 19.3 Update Mirror and Layered Murder text plus English/Chinese card localization and the design inventory
- [x] 19.4 Validate the change and run the full Windows build-and-install script

## 20. Minimal Upgrade-Preview and Block Maintenance

- [x] 20.1 Restrict Provoke and Cameo upgrade-preview differences to the content that changes
- [x] 20.2 Raise Silver Lining's Block to 7 and lower Kindle's base Block to 5
- [x] 20.3 Update the design inventory, validate the change, and run the full Windows build-and-install script
