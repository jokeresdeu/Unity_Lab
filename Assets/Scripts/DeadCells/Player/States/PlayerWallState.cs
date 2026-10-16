using UnityEngine;

namespace DeadCells.Player.States
{
    [DisallowMultipleComponent]
    public sealed class PlayerWallState : PlayerMovementState
    {
        [Header("Wall Check")]
        [SerializeField] private Transform _wallChecker;
        [SerializeField] private Vector2 _wallCheckRect = new Vector2(0.2f, 1.2f);
        [SerializeField] private LayerMask _wall;

        [Header("Wall")]
        [SerializeField] private float _wallRunSpeed = 5;
        [SerializeField] private float _wallRunTime = 0.35f;
        [SerializeField] private float _wallHoldTime = 0.2f;
        [SerializeField] private float _wallSlideSpeed = 2;

        [Header("Wall Jump")]
        [SerializeField] private float _wallJumpHorizontalSpeed = 6;
        [SerializeField] private float _wallJumpVerticalSpeed = 10;
        [SerializeField] private float _wallJumpAwayMultiplier = 2;
        [SerializeField] private float _wallJumpTowardMultiplier = 0.5f;
        [SerializeField] private float _wallJumpTowardVerticalMultiplier = 1.2f;
        [SerializeField] private float _wallReattachTime = 0.2f;

        [Header("Contact")]
        [SerializeField] private Collider2D _standCollider;
        [SerializeField] private LayerMask _ground;

        private float _timeOnWall;
        private int _wallDirection;
        private Collider2D _currentWall;
        private float _attachAllowedAt;
        private bool _isGrounded;

        public override PlayerState Id => PlayerState.Wall;
        public override int ActivationPriority => 40;
        public override int AnimationValue => (int)Mathf.Clamp(Rigidbody.linearVelocityY, -1, 1);
        private bool IsAvailable => _currentWall != null && Time.time >= _attachAllowedAt;

        public override bool CanBeActivated(PlayerState? currentState)
        {
            UpdatePhysics();

            if(_isGrounded)
                return false;

            if(currentState != Id || !Input.ConsumeJump())
                return IsAvailable && !(Input.JumpBuffered && Input.Vertical < 0);

            StartWallJump();
            return false;
        }

        public override void ResetState()
        {
            _timeOnWall = 0;
            _attachAllowedAt = 0;
            _currentWall = null;
            _isGrounded = false;
        }

        public override void EnterState()
        {
            var canWallRun = Rigidbody.linearVelocityY > 0;
            Rigidbody.gravityScale = 0;
            Rigidbody.linearVelocity = Vector2.zero;
            _wallDirection = transform.forward.z > 0 ? 1 : -1;
            _timeOnWall = canWallRun ? 0 : _wallRunTime + _wallHoldTime;
            SnapToWall();
            UpdateWallMovement();
        }

        public override void UpdateState()
        {
            SnapToWall();
            UpdateWallMovement();
            _timeOnWall += Time.fixedDeltaTime;
        }

        public override void EndState()
        {
            base.EndState();
            _attachAllowedAt = Time.time + _wallReattachTime;
        }

        private void UpdatePhysics()
        {
            FindColliders(_wallChecker, _wallCheckRect, _wall);
            _currentWall = Overlaps.Count > 0 ? Overlaps[0] : null;
            _isGrounded = IsOnGround();
        }

        private bool IsOnGround()
        {
            if(_standCollider == null || !_standCollider.enabled)
                return false;

            var bounds = _standCollider.bounds;
            FindColliders(new Vector2(bounds.center.x, bounds.min.y),
                new Vector2(bounds.size.x * 0.6f, 0.13f), _ground);
            return Overlaps.Count > 0;
        }

        private void SnapToWall()
        {
            if(_standCollider == null || !_standCollider.enabled || _currentWall == null)
                return;

            var separation = Physics2D.Distance(_standCollider, _currentWall);
            if(!separation.isValid)
                return;

            Rigidbody.position += new Vector2(separation.pointB.x - separation.pointA.x, 0);
        }

        private void UpdateWallMovement()
        {
            Rigidbody.linearVelocityX = 0;

            if(_timeOnWall < _wallRunTime)
                Rigidbody.linearVelocityY = _wallRunSpeed;
            else if(_timeOnWall < _wallRunTime + _wallHoldTime)
                Rigidbody.linearVelocityY = 0;
            else
                Rigidbody.linearVelocityY = -_wallSlideSpeed;
        }

        private void StartWallJump()
        {
            var horizontalSpeed = _wallJumpHorizontalSpeed;
            var verticalSpeed = _wallJumpVerticalSpeed;

            if(Input.Horizontal == -_wallDirection)
                horizontalSpeed *= _wallJumpAwayMultiplier;
            else if(Input.Horizontal == _wallDirection)
            {
                horizontalSpeed *= _wallJumpTowardMultiplier;
                verticalSpeed *= _wallJumpTowardVerticalMultiplier;
            }

            var jumpDirection = -_wallDirection;
            Rigidbody.linearVelocity = new Vector2(jumpDirection * horizontalSpeed, verticalSpeed);

            if(Mathf.Sign(transform.forward.z) != jumpDirection)
                transform.Rotate(0, 180, 0);
        }

        private void OnDrawGizmosSelected() => DrawChecker(_wallChecker, _wallCheckRect, Color.yellow);

        private static void DrawChecker(Transform checker, Vector2 size, Color color)
        {
            if(checker == null)
                return;

            Gizmos.color = color;
            Gizmos.DrawWireCube(checker.position, size);
        }
    }
}
