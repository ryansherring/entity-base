# Entity Base

> Modular character creation, movement, stats, inventory, and abilities for Player and NPC entities in Unity 6.

**Entity Base** is the movement and entity foundation layer for Unity games. It provides a shared `CharacterController`-based motor driven by swappable input sources -- Unity Input System for players, A* Pathfinding Project for NPCs -- with a full attribute system, inventory, and ability framework on top.

Other packages (AI behavior, combat, dialogue, networking) build on top of this foundation by subscribing to the `EntityEvents` event bus.

## Table of Contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Components Reference](#components-reference)
- [Editor Tools](#editor-tools)
- [Events System](#events-system)
- [Configuration](#configuration)
- [Controls](#controls)
- [Package Structure](#package-structure)
- [Extending the Package](#extending-the-package)

---

## Features

### Movement
- **Shared EntityMotor** -- Single `CharacterController`-based movement component works for both Player and NPC
- **IMovementInput interface** -- Decouples motor from input source; swap between player input and AI pathfinding
- **First/third-person toggle** -- Press V to switch perspectives via Cinemachine 3 camera blending
- **Camera-relative movement** -- WASD always moves relative to the camera; NPCs use their own forward

### Stats & Attributes
- **Dynamic stat list** -- `List<StatInstance>` driven by `StatDefinition` ScriptableObject assets; any number of stats per entity
- **Auto-decay with regen delay** -- Stats tick automatically; regen pauses after taking damage
- **Cross-stat modifiers** -- Low energy accelerates hunger; low rest drains energy faster (null-safe for missing stats)
- **Runtime API** -- `GetStat()`, `AddStat()`, `RemoveStat()`, `Initialize()` for full stat management
- **Stats HUD** -- IMGUI stat bars at top-left showing colored bars for HP (red), Energy (yellow), Mana (blue), etc.

### Inventory
- **Stacking** -- Items auto-stack up to `maxStackSize`; overflow creates new slots
- **Consumables** -- Items can restore stats when consumed (configurable per item)
- **Odin TableList** -- Clean table-based inspector with item icon previews

### Abilities
- **Cooldowns and cast times** -- Per-ability cooldown tracking with optional cast-before-execute
- **Generic stat costs** -- `List<AbilityCost>` allows abilities to cost any combination of stats (Mana, Energy, HP, custom)
- **Effect prefabs** -- Optional VFX spawned on ability execution

### Editor Tooling
- **One-click entity creation** -- `Tools > Entity Base > Create Player/NPC` builds fully-configured entities
- **Scene setup wizard** -- `Tools > Entity Base > Setup Test Scene` creates ground, player, NPC, and dual Cinemachine cameras
- **Entity validation** -- `Tools > Entity Base > Validate All Entities` checks for missing components, wrong configurations, and wiring issues
- **Camera fix utility** -- Repairs Cinemachine struct-based settings using the copy-back pattern
- **SceneView gizmos** -- Ground check visualization, NPC destination lines, entity identity labels
- **Project Settings** -- `Edit > Project Settings > Entity Base` for default values

### Events System
- **EntityEvents component** -- Central event bus for stat changes, inventory operations, ability usage, movement events, and NPC navigation
- **Zero coupling** -- External systems subscribe to events without referencing package internals

---

## Requirements

| Dependency | Version | Purpose |
|---|---|---|
| **Unity** | 6000.x+ (Unity 6) | Minimum target |
| **Universal Render Pipeline** | 17.x | Rendering (for material creation in editor tools) |
| **Input System** | >= 1.18.0 | Player input (auto-installed as package dependency) |
| **Cinemachine** | >= 3.1.0 | Camera management (auto-installed as package dependency) |
| **A* Pathfinding Project** | >= 5.4.0 | NPC navigation (must be installed separately) |
| **Odin Inspector** | >= 3.x | Inspector UX (DLL plugin, must be installed separately) |

---

## Installation

### Prerequisites

Before installing Entity Base, ensure these are already in your project:
1. **A* Pathfinding Project** (>= 5.4) — from the Asset Store or as a package
2. **Odin Inspector** (>= 3.x) — DLL plugin in `Assets/Plugins/Sirenix/`
3. An **InputActionAsset** with a `Player` action map containing `Move` (Vector2), `Jump` (Button), and `Sprint` (Button) actions

### Option A: Git URL (Recommended)

In Unity: **Window > Package Manager > + > Add package from git URL**, then enter:

```
https://github.com/ransherring/entity-base.git
```

To pin a specific version, append the tag:

```
https://github.com/ransherring/entity-base.git#v0.2.0
```

Or add directly to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.ransherring.entity-base": "https://github.com/ransherring/entity-base.git#v0.2.0"
  }
}
```

### Option B: Local / Embedded Package (for Development)

Clone or copy the package folder into your project's `Packages/` directory:

```
YourProject/
  Packages/
    com.ransherring.entity-base/   <-- this package
    manifest.json
```

Unity auto-detects embedded packages. No `manifest.json` changes needed.

---

## Quick Start

### 1. Create a Player Entity

`Tools > Entity Base > Create Player`

This creates a capsule with:
- `EntityIdentity` (Player type)
- `CharacterController`
- `EntityMotor`
- `PlayerMovementInput` (wired to your InputActionAsset)
- `CameraPerspectiveManager`
- `EntityAttributes`, `Inventory`, `AbilitySystem`
- `EntityEvents`
- `ControlsHUD` (hidden by default, press ` to toggle)
- `StatsHUD` (shows stat bars for showInHUD stats)
- `SightSensor` + `HearingSensor`
- `HeadTarget` child object at eye level

### 2. Create an NPC Entity

`Tools > Entity Base > Create NPC`

This creates a capsule with:
- `EntityIdentity` (NPC type)
- `CharacterController`
- `EntityMotor`
- `NPCMovementInput` + `Seeker` + `AIPath` (A* Pathfinding)
- `EntityAttributes`, `Inventory`, `AbilitySystem`
- `EntityEvents`
- `SightSensor` + `HearingSensor`

### 3. One-Click Test Scene

`Tools > Entity Base > Setup Test Scene`

Creates a complete test environment:
- 100x100 ground plane
- Player entity (blue capsule)
- NPC entity (red capsule)
- Third-person orbital camera (Cinemachine)
- First-person head-locked camera (Cinemachine)
- Main Camera with CinemachineBrain

Press Play and move with WASD immediately.

---

## Architecture

```
                    EntityIdentity
                         |
              +----------+----------+
              |                     |
         [Player]              [NPC]
              |                     |
    PlayerMovementInput    NPCMovementInput
              |                     |
              +-----> IMovementInput <-----+
                           |
                      EntityMotor
                     (CharacterController)
                           |
              +-----+------+------+-----+
              |     |      |      |     |
          EntityAttributes  Inventory  AbilitySystem  EntityEvents
              |
    List<StatInstance> (dynamic)
    [HP] [Energy] [Mana] [Rest] [Hunger] ...
    (driven by StatDefinition ScriptableObjects)
```

### Key Design Decisions

- **IMovementInput** decouples the motor from the input source. The same `EntityMotor` drives both player (keyboard) and NPC (pathfinding) movement.
- **NPC pathfinding delegation**: `AIPath.canMove = false` prevents A* from moving the transform. Instead, `NPCMovementInput` reads `AIPath.desiredVelocity` and translates it into `IMovementInput` vectors for the shared motor.
- **Cinemachine struct pattern**: `CameraTarget` and `TrackerSettings` are structs. The package consistently uses the copy-modify-assign-back pattern.
- **EntityEvents** is optional but recommended. Components check for it with null-conditional (`_events?.RaiseFoo()`), so entities work fine without it.

---

## Components Reference

### Core

| Component | Purpose | Required On |
|---|---|---|
| `EntityIdentity` | Marks entity as Player or NPC, holds display name and model prefab | All entities |
| `EntityMotor` | CharacterController-based movement (walk, sprint, jump, gravity) | All entities |
| `PlayerMovementInput` | Reads Unity Input System Player action map | Player only |
| `NPCMovementInput` | Reads A* Pathfinding desired velocity | NPC only |
| `EntityEvents` | Event bus for stat/inventory/ability/movement events | All entities (optional) |

### Camera & HUD (Player Only)

| Component | Purpose |
|---|---|
| `CameraPerspectiveManager` | Toggles first/third-person Cinemachine cameras |
| `ControlsHUD` | IMGUI controls overlay (backtick toggle, hidden by default) |
| `StatsHUD` | IMGUI stat bars for stats with `showInHUD = true` |

### Data Systems

| Component | Purpose |
|---|---|
| `EntityAttributes` | Dynamic `List<StatInstance>` with lookup API (`GetStat`, `AddStat`, `Initialize`) |
| `Inventory` | Slot-based inventory with stacking and consumable support |
| `AbilitySystem` | Ability slots with cooldowns, costs, and effect spawning |

### ScriptableObjects

| Asset Type | Create Menu Path | Purpose |
|---|---|---|
| `StatDefinition` | Entity Base/Stat Definition | Template for a stat type (HP, Energy, etc.) |
| `InventoryItem` | Entity Base/Inventory Item | Item definition with stacking and consumable effects |
| `AbilityDefinition` | Entity Base/Ability Definition | Ability template with costs, cooldowns, and effects |

---

## Editor Tools

All tools are under `Tools > Entity Base`:

| Menu Item | Description |
|---|---|
| **Create Player** | Creates a fully configured player entity |
| **Create NPC** | Creates a fully configured NPC entity with A* pathfinding |
| **Setup Test Scene** | One-click test scene with player, NPC, ground, and cameras |
| **Fix Camera Settings** | Repairs Cinemachine orbital/pan-tilt camera configuration |
| **Validate All Entities** | Scans all entities in scene for missing/misconfigured components |

### Context Menu

Right-click any `EntityIdentity` component header > **Validate Entity** to check a single entity.

### Project Settings

`Edit > Project Settings > Entity Base` provides:
- Default movement speeds for new entities
- Default stat definitions for Initialize Defaults
- Gizmo and validation preferences
- InputActionAsset path configuration

### SceneView Gizmos

When an entity is selected:
- **EntityMotor**: Green/red sphere showing ground check (green = grounded, red = airborne)
- **NPCMovementInput**: Orange line to destination, green sphere when arrived
- **EntityIdentity**: Floating label showing `[P]` or `[NPC]` with display name

---

## Events System

Add an `EntityEvents` component to any entity to receive gameplay callbacks:

```csharp
using EntityBase;

public class CombatSystem : MonoBehaviour
{
    void Start()
    {
        var events = GetComponent<EntityEvents>();

        events.OnStatDepleted += stat => {
            if (stat.definition.statName == "HP")
                HandleDeath();
        };

        events.OnAbilityExecuted += ability => {
            Debug.Log($"Used {ability.abilityName}");
        };

        events.OnLanded += () => {
            // Play landing sound/particles
        };

        events.OnDestinationReached += () => {
            // NPC arrived, pick next task
        };
    }
}
```

### Available Events

| Event | Args | Fires When |
|---|---|---|
| `OnStatChanged` | `StatInstance, float oldVal, float newVal` | Any stat value changes |
| `OnStatDepleted` | `StatInstance` | Stat reaches minimum |
| `OnStatFull` | `StatInstance` | Stat reaches maximum |
| `OnItemAdded` | `InventoryItem, int amount` | Item added to inventory |
| `OnItemRemoved` | `InventoryItem, int amount` | Item removed from inventory |
| `OnItemConsumed` | `InventoryItem` | Consumable used |
| `OnAbilityStarted` | `AbilityDefinition` | Ability activated (or cast begins) |
| `OnAbilityExecuted` | `AbilityDefinition` | Ability effect fires |
| `OnAbilityFailed` | `AbilityDefinition, string reason` | Ability blocked (cooldown, cost) |
| `OnLanded` | -- | Entity touches ground after airborne |
| `OnJumped` | -- | Entity jumps |
| `OnSprintChanged` | `bool isSprinting` | Sprint state toggles |
| `OnDestinationReached` | -- | NPC arrives at destination |
| `OnDestinationSet` | `Vector3 destination` | NPC given new destination |

---

## Configuration

### StatDefinition Fields

| Field | Type | Description |
|---|---|---|
| `statName` | string | Display name |
| `icon` | Sprite | HUD/inspector icon |
| `minValue` / `maxValue` | float | Value bounds |
| `defaultValue` | float | Starting value |
| `baseDecayRate` | float | Per-second change (negative = decay, positive = regen, 0 = static) |
| `regenDelay` | float | Seconds after damage before regen resumes |
| `barColor` | Color | Progress bar color |
| `showInHUD` | bool | Whether to display in player HUD |

### Cross-Stat Modifier Rules (NPC)

| Condition | Effect |
|---|---|
| Energy < 30% | Hunger decays 1.5x faster |
| Rest < 25% | Energy decays 1.75x faster |
| Hunger < 20% | Happiness decays 1.5x faster |
| Thirst < 20% | Happiness decays 1.5x faster |
| Both Hunger + Thirst < 20% | Happiness decays 2.0x faster |

---

## Controls

| Input | Action |
|---|---|
| **WASD** | Move (camera-relative) |
| **Mouse** | Look / Orbit |
| **Scroll** | Zoom In / Out (third-person) |
| **Space** | Jump |
| **Shift** | Sprint |
| **V** | Toggle first/third person perspective |
| **`** (backtick) | Toggle dev mode (controls overlay) |

---

## Package Structure

```
com.ransherring.entity-base/
  package.json
  README.md, CHANGELOG.md, LICENSE.md

  Runtime/
    EntityBase.asmdef
    Core/
      EntityIdentity.cs       # Player/NPC identity
      IMovementInput.cs       # Input abstraction interface
      PlayerMovementInput.cs  # Unity Input System implementation
      NPCMovementInput.cs     # A* Pathfinding implementation
      EntityMotor.cs          # Shared CharacterController motor
      EntityEvents.cs         # Event bus for external systems
    Camera/
      CameraPerspectiveManager.cs  # First/third person toggle
      ControlsHUD.cs               # IMGUI controls overlay (backtick toggle)
      StatsHUD.cs                  # IMGUI stat bars (Player only)
    Attributes/
      StatDefinition.cs       # ScriptableObject stat template
      StatInstance.cs          # Runtime stat with auto-tick
      EntityAttributes.cs     # Per-entity stat container
    Inventory/
      InventoryItem.cs        # ScriptableObject item definition
      Inventory.cs            # Slot-based inventory
    Abilities/
      AbilityDefinition.cs    # ScriptableObject ability template
      AbilitySystem.cs        # Cooldowns, costs, execution

  Editor/
    EntityBase.Editor.asmdef
    EntityCreator.cs              # Create Player/NPC/Test Scene menu items
    CameraFixUtility.cs           # Fix Cinemachine camera settings
    EntityValidator.cs            # Component validation tool
    EntityGizmos.cs               # SceneView visualization
    EntityBaseSettings.cs   # Project Settings provider

  Samples~/
    (imported via Package Manager > Samples)

  Documentation~/
    index.md
```

---

## Extending the Package

### Custom Movement Input

Implement `IMovementInput` to create new input sources (e.g., replay playback, network sync):

```csharp
using EntityBase;

public class ReplayMovementInput : MonoBehaviour, IMovementInput
{
    public Vector2 MoveInput { get; private set; }
    public bool JumpInput { get; private set; }
    public bool SprintInput { get; private set; }
    public void ConsumeJump() => JumpInput = false;

    // Feed recorded input data each frame...
}
```

### Custom Stat Effects

Subscribe to `EntityEvents.OnStatChanged` or modify `StatInstance.Tick()` behavior via `decayModifier`:

```csharp
// Poison effect: double HP decay for 10 seconds
var attributes = GetComponent<EntityAttributes>();
var hp = attributes.GetStat("HP");
if (hp != null) hp.decayModifier = 2.0f;
```

### NPC AI Integration

```csharp
var npcInput = GetComponent<NPCMovementInput>();
var events = GetComponent<EntityEvents>();

npcInput.SetDestination(targetPosition);
events.OnDestinationReached += () => {
    // Pick next waypoint, start interaction, etc.
};
```

See [EXTENSIBILITY.md](EXTENSIBILITY.md) for detailed integration patterns.

---

## License

MIT License. See [LICENSE.md](LICENSE.md).
