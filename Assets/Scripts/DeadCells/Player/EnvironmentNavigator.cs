using UnityEngine;

namespace DeadCells.Player
{
    public class EnvironmentNavigator : MonoBehaviour
    {
        [Header("Ground")]
        [SerializeField] private Transform _groundChecker;
        [SerializeField] private Vector2 _groundCheckRect = new(0.7f, 0.15f);
        [SerializeField] private LayerMask _ground;
        [SerializeField] private LayerMask _oneWayPlatform;

        [Header("Stand")]
        [SerializeField] private Transform _standChecker;
        [SerializeField] private Vector2 _standCheckRect = new(0.7f, 0.8f);

        [Header("Wall")]
        [SerializeField] private Transform _wallChecker;
        [SerializeField] private Vector2 _wallCheckRect = new(0.2f, 1.2f);
        [SerializeField] private LayerMask _wall;

        [Header("Ledge")]
        [SerializeField] private Transform _ledgeChecker;
        [SerializeField] private Vector2 _ledgeCheckRect = new(0.2f, 0.6f);

        [Header("Ladder")]
        [SerializeField] private Transform _ladderChecker;
        [SerializeField] private Vector2 _ladderCheckRect = new(0.6f, 1.4f);
        [SerializeField] private LayerMask _ladder;

        public bool IsGrounded => GetGroundCollider() != null;
        public bool IsOnOneWayPlatform => CurrentOneWayPlatform != null;
        public bool HasLadder => CurrentLadder != null;
        public bool IsTouchingWall => CurrentWall != null;

        public Collider2D CurrentOneWayPlatform => GetGroundCollider(_oneWayPlatform);
        public Collider2D CurrentWall => Physics2D.OverlapBox(_wallChecker.position, _wallCheckRect, 0, _wall);
        public Collider2D CurrentLadder => Physics2D.OverlapBox(_ladderChecker.position, _ladderCheckRect, 0, _ladder);

        public float LadderCenterX => CurrentLadder == null ? transform.position.x : CurrentLadder.bounds.center.x;

        private PlatformEffector2D _dropPlatform;
        private float _dropPlatformRotation;
        private float _dropPlatformTimer;

        private void FixedUpdate()
        {
            if(_dropPlatformTimer <= 0)
                return;

            _dropPlatformTimer -= Time.fixedDeltaTime;

            if(_dropPlatformTimer <= 0)
                ResetDropPlatform();
        }

        public bool CanStand() =>
            Physics2D.OverlapBox(_standChecker.position, _standCheckRect, 0, _ground.value | _wall.value) == null;

        public bool GroundBelow(float distance)
        {
            var position = (Vector2)_groundChecker.position + Vector2.down * distance / 2;
            var size = new Vector2(_groundCheckRect.x, _groundCheckRect.y + distance);

            return Physics2D.OverlapBox(position, size, 0, _ground.value | _oneWayPlatform.value) != null;
        }

        public bool TryGetLedge(StateCollider stateCollider, float direction, out Vector2 position)
        {
            position = Vector2.zero;

            var wall = CurrentWall;

            if(wall == null)
                return false;

            if(Physics2D.OverlapBox(_ledgeChecker.position, _ledgeCheckRect, 0, _wall) != null)
                return false;

            var playerCollider = stateCollider.Collider;

            var targetColliderCenterX = direction > 0
                ? wall.bounds.min.x + playerCollider.bounds.extents.x
                : wall.bounds.max.x - playerCollider.bounds.extents.x;

            var targetColliderCenterY = wall.bounds.max.y + playerCollider.bounds.extents.y;

            var centerOffset = playerCollider.bounds.center - transform.position;

            position = new Vector2(
                targetColliderCenterX - centerOffset.x,
                targetColliderCenterY - centerOffset.y);

            return true;
        }

        public void DropThroughOneWay(float time)
        {
            var platform = CurrentOneWayPlatform;

            if(platform == null)
                return;

            var effector = platform.GetComponent<PlatformEffector2D>();

            if(effector == null)
                return;

            if(_dropPlatform != null)
                ResetDropPlatform();

            _dropPlatform = effector;
            _dropPlatformRotation = effector.rotationalOffset;
            _dropPlatform.rotationalOffset = _dropPlatformRotation + 180;
            _dropPlatformTimer = time;
        }

        private void ResetDropPlatform()
        {
            if(_dropPlatform == null)
                return;

            _dropPlatform.rotationalOffset = _dropPlatformRotation;
            _dropPlatform = null;
            _dropPlatformTimer = 0;
        }

        private Collider2D GetGroundCollider() =>
            GetGroundCollider(_ground.value | _oneWayPlatform.value);

        private Collider2D GetGroundCollider(LayerMask mask) =>
            Physics2D.OverlapBox(_groundChecker.position, _groundCheckRect, 0, mask);

        private void OnDrawGizmos()
        {
            if(_groundChecker != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(_groundChecker.position, _groundCheckRect);
            }

            if(_standChecker != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(_standChecker.position, _standCheckRect);
            }

            if(_wallChecker != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(_wallChecker.position, _wallCheckRect);
            }

            if(_ledgeChecker != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(_ledgeChecker.position, _ledgeCheckRect);
            }

            if(_ladderChecker != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(_ladderChecker.position, _ladderCheckRect);
            }
        }
    }
}