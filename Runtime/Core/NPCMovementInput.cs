using UnityEngine;
using Pathfinding;

namespace EntityBase
{
    /// <summary>
    /// Implements IMovementInput via A* Pathfinding Project.
    /// Disables AIPath's transform updates and translates desired velocity into
    /// IMovementInput so the shared EntityMotor drives movement via CharacterController.
    /// </summary>
    [RequireComponent(typeof(Seeker))]
    [RequireComponent(typeof(AIPath))]
    public class NPCMovementInput : MonoBehaviour, IMovementInput
    {
        private AIPath _aiPath;
        private Seeker _seeker;
        private EntityEvents _events;

        private Vector2 _moveInput;
        private bool _jumpInput;
        private bool _sprintInput;
        private bool _wasAtDestination = true;

        /// <summary>Whether the NPC should sprint to its destination.</summary>
        public bool ShouldSprint { get; set; }

        /// <summary>AI-settable: whether the NPC should ascend (swim/fly up).</summary>
        public bool ShouldAscend { get; set; }

        /// <summary>AI-settable: whether the NPC should descend (swim/fly down).</summary>
        public bool ShouldDescend { get; set; }

        public Vector2 MoveInput => _moveInput;
        public bool JumpInput => _jumpInput;
        public bool SprintInput => _sprintInput;
        public bool AscendInput => ShouldAscend;
        public bool DescendInput => ShouldDescend;

        /// <summary>True when the NPC has reached its current destination.</summary>
        public bool HasReachedDestination => _aiPath != null && _aiPath.reachedDestination;

        public void ConsumeJump()
        {
            _jumpInput = false;
        }

        /// <summary>
        /// Set a world-space destination for the NPC to navigate to.
        /// </summary>
        public void SetDestination(Vector3 destination)
        {
            if (_aiPath != null)
            {
                _aiPath.destination = destination;
                _aiPath.SearchPath();
                _wasAtDestination = false;
                _events?.RaiseDestinationSet(destination);
            }
        }

        /// <summary>
        /// Stop the NPC from moving.
        /// </summary>
        public void Stop()
        {
            if (_aiPath != null)
            {
                _aiPath.destination = transform.position;
                _aiPath.SetPath(null);
            }
            _moveInput = Vector2.zero;
        }

        private void Awake()
        {
            _seeker = GetComponent<Seeker>();
            _aiPath = GetComponent<AIPath>();
            _events = GetComponent<EntityEvents>();

            if (_aiPath != null)
            {
                // Disable AIPath's movement and rotation so EntityMotor handles it.
                // Keep updatePosition = true so AIPath reads the transform position
                // (which CharacterController updates) for path calculations.
                _aiPath.canMove = false;
                _aiPath.updateRotation = false;
            }
        }

        private void Update()
        {
            if (_aiPath == null || !_aiPath.hasPath)
            {
                _moveInput = Vector2.zero;
                return;
            }

            if (_aiPath.reachedDestination)
            {
                if (!_wasAtDestination)
                {
                    _wasAtDestination = true;
                    _events?.RaiseDestinationReached();
                }
                _moveInput = Vector2.zero;
                return;
            }

            // Get the desired velocity from AIPath and convert to a 2D input vector
            Vector3 desiredVelocity = _aiPath.desiredVelocity;

            if (desiredVelocity.sqrMagnitude > 0.01f)
            {
                // Normalize to a direction and convert to input-space (x = horizontal, y = forward)
                Vector3 dir = desiredVelocity.normalized;
                _moveInput = new Vector2(dir.x, dir.z);

                // Clamp to unit circle
                if (_moveInput.sqrMagnitude > 1f)
                    _moveInput = _moveInput.normalized;
            }
            else
            {
                _moveInput = Vector2.zero;
            }

            _sprintInput = ShouldSprint;
        }
    }
}
