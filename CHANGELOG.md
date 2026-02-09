# Changelog

## [0.2.0] - 2026-02-08

### Changed
- **Package renamed** from `com.yourname.entity-controller` to `com.yourname.entity-base`
- **Namespace renamed** from `EntityController` to `EntityBase` (all assemblies)
- **Menu paths** changed from `Tools/Entity Controller/` to `Tools/Entity Base/`
- `EntityControllerSettings` renamed to `EntityBaseSettings`

### Added

#### Locomotion
- `LocomotionMode` enum: Ground, Swimming, Flying
- `EntityMotor.SetLocomotionMode()` API for switching between modes
- Swimming physics: buoyancy, water drag, 3D water movement with ascend/descend
- Flying physics: gravity-free 3D movement with ascend/descend and altitude hold
- `swimSpeed`, `swimSprintSpeed`, `swimVerticalSpeed`, `waterDrag`, `buoyancy`, `waterSurfaceY` motor settings
- `flySpeed`, `flySprintSpeed`, `flyVerticalSpeed` motor settings

#### Input
- `IMovementInput.AscendInput` and `DescendInput` properties
- `PlayerMovementInput`: AscendInput reads held Space, DescendInput reads Descend/Crouch action
- `NPCMovementInput`: ShouldAscend/ShouldDescend AI-settable properties

#### Animation
- `EntityAnimator` bridge component: reads motor state, writes 7 parameters to Animator
- Parameters: Speed, VerticalSpeed, IsGrounded, IsSprinting, LocomotionMode, Jump (trigger), Land (trigger)
- `AnimatorControllerGenerator` editor tool: generates base controller with Ground/Swim/Fly sub-state machines
- 16 placeholder clips + AnimatorOverrideController for easy clip swapping

#### Events
- `OnLocomotionModeChanged(from, to)` event
- `OnStartedSwimming`, `OnStartedFlying`, `OnReturnedToGround` events

#### Template
- `swimSpeed`, `swimSprintSpeed`, `flySpeed`, `flySprintSpeed` fields on EntityTemplate

#### HUD
- `ControlsHUD` now shows mode-aware hints (Ascend/Descend when swimming/flying)

---

## [0.1.0] - 2026-02-08

### Added

#### Core
- `EntityIdentity` component for Player/NPC designation with Odin toggle buttons
- `EntityMotor` CharacterController-based movement with walk, sprint, jump, gravity
- `IMovementInput` interface decoupling motor from input source
- `PlayerMovementInput` Unity Input System implementation
- `NPCMovementInput` A* Pathfinding Project implementation with SetDestination/Stop API
- `EntityEvents` central event bus for stat, inventory, ability, and movement events

#### Camera
- `CameraPerspectiveManager` Cinemachine 3 first/third-person toggle
- `ControlsHUD` IMGUI controls overlay

#### Attributes
- `StatDefinition` ScriptableObject template for stat types
- `StatInstance` runtime stat with auto-tick, decay modifiers, and regen delay
- `EntityAttributes` per-entity stat container with cross-stat influence rules

#### Inventory
- `InventoryItem` ScriptableObject with stacking and consumable support
- `Inventory` slot-based inventory with AddItem, RemoveItem, ConsumeItem

#### Abilities
- `AbilityDefinition` ScriptableObject with costs, cooldowns, cast times
- `AbilitySystem` ability management with resource checking and effect spawning

#### Editor
- `EntityCreator` menu items: Create Player, Create NPC, Setup Test Scene
- `EntityCreator` asset generators: Create Default Stat Assets, Create Sample Items, Create Sample Abilities
- `CameraFixUtility` Cinemachine camera settings repair tool
- `EntityValidator` entity component validation with context menu support
- `EntityGizmos` SceneView visualization for ground checks and NPC paths
- `EntityBaseSettings` Project Settings provider

#### Samples
- Default Assets sample (import via Package Manager > Samples)
- `SampleSetup` one-click editor script: Setup Everything, Create All Default Assets
- 7 default StatDefinition presets (HP, Energy, Mana, Rest, Hunger, Thirst, Happiness)
- 3 sample InventoryItem assets (Health Potion, Bread, Iron Sword)
- 3 sample AbilityDefinition assets (Fireball, Heal, Dash)

#### Documentation
- Comprehensive README with architecture overview and API reference
- EXTENSIBILITY.md with integration patterns for AI, combat, animation, UI, networking
- TESTING.md with unit test specs, integration tests, and manual checklist
