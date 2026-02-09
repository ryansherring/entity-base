# Testing Plan

This document outlines the testing strategy for `com.yourname.entity-base`. Testing covers three tiers: automated unit tests, automated integration tests, and manual verification.

---

## 1. Unit Tests

Unit tests validate individual classes in isolation. They run in Edit Mode (no scene required) and mock dependencies where needed.

### Test Assembly Setup

Create `Tests/Editor/EntityBase.Tests.Editor.asmdef`:
```json
{
    "name": "EntityBase.Tests.Editor",
    "references": ["EntityBase", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    "includePlatforms": ["Editor"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

### StatInstance Tests

| Test | Description | Expected |
|---|---|---|
| `SetValue_ClampsToMinMax` | Call `SetValue(200)` on a stat with max 100 | `currentValue == 100` |
| `SetValue_ClampsToMin` | Call `SetValue(-50)` on a stat with min 0 | `currentValue == 0` |
| `Add_IncreasesValue` | `Add(25)` from 50 | `currentValue == 75` |
| `Subtract_DecreasesValue` | `Subtract(30)` from 100 | `currentValue == 70` |
| `Subtract_ClampsAtMin` | `Subtract(150)` from 100, min=0 | `currentValue == 0` |
| `Tick_AppliesDecay` | Stat with `baseDecayRate = -5`, tick 1.0s | `currentValue -= 5` |
| `Tick_AppliesRegen` | Stat with `baseDecayRate = 10`, tick 1.0s | `currentValue += 10` |
| `Tick_RespectsRegenDelay` | Set `regenDelay = 2`, Subtract, then tick 1.0s | No regen yet |
| `Tick_RegensAfterDelay` | Set `regenDelay = 2`, Subtract, tick 3.0s | Regen applies after 2s delay |
| `Tick_StaticStatNoChange` | Stat with `baseDecayRate = 0`, tick any dt | `currentValue` unchanged |
| `DecayModifier_ScalesRate` | `decayModifier = 2.0`, `baseDecayRate = -5`, tick 1.0s | `currentValue -= 10` |
| `GetNormalized_ReturnsCorrect` | Value 75, range 0-100 | Returns `0.75` |
| `GetNormalized_EmptyRange` | `maxValue == minValue` | Returns `0` |
| `ResetToDefault_RestoresValue` | Modify value, call `ResetToDefault()` | Returns to `defaultValue` |

### Inventory Tests

| Test | Description | Expected |
|---|---|---|
| `AddItem_CreatesSlot` | Add 1 item to empty inventory | `slots.Count == 1`, `slots[0].count == 1` |
| `AddItem_StacksExisting` | Add 5 of item (maxStack=10), then add 3 more | `slots.Count == 1`, `slots[0].count == 8` |
| `AddItem_SplitsOverflow` | Add 15 of item (maxStack=10) | `slots.Count == 2`, counts 10+5 |
| `AddItem_ReturnsOverflow` | Add 25 to inventory with 2 slots, maxStack=10 | Returns 5 (overflow) |
| `AddItem_NullItem` | Pass null item | Returns amount unchanged |
| `RemoveItem_DecreasesCount` | Have 5, remove 3 | `slots[0].count == 2` |
| `RemoveItem_RemovesEmptySlot` | Have 3, remove 3 | `slots.Count == 0` |
| `RemoveItem_InsufficientFails` | Have 2, remove 5 | Returns false, inventory unchanged |
| `RemoveItem_SpansMultipleSlots` | Two slots of 5, remove 8 | First slot gone, second has 2 |
| `HasItem_TrueWhenSufficient` | Have 10, check HasItem(5) | Returns true |
| `HasItem_FalseWhenInsufficient` | Have 2, check HasItem(5) | Returns false |
| `HasItem_NullItemFalse` | Pass null | Returns false |
| `GetItemCount_SumsAllSlots` | Three slots with 5, 3, 7 | Returns 15 |
| `ConsumeItem_NonConsumableFails` | Item with `consumable = false` | Returns false |
| `ConsumeItem_RemovesAndAppliesStat` | Consumable heals 25 HP | HP increases by 25, item count decreases |
| `MaxSlots_PreventsOverflow` | `maxSlots = 3`, add 4 non-stackable items | `slots.Count == 3`, overflow = 1 |

### AbilitySystem Tests

| Test | Description | Expected |
|---|---|---|
| `TryUse_Succeeds` | Ability ready, sufficient resources | Returns true, cooldown starts |
| `TryUse_OnCooldown` | Ability with cooldown > 0 | Returns false |
| `TryUse_InsufficientMana` | Mana cost 50, current mana 30 | Returns false, mana unchanged |
| `TryUse_InsufficientEnergy` | Energy cost 50, current energy 30 | Returns false, energy unchanged |
| `TryUse_DeductsCosts` | Mana cost 20, energy cost 10 | Both deducted after success |
| `TryUse_InvalidIndex` | Pass index -1 or >= count | Returns false |
| `TryUse_NullDefinition` | Slot with null definition | Returns false |
| `CooldownTick_Decreases` | Set cooldown to 5, tick 2s | `cooldownRemaining == 3` |
| `CooldownTick_StopsAtZero` | Cooldown 1, tick 5s | `cooldownRemaining == 0` |
| `CastTime_DelaysExecution` | Ability with castTime 2.0 | Effect not spawned until 2s later |
| `TryUseByDefinition_FindsSlot` | Pass AbilityDefinition reference | Resolves to correct slot |

---

## 2. Integration Tests

Integration tests validate component interactions in a real Unity scene. They run in Play Mode.

### Test Assembly Setup

Create `Tests/Runtime/EntityBase.Tests.Runtime.asmdef`:
```json
{
    "name": "EntityBase.Tests.Runtime",
    "references": ["EntityBase", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

### Player Movement Integration

| Test | Description |
|---|---|
| `Player_MovesWithInput` | Spawn player, set `MoveInput = (0, 1)` for 1s, verify position changed on Z axis |
| `Player_Jumps` | Spawn player on ground, set `JumpInput = true`, verify Y position increases |
| `Player_Sprints` | Compare velocity at walkSpeed vs sprintSpeed |
| `Player_GroundCheck` | Place player on ground plane, verify `isGrounded == true` |
| `Player_Falls` | Place player above ground, verify gravity pulls down |
| `Player_TerminalVelocity` | Let player fall for 10s, verify velocity doesn't exceed terminal |

### NPC Pathfinding Integration

| Test | Description |
|---|---|
| `NPC_MovesToDestination` | Call `SetDestination()`, wait, verify position moved toward target |
| `NPC_ReachesDestination` | Call `SetDestination()` to nearby point, verify `HasReachedDestination == true` |
| `NPC_StopClearsMovement` | Call `SetDestination()` then `Stop()`, verify `MoveInput == Vector2.zero` |
| `NPC_DestinationEventFires` | Subscribe to `OnDestinationReached`, set destination, verify callback |

### Stat Decay Integration

| Test | Description |
|---|---|
| `Stats_TickInPlayMode` | Create entity with decaying stat, run for 2s, verify value decreased |
| `NPC_CrossStatModifiers` | Set energy to 20%, verify hunger decayModifier changed |
| `MagicUnlocked_ShowsMana` | Toggle `magicUnlocked`, verify mana ticks |

### Inventory + Attributes Integration

| Test | Description |
|---|---|
| `ConsumeItem_RestoresHP` | Add health potion, consume it, verify HP increased |
| `ConsumeItem_FiresEvent` | Subscribe to `OnItemConsumed`, consume item, verify callback |

### Ability Integration

| Test | Description |
|---|---|
| `Ability_DeductsManaAndEnergy` | Use ability with costs, verify stats decreased |
| `Ability_SpawnsEffect` | Use ability with effectPrefab, verify instantiated |
| `Ability_EventChain` | Verify `OnAbilityStarted` -> `OnAbilityExecuted` order |

---

## 3. Manual Test Checklist

These tests require a human to verify visual/interactive behavior.

### Setup

- [ ] Package appears in Unity Package Manager window
- [ ] No compile errors after fresh import (`check_compile_errors`)
- [ ] `Tools > Entity Base` menu exists with all items

### Entity Creation

- [ ] `Create Player` produces entity with all expected components
- [ ] `Create NPC` produces entity with A* pathfinding components
- [ ] Player entity has `HeadTarget` child at correct position
- [ ] NPC entity does NOT have camera components
- [ ] Odin Inspector shows grouped fields, foldouts, and toggle buttons

### Movement

- [ ] Player moves with WASD in Play Mode
- [ ] Camera orbits with mouse in third-person
- [ ] Press V to switch to first-person
- [ ] First-person: mouse look works, WASD strafes
- [ ] Press V to switch back to third-person (smooth blend)
- [ ] Sprint with Shift increases speed visibly
- [ ] Jump with Space, character goes airborne then lands
- [ ] ControlsHUD shows in bottom-left corner with correct mode

### Camera Fix

- [ ] `Fix Camera Settings` repairs broken orbital camera
- [ ] After fix, scroll wheel zooms in/out
- [ ] Vertical orbit range is -20 to 60 degrees
- [ ] Camera tracks player with appropriate damping

### Stats

- [ ] Inspector shows HP and Energy on all entities
- [ ] Toggle `magicUnlocked` -> Mana appears/disappears in inspector
- [ ] On NPC: Rest, Hunger, Thirst, Happiness visible in "NPC Needs" foldout
- [ ] On Player: NPC Needs section is hidden
- [ ] In Play Mode: decaying stats decrease over time
- [ ] `Initialize Defaults` button works with stat asset pickers
- [ ] Progress bars show correct colors from StatDefinition

### Inventory

- [ ] Add items via inspector table
- [ ] Items stack correctly (adding same item increases count)
- [ ] Item icon previews show in table
- [ ] Count slider respects maxStackSize

### Abilities

- [ ] Add ability definitions to slots via inspector table
- [ ] In Play Mode: `TryUseAbility(0)` starts cooldown (visible in progress bar)
- [ ] Cooldown ticks down visually
- [ ] After cooldown, ability can be used again
- [ ] If mana/energy insufficient, ability fails

### Validation

- [ ] `Validate All Entities` reports issues for misconfigured entities
- [ ] Right-click EntityIdentity > `Validate Entity` works
- [ ] No false positives on correctly configured entities

### Gizmos

- [ ] Select player: green sphere at feet when grounded
- [ ] Jump: sphere turns red while airborne
- [ ] Select NPC with destination: orange line to target
- [ ] Entity labels visible above all entities in SceneView

### Project Settings

- [ ] `Edit > Project Settings > Entity Base` shows settings panel
- [ ] Settings persist between editor sessions

---

## 4. Performance Considerations

### What to Profile

- `EntityAttributes.Update()` -- ticks up to 7 stats per entity per frame. With 100+ NPCs, consider batching or reducing tick frequency.
- `Inventory.AddItem()` -- linear scan for stacking. Fine for <100 slots; consider a dictionary lookup for large inventories.
- `AbilitySystem.Update()` -- iterates ability list every frame for cooldown/cast ticking. Minimal cost unless entities have 50+ abilities.

### Recommended Profiling Tests

| Scenario | Target | Method |
|---|---|---|
| 100 NPCs with 7 stats each | < 0.5ms total | Unity Profiler, Deep Profile on `EntityAttributes.Update` |
| 50 NPCs pathfinding simultaneously | < 1.0ms total | Profile `NPCMovementInput.Update` |
| Inventory with 100 slots, frequent adds | < 0.1ms per operation | Stopwatch in test |

---

## 5. Regression Testing Strategy

Before each release:

1. Run all Edit Mode unit tests
2. Run all Play Mode integration tests
3. Run through manual checklist
4. Test fresh import into a new empty Unity 6 project
5. Verify `Tools > Entity Base > Setup Test Scene` works from scratch
6. Play the test scene for 2 minutes, exercise all controls
7. Verify no console errors or warnings (excluding expected A* warnings without navmesh)
