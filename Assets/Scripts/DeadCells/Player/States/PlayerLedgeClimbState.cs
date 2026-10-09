using UnityEngine;

namespace DeadCells.Player.States
{
    [DisallowMultipleComponent]
    public sealed class PlayerLedgeClimbState : PlayerMovementState
    {
        [Header("Wall Check")]
        [SerializeField] private Transform _wallChecker;
        [SerializeField] private Vector2 _wallCheckRect = new Vector2(0.2f, 1.2f);
        [SerializeField] private LayerMask _wall;

        [Header("Ledge Check")]
        [SerializeField] private Transform _ledgeChecker;
        [SerializeField] private Vector2 _ledgeCheckRect = new Vector2(0.2f, 0.6f);
        [SerializeField] private float _ledgeClearance = 0.02f;

        [Header("Ledge")]
        [SerializeField] private float _ledgeClimbSpeed = 6;
        [SerializeField] private Collider2D _standCollider;

        private Vector2 _ledgeTarget;
        private Vector2 _climbCorner;

        public override PlayerState Id => PlayerState.LedgeClimb;
        public override int ActivationPriority => 50;
        
        private bool _isComplete;
        private bool _isAvailable;

        public override bool CanBeActivated(PlayerState? currentState)
        {
            if(currentState == Id)
                return !_isComplete;

            if(currentState != PlayerState.Air || AnimationController.StateValue <= 0)
                return false;

            FindColliders(_wallChecker, _wallCheckRect, _wall);
            var wall = Overlaps.Count > 0 ? Overlaps[0] : null;
            _isAvailable = TryGetLedge(wall, _standCollider, out var position);

            if(_isAvailable)
            {
                _ledgeTarget = position;

                if(Input.Vertical >= 0)
                    return true;
            }

            return false;
        }

        public override void EnterState()
        {
            Rigidbody.gravityScale = 0;
            Rigidbody.linearVelocity = Vector2.zero;
            _isComplete = false;
            _climbCorner = new Vector2(Rigidbody.position.x, _ledgeTarget.y);
        }

        public override void UpdateState()
        {
            var target = Rigidbody.position.y < _ledgeTarget.y - 0.01f ? _climbCorner : _ledgeTarget;
            var position = Vector2.MoveTowards(Rigidbody.position, target, _ledgeClimbSpeed * Time.fixedDeltaTime);
            Rigidbody.MovePosition(position);

            if(Vector2.Distance(position, _ledgeTarget) > 0.01f)
                return;

            Rigidbody.position = _ledgeTarget;
            _isComplete = true;
        }

        private bool TryGetLedge(Collider2D wall, Collider2D activeCollider, out Vector2 position)
        {
            position = Vector2.zero;

            if(wall == null || _ledgeChecker == null || activeCollider == null)
                return false;

            if(Overlaps.Count > 0)
                return false;

            var playerBounds = activeCollider.bounds;
            var centerX = transform.forward.z > 0 ? wall.bounds.min.x + playerBounds.extents.x + _ledgeClearance
                : wall.bounds.max.x - playerBounds.extents.x - _ledgeClearance;
            var center = new Vector2(centerX, wall.bounds.max.y + playerBounds.extents.y + _ledgeClearance);
            if(Overlaps.Count > 0)
                return false;

            var centerOffset = (Vector2)playerBounds.center - Rigidbody.position;
            position = center - centerOffset;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            DrawChecker(_wallChecker, _wallCheckRect, Color.yellow);
            DrawChecker(_ledgeChecker, _ledgeCheckRect, Color.green);
        }

        private static void DrawChecker(Transform checker, Vector2 size, Color color)
        {
            if(checker == null)
                return;

            Gizmos.color = color;
            Gizmos.DrawWireCube(checker.position, size);
        }
    }
}
