using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeadCells.Player.States
{
    [DisallowMultipleComponent]
    public sealed class PlayerGroundState : PlayerMovementState
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 4;

        [Header("Ground")]
        [SerializeField] private Transform _groundChecker;
        [SerializeField] private Vector2 _groundCheckRect = new Vector2(0.7f, 0.15f);
        [SerializeField] private LayerMask _ground;
        [SerializeField] private LayerMask _oneWayPlatform;

        [Header("One Way Platform")]
        [SerializeField] private float _dropThroughTime = 0.5f;
        [SerializeField] private float _dropThroughSpeed = 2;

        private readonly List<DroppedPlatform> _droppedPlatforms = new();
        private Collider2D _currentGround;

        public override PlayerState Id => PlayerState.Ground;
        public override int ActivationPriority => 10;
        public override int AnimationValue => Input.Horizontal;
        private bool IsGrounded => _currentGround != null;

        private bool IsOnOneWayPlatform => _currentGround != null &&
                                           (_oneWayPlatform.value & (1 << _currentGround.gameObject.layer)) != 0;
        private bool CanDropThrough => Input.JumpBuffered && Input.Vertical < 0 &&
                                       IsOnOneWayPlatform && GetCurrentEffector() != null;
        
        public override bool CanBeActivated(PlayerState? currentState)
        {
            UpdatePhysics();

            if(currentState == Id && CanDropThrough)
            {
                DropThroughPlatform();
                return false;
            }

            return IsGrounded && Rigidbody.linearVelocityY <= 0 &&
                   !Input.JumpBuffered && !Input.RollBuffered && Input.Vertical >= 0;
        }

        public override void ResetState() =>  RestoreDroppedPlatforms();

        public override void EnterState()
        {
            base.EnterState();
            MoveGround();
        }

        public override void UpdateState() =>  MoveGround();

        private void DropThroughPlatform()
        {
            if(!CanDropThrough)
                return;

            var effector = GetCurrentEffector();

            Input.ConsumeJump();

            AddDroppedPlatform(effector, effector.rotationalOffset, Time.time + _dropThroughTime);

            effector.rotationalOffset += 180;
            _currentGround = null;
            Rigidbody.linearVelocityY = Mathf.Min(Rigidbody.linearVelocityY, -_dropThroughSpeed);
        }

        private void RestoreDroppedPlatforms()
        {
            foreach(var platform in _droppedPlatforms)
                RestorePlatform(platform);

            _droppedPlatforms.Clear();
        }

        private void UpdateGround()
        {
            FindColliders(_groundChecker.position, _groundCheckRect, _ground.value | _oneWayPlatform.value, true);
            _currentGround = null;

            foreach(var otherCollider in Overlaps)
            {
                if(IsDropped(otherCollider))
                    continue;

                if(otherCollider.isTrigger && !IsStandingOnTop(otherCollider))
                    continue;

                if(_currentGround == null || otherCollider.bounds.max.y > _currentGround.bounds.max.y)
                    _currentGround = otherCollider;
            }
        }

        private bool IsStandingOnTop(Collider2D otherCollider)
        {
            var top = otherCollider.bounds.max.y;
            var checkerMin = _groundChecker.position.y - _groundCheckRect.y * 0.5f;
            var checkerMax = _groundChecker.position.y + _groundCheckRect.y * 0.5f;
            return top >= checkerMin && top <= checkerMax + 0.05f;
        }

        private void UpdatePhysics()
        {
            UpdateDroppedPlatforms();
            UpdateGround();
        }

        private void AddDroppedPlatform(PlatformEffector2D effector, float rotationalOffset, float until)
        {
            _droppedPlatforms.Add(new DroppedPlatform
            {
                Effector = effector,
                RotationalOffset = rotationalOffset,
                Until = until
            });
        }

        private void MoveGround()
        {
            if(Input.Horizontal != 0 && Mathf.Sign(transform.forward.z) != Input.Horizontal)
                transform.Rotate(0, 180, 0);

            Rigidbody.linearVelocityX = Input.Horizontal * _moveSpeed;
        }

        private void UpdateDroppedPlatforms()
        {
            for(var i = _droppedPlatforms.Count - 1; i >= 0; i--)
            {
                var platform = _droppedPlatforms[i];

                if(platform.Effector != null && Time.time < platform.Until)
                    continue;

                RestorePlatform(platform);
                _droppedPlatforms.RemoveAt(i);
            }
        }

        private static void RestorePlatform(DroppedPlatform platform)
        {
            if(platform.Effector != null)
                platform.Effector.rotationalOffset = platform.RotationalOffset;
        }

        private PlatformEffector2D GetCurrentEffector() => _currentGround != null ? 
            _currentGround.GetComponent<PlatformEffector2D>() : null;

        private bool IsDropped(Collider2D groundCollider)
        {
            var effector = groundCollider.GetComponent<PlatformEffector2D>();
            return _droppedPlatforms.Any(platform => platform.Effector == effector);
        }

        private void OnDrawGizmosSelected()
        {
            if(_groundChecker == null)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_groundChecker.position, _groundCheckRect);
        }

        private sealed class DroppedPlatform
        {
            public PlatformEffector2D Effector;
            public float Until;
            public float RotationalOffset;
        }
    }
}
