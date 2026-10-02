using UnityEngine;

namespace DeadCells.Versions.V12
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
        [SerializeField] private Vector2 _standCheckRect = new(0.7f, 0.7f);

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

        private PlatformEffector2D _dropPlatform;
        private Collider2D _dropPlatformCollider;
        private float _dropPlatformRotation;
        private float _dropPlatformTimer;

        public bool IsGrounded
        {
            get
            {
                var colliders = Physics2D.OverlapBoxAll(_groundChecker.position, _groundCheckRect, 0, _ground.value | _oneWayPlatform.value);

                foreach(var collider in colliders)
                {
                    if(collider != _dropPlatformCollider)
                        return true;
                }

                return false;
            }
        }

        public bool IsOnOneWayPlatform => CurrentOneWayPlatform != null;

        public Collider2D CurrentOneWayPlatform
        {
            get
            {
                var colliders = Physics2D.OverlapBoxAll(_groundChecker.position, _groundCheckRect, 0, _oneWayPlatform);

                foreach(var collider in colliders)
                {
                    if(collider != _dropPlatformCollider)
                        return collider;
                }

                return null;
            }
        }

        public bool IsTouchingWall => CurrentWall != null;

        public Collider2D CurrentWall =>
            Physics2D.OverlapBox(_wallChecker.position, _wallCheckRect, 0, _wall);

        public bool HasLadder => CurrentLadder != null;

        public Collider2D CurrentLadder =>
            Physics2D.OverlapBox(_ladderChecker.position, _ladderCheckRect, 0, _ladder);

        public float LadderCenterX
        {
            get
            {
                var ladder = CurrentLadder;
                return ladder == null ? transform.position.x : ladder.bounds.center.x;
            }
        }

        private void FixedUpdate()
        {
            if(_dropPlatform == null)
            {
                _dropPlatformCollider = null;
                _dropPlatformTimer = 0;
                return;
            }

            _dropPlatformTimer -= Time.fixedDeltaTime;

            if(_dropPlatformTimer <= 0)
                ResetDropPlatform();
        }

        public bool DropThroughOneWay(float time)
        {
            var platform = CurrentOneWayPlatform;

            if(platform == null)
                return false;

            var effector = platform.GetComponent<PlatformEffector2D>();

            if(effector == null)
                return false;

            if(_dropPlatform != null)
                ResetDropPlatform();

            _dropPlatform = effector;
            _dropPlatformCollider = platform;
            _dropPlatformRotation = effector.rotationalOffset;
            _dropPlatform.rotationalOffset = _dropPlatformRotation + 180;
            _dropPlatformTimer = time;

            return true;
        }

        private void ResetDropPlatform()
        {
            if(_dropPlatform != null)
                _dropPlatform.rotationalOffset = _dropPlatformRotation;

            _dropPlatform = null;
            _dropPlatformCollider = null;
            _dropPlatformTimer = 0;
        }

        private void OnDisable()
        {
            if(_dropPlatform != null)
                _dropPlatform.rotationalOffset = _dropPlatformRotation;

            _dropPlatform = null;
            _dropPlatformCollider = null;
            _dropPlatformTimer = 0;
        }

        public bool CanStand()
        {
            return Physics2D.OverlapBox(_standChecker.position, _standCheckRect, 0, _ground.value | _wall.value) == null;
        }

        public bool GroundBelow(float distance)
        {
            var position = (Vector2)_groundChecker.position + Vector2.down * distance / 2;
            var size = new Vector2(_groundCheckRect.x, _groundCheckRect.y + distance);

            var colliders = Physics2D.OverlapBoxAll(position, size, 0, _ground.value | _oneWayPlatform.value);

            foreach(var collider in colliders)
            {
                if(collider != _dropPlatformCollider)
                    return true;
            }

            return false;
        }

        public bool TryGetLedge(StateCollider stateCollider, float direction, out Vector2 position)
        {
            position = Vector2.zero;

            if(stateCollider == null || stateCollider.Collider == null || CurrentWall == null)
                return false;

            if(Physics2D.OverlapBox(_ledgeChecker.position, _ledgeCheckRect, 0, _wall) != null)
                return false;

            var rayDistance = _ledgeChecker.position.y - _wallChecker.position.y + _wallCheckRect.y / 2;
            var surface = Physics2D.Raycast(_ledgeChecker.position, Vector2.down, rayDistance, _wall);

            if(surface.collider == null || surface.normal.y < 0.5f)
                return false;

            var playerCollider = stateCollider.Collider;
            var bounds = playerCollider.bounds;
            var centerOffset = (Vector2)bounds.center - (Vector2)transform.position;
            var size = (Vector2)bounds.size - new Vector2(0.02f, 0.02f);
            var mask = _ground.value | _wall.value;

            var targetCenter = new Vector2(
                surface.point.x + Mathf.Sign(direction) * (bounds.extents.x + 0.02f),
                surface.point.y + bounds.extents.y + 0.02f);

            var climbDistance = targetCenter.y - bounds.center.y;

            if(climbDistance < 0 || Physics2D.OverlapBox(targetCenter, size, 0, mask) != null)
                return false;

            if(Physics2D.BoxCast(bounds.center, size, 0, Vector2.up, climbDistance, mask).collider != null)
                return false;

            var cornerCenter = new Vector2(bounds.center.x, targetCenter.y);
            var horizontalDistance = Mathf.Abs(targetCenter.x - cornerCenter.x);

            if(Physics2D.BoxCast(cornerCenter, size, 0, Vector2.right * Mathf.Sign(direction), horizontalDistance, mask).collider != null)
                return false;

            position = targetCenter - centerOffset;
            return true;
        }

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
