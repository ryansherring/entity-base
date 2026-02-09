# Extensibility Guide

This document explains how to extend and integrate with `com.yourname.entity-base` from external packages and game-specific code.

---

## Table of Contents

- [Extension Points Overview](#extension-points-overview)
- [Custom Movement Inputs](#custom-movement-inputs)
- [AI Behavior Integration](#ai-behavior-integration)
- [Combat System Integration](#combat-system-integration)
- [Custom Stats and Decay Rules](#custom-stats-and-decay-rules)
- [Custom Inventory Behaviors](#custom-inventory-behaviors)
- [Custom Ability Execution](#custom-ability-execution)
- [Animation Integration](#animation-integration)
- [Networking / Multiplayer](#networking--multiplayer)
- [UI Integration](#ui-integration)
- [Creating Additional Editor Tools](#creating-additional-editor-tools)
- [Assembly Reference Setup](#assembly-reference-setup)

---

## Extension Points Overview

The package is designed with specific seams for extension:

| Extension Point | Mechanism | Use Case |
|---|---|---|
| `IMovementInput` | Interface implementation | Custom input sources (network, replay, AI) |
| `EntityEvents` | Event subscription | React to gameplay events without coupling |
| `StatDefinition` | ScriptableObject | Define new stat types |
| `StatInstance.decayModifier` | Public field | Buff/debuff systems |
| `InventoryItem` | ScriptableObject | Define new item types |
| `AbilityDefinition` | ScriptableObject | Define new abilities |
| `AbilitySystem.TryUseAbility()` | Public API | Trigger abilities from external systems |
| `NPCMovementInput.SetDestination()` | Public API | Drive NPC navigation from AI |
| `Inventory.AddItem() / RemoveItem()` | Public API | Loot, shops, crafting |

---

## Custom Movement Inputs

### When to Use

- Networked character with replicated input
- Cutscene/scripted movement
- Replay system
- Alternative input device (gamepad with custom mapping)

### How to Implement

Create a MonoBehaviour that implements `IMovementInput`:

```csharp
using UnityEngine;
using EntityBase;

public class NetworkMovementInput : MonoBehaviour, IMovementInput
{
    public Vector2 MoveInput { get; private set; }
    public bool JumpInput { get; private set; }
    public bool SprintInput { get; private set; }

    public void ConsumeJump() => JumpInput = false;

    // Called by your networking layer when input arrives
    public void ApplyNetworkInput(Vector2 move, bool jump, bool sprint)
    {
        MoveInput = move;
        JumpInput = jump;
        SprintInput = sprint;
    }
}
```

Then attach it to the entity instead of `PlayerMovementInput` or `NPCMovementInput`. The `EntityMotor` will find it via `GetComponent<IMovementInput>()`.

### Scripted Movement Example

```csharp
public class CutsceneMovementInput : MonoBehaviour, IMovementInput
{
    public Vector2 MoveInput { get; set; }
    public bool JumpInput { get; set; }
    public bool SprintInput { get; set; }
    public void ConsumeJump() => JumpInput = false;

    public void WalkForward() => MoveInput = new Vector2(0, 1);
    public void Stop() => MoveInput = Vector2.zero;
}
```

---

## AI Behavior Integration

The package provides movement but **not** decision-making. An AI behavior tree / GOAP / utility system drives NPCs by calling into the `NPCMovementInput` and `EntityEvents` APIs.

### Pattern: AI Controller Component

```csharp
using UnityEngine;
using EntityBase;

public class NPCAIController : MonoBehaviour
{
    private NPCMovementInput _movement;
    private EntityAttributes _attributes;
    private EntityEvents _events;
    private Inventory _inventory;

    void Start()
    {
        _movement = GetComponent<NPCMovementInput>();
        _attributes = GetComponent<EntityAttributes>();
        _events = GetComponent<EntityEvents>();
        _inventory = GetComponent<Inventory>();

        _events.OnDestinationReached += OnArrived;
        _events.OnStatDepleted += OnStatDepleted;
    }

    void Update()
    {
        // Example: go to food source when hungry
        if (_attributes.hunger.GetNormalized() < 0.3f)
        {
            Vector3 foodSource = FindNearestFoodSource();
            _movement.SetDestination(foodSource);
            _movement.ShouldSprint = _attributes.hunger.GetNormalized() < 0.1f;
        }
    }

    void OnArrived()
    {
        // Consume food if at food source
        // Use inventory to consume a food item
    }

    void OnStatDepleted(StatInstance stat)
    {
        if (stat.definition.statName == "HP")
            HandleDeath();
    }
}
```

### Behavior Tree Integration

If using a behavior tree package (NodeCanvas, Behavior Designer, etc.), create action nodes that call:

```csharp
// Move to position
npcMovementInput.SetDestination(targetPosition);

// Check if arrived
return npcMovementInput.HasReachedDestination ? NodeResult.Success : NodeResult.Running;

// Stop movement
npcMovementInput.Stop();

// Sprint toggle
npcMovementInput.ShouldSprint = true;
```

---

## Combat System Integration

The entity controller provides the stat backbone but not combat logic. A combat system reads and modifies stats.

### Dealing Damage

```csharp
using EntityBase;

public static class CombatHelper
{
    public static void DealDamage(EntityAttributes target, float amount)
    {
        target.hp.Subtract(amount);

        // Check for death
        if (target.hp.currentValue <= target.hp.definition.minValue)
        {
            // Handle death via events or directly
            var events = target.GetComponent<EntityEvents>();
            // EntityEvents.OnStatDepleted will fire automatically from Subtract
        }
    }
}
```

### Buff/Debuff System

Use `decayModifier` to create temporary effects:

```csharp
using UnityEngine;
using EntityBase;
using System.Collections;

public class BuffSystem : MonoBehaviour
{
    public void ApplyPoison(EntityAttributes target, float duration)
    {
        StartCoroutine(PoisonCoroutine(target, duration));
    }

    private IEnumerator PoisonCoroutine(EntityAttributes target, float duration)
    {
        float originalModifier = target.hp.decayModifier;
        target.hp.decayModifier = 3.0f; // Triple HP decay
        yield return new WaitForSeconds(duration);
        target.hp.decayModifier = originalModifier;
    }

    public void ApplySpeedBoost(EntityMotor motor, float duration, float multiplier)
    {
        StartCoroutine(SpeedBoostCoroutine(motor, duration, multiplier));
    }

    private IEnumerator SpeedBoostCoroutine(EntityMotor motor, float duration, float multiplier)
    {
        float originalWalk = motor.walkSpeed;
        float originalSprint = motor.sprintSpeed;
        motor.walkSpeed *= multiplier;
        motor.sprintSpeed *= multiplier;
        yield return new WaitForSeconds(duration);
        motor.walkSpeed = originalWalk;
        motor.sprintSpeed = originalSprint;
    }
}
```

---

## Custom Stats and Decay Rules

### Adding a New Stat Type

1. Create a new `StatDefinition` asset: `Create > Entity Base > Stat Definition`
2. Configure name, range, decay rate, bar color
3. Assign it via the `Initialize Defaults` button on `EntityAttributes`

### Custom Decay Interactions

Override the default cross-stat modifiers by subclassing or wrapping `EntityAttributes`:

```csharp
using EntityBase;

public class CustomEntityAttributes : EntityAttributes
{
    // Add new stats
    public StatInstance stamina = new StatInstance();
    public StatInstance sanity = new StatInstance();

    // Override Update to add custom decay rules
    private void LateUpdate()
    {
        // Custom: low sanity increases mana decay
        if (sanity.definition != null && sanity.GetNormalized() < 0.25f)
            mana.decayModifier = 2.0f;
    }
}
```

Note: Since `EntityAttributes.Update()` is `private`, you'd add custom logic in `LateUpdate()` or subscribe to events. For deeper customization, you can also modify `EntityAttributes` directly in your fork of the package.

---

## Custom Inventory Behaviors

### Loot Drops

```csharp
using EntityBase;

public class LootDrop : MonoBehaviour
{
    public InventoryItem item;
    public int amount = 1;

    public bool PickUp(Inventory targetInventory)
    {
        int overflow = targetInventory.AddItem(item, amount);
        if (overflow < amount)
        {
            amount = overflow;
            if (amount <= 0)
                Destroy(gameObject);
            return true;
        }
        return false; // Inventory full
    }
}
```

### Shop / Trading

```csharp
public static class TradeHelper
{
    public static bool Trade(
        Inventory buyer, Inventory seller,
        InventoryItem item, int amount,
        InventoryItem currency, int cost)
    {
        if (!buyer.HasItem(currency, cost)) return false;
        if (!seller.HasItem(item, amount)) return false;

        buyer.RemoveItem(currency, cost);
        seller.AddItem(currency, cost);
        seller.RemoveItem(item, amount);
        buyer.AddItem(item, amount);
        return true;
    }
}
```

### Equipment System

The inventory provides stacking and consumption. For equipment, add an equipment component alongside it:

```csharp
using UnityEngine;
using EntityBase;

public class Equipment : MonoBehaviour
{
    public InventoryItem weapon;
    public InventoryItem armor;
    public InventoryItem helmet;

    private Inventory _inventory;

    void Awake() => _inventory = GetComponent<Inventory>();

    public bool Equip(InventoryItem item, string slot)
    {
        if (!_inventory.HasItem(item)) return false;
        _inventory.RemoveItem(item);

        // Swap with currently equipped item
        InventoryItem current = null;
        switch (slot)
        {
            case "weapon": current = weapon; weapon = item; break;
            case "armor": current = armor; armor = item; break;
            case "helmet": current = helmet; helmet = item; break;
        }

        if (current != null)
            _inventory.AddItem(current);

        return true;
    }
}
```

---

## Custom Ability Execution

### Override Ability Behavior

The default `AbilitySystem` spawns an effect prefab. For custom behavior, subscribe to events:

```csharp
using EntityBase;

public class CustomAbilityHandler : MonoBehaviour
{
    void Start()
    {
        var events = GetComponent<EntityEvents>();
        events.OnAbilityExecuted += HandleAbility;
    }

    void HandleAbility(AbilityDefinition def)
    {
        switch (def.targetType)
        {
            case AbilityTargetType.Self:
                ApplySelfBuff(def);
                break;
            case AbilityTargetType.SingleTarget:
                RaycastAndDamage(def);
                break;
            case AbilityTargetType.AreaOfEffect:
                SpawnAOE(def);
                break;
            case AbilityTargetType.Directional:
                LaunchProjectile(def);
                break;
        }
    }

    // Implement each method...
}
```

### Triggering Abilities from UI

```csharp
// In your UI button handler:
var abilitySystem = playerEntity.GetComponent<AbilitySystem>();
bool success = abilitySystem.TryUseAbility(slotIndex);
if (!success)
    ShowErrorMessage("Ability not ready!");
```

---

## Animation Integration

The package does **not** include an animation controller. Wire animations by reading EntityMotor state:

```csharp
using UnityEngine;
using EntityBase;

[RequireComponent(typeof(Animator))]
public class EntityAnimator : MonoBehaviour
{
    private Animator _animator;
    private EntityMotor _motor;
    private EntityEvents _events;
    private IMovementInput _input;

    // Animator parameter hashes
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Grounded = Animator.StringToHash("Grounded");
    private static readonly int Jump = Animator.StringToHash("Jump");

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _motor = GetComponent<EntityMotor>();
        _events = GetComponent<EntityEvents>();
        _input = GetComponent<IMovementInput>();
    }

    void Start()
    {
        if (_events != null)
        {
            _events.OnJumped += () => _animator.SetTrigger(Jump);
            _events.OnAbilityStarted += def => _animator.SetTrigger(def.abilityName);
        }
    }

    void Update()
    {
        _animator.SetFloat(Speed, _motor.currentSpeed);
        _animator.SetBool(Grounded, _motor.isGrounded);
    }
}
```

---

## Networking / Multiplayer

### Approach: Replicate Input, Not State

The cleanest multiplayer pattern is to replicate `IMovementInput` values across the network and let each client's `EntityMotor` simulate locally:

```csharp
// On the owning client, send input to server:
var input = GetComponent<PlayerMovementInput>();
NetworkSend(input.MoveInput, input.JumpInput, input.SprintInput);

// On the receiving client, apply to a NetworkMovementInput:
var netInput = GetComponent<NetworkMovementInput>();
netInput.ApplyNetworkInput(receivedMove, receivedJump, receivedSprint);
```

### State Sync Fallback

For stats and inventory, sync the authoritative state from the server:

```csharp
// Server -> Client stat sync
var attributes = GetComponent<EntityAttributes>();
attributes.hp.SetValue(serverHP);
attributes.energy.SetValue(serverEnergy);
```

---

## UI Integration

### Stat Bars

Read `StatInstance` values to drive UI:

```csharp
using UnityEngine;
using UnityEngine.UI;
using EntityBase;

public class StatBarUI : MonoBehaviour
{
    public EntityAttributes targetAttributes;
    public Image hpBar;
    public Image energyBar;
    public Image manaBar;

    void Update()
    {
        if (targetAttributes == null) return;
        hpBar.fillAmount = targetAttributes.hp.GetNormalized();
        energyBar.fillAmount = targetAttributes.energy.GetNormalized();

        manaBar.gameObject.SetActive(targetAttributes.magicUnlocked);
        if (targetAttributes.magicUnlocked)
            manaBar.fillAmount = targetAttributes.mana.GetNormalized();
    }
}
```

### Inventory UI

Read `Inventory.slots` to populate a grid:

```csharp
void RefreshInventoryUI()
{
    var inventory = player.GetComponent<Inventory>();
    for (int i = 0; i < slotUIs.Length; i++)
    {
        if (i < inventory.slots.Count && inventory.slots[i].item != null)
        {
            slotUIs[i].icon.sprite = inventory.slots[i].item.icon;
            slotUIs[i].countText.text = inventory.slots[i].count.ToString();
            slotUIs[i].gameObject.SetActive(true);
        }
        else
        {
            slotUIs[i].gameObject.SetActive(false);
        }
    }
}
```

### Ability Cooldown UI

```csharp
void UpdateAbilityUI()
{
    var system = player.GetComponent<AbilitySystem>();
    for (int i = 0; i < system.abilities.Count; i++)
    {
        var slot = system.abilities[i];
        abilityIcons[i].sprite = slot.definition?.icon;
        cooldownOverlays[i].fillAmount = slot.IsReady ? 0f
            : slot.cooldownRemaining / slot.definition.cooldown;
    }
}
```

---

## Creating Additional Editor Tools

### Custom Entity Prefab Creator

```csharp
using UnityEditor;
using EntityBase;
using EntityBase.Editor;

public static class CustomEntityTools
{
    [MenuItem("Tools/My Game/Create Guard NPC")]
    static void CreateGuard()
    {
        EntityCreator.CreateNPC();
        var npc = UnityEngine.GameObject.Find("NPC");
        npc.name = "Guard";

        var identity = npc.GetComponent<EntityIdentity>();
        identity.displayName = "Town Guard";

        var motor = npc.GetComponent<EntityMotor>();
        motor.walkSpeed = 3f;
        motor.sprintSpeed = 8f;
    }
}
```

### Custom Validator Rules

```csharp
// Add game-specific validation
[MenuItem("Tools/My Game/Validate Guards")]
static void ValidateGuards()
{
    var identities = Object.FindObjectsByType<EntityIdentity>(FindObjectsSortMode.None);
    foreach (var id in identities)
    {
        if (id.displayName.Contains("Guard"))
        {
            EntityValidator.ValidateEntity(id); // Run standard checks
            // Add custom checks...
            if (id.GetComponent<GuardAI>() == null)
                Debug.LogWarning($"Guard '{id.displayName}' missing GuardAI component", id.gameObject);
        }
    }
}
```

---

## Assembly Reference Setup

To reference Entity Base from your game code, add to your `.asmdef`:

```json
{
    "references": [
        "EntityBase"
    ]
}
```

If your game code doesn't use an asmdef (default assembly), the `EntityBase` assembly is auto-referenced since `autoReferenced: true` in the package asmdef.

To use types from the package in your scripts:

```csharp
using EntityBase;
```

---

## Summary of Integration Patterns

| External System | Reads From | Writes To | Events Used |
|---|---|---|---|
| AI Behavior | `EntityAttributes`, `Inventory` | `NPCMovementInput.SetDestination()` | `OnDestinationReached`, `OnStatDepleted` |
| Combat | `EntityAttributes.hp` | `StatInstance.Subtract()` | `OnStatChanged`, `OnStatDepleted` |
| Buff/Debuff | `StatInstance.decayModifier` | `StatInstance.decayModifier` | `OnStatChanged` |
| Animation | `EntityMotor.currentSpeed`, `.isGrounded` | -- | `OnJumped`, `OnLanded`, `OnSprintChanged` |
| UI | All stat/inventory/ability values | -- | All events |
| Networking | `IMovementInput` values | Custom `IMovementInput` impl | -- |
| Loot | -- | `Inventory.AddItem()` | `OnItemAdded` |
| Shops | `Inventory.HasItem()` | `Inventory.AddItem()`, `.RemoveItem()` | `OnItemAdded`, `OnItemRemoved` |
| Dialogue | `EntityIdentity.DisplayName` | -- | -- |
