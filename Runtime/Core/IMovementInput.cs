using UnityEngine;

namespace EntityBase
{
    /// <summary>
    /// Interface decoupling the EntityMotor from its input source.
    /// Implemented by PlayerMovementInput (Input System) and NPCMovementInput (A* Pathfinding).
    /// </summary>
    public interface IMovementInput
    {
        /// <summary>Current movement input as a normalized 2D vector (x = horizontal, y = forward).</summary>
        Vector2 MoveInput { get; }

        /// <summary>True when a jump has been requested (consumed after use).</summary>
        bool JumpInput { get; }

        /// <summary>True while sprint is held.</summary>
        bool SprintInput { get; }

        /// <summary>True while ascend is held (swim up / fly up). Distinct from JumpInput which is an impulse.</summary>
        bool AscendInput { get; }

        /// <summary>True while descend is held (swim down / fly down).</summary>
        bool DescendInput { get; }

        /// <summary>Call after processing jump to prevent repeated jumps from a single press.</summary>
        void ConsumeJump();
    }
}
