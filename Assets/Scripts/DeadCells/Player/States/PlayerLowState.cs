using System;
using DeadCells.Player.AnimationControllers;
using UnityEngine;

namespace DeadCells.Player.States
{
    [DisallowMultipleComponent]
    public sealed class PlayerLowState : PlayerMovementState
    {
        private const float HeadCheckPadding = 0.03f;

        [Header("Colliders")]
        [SerializeField] private Collider2D _standCollider;
        [SerializeField] private Collider2D _lowCollider;

        [Header("Stand Check")]
        [SerializeField] private LayerMask _obstacles;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 4;
        [SerializeField] private float _crouchSpeedMultiplier = 0.5f;

        [Header("Roll")]
        [SerializeField] private float _rollSpeed = 12;

        private int _rollDirection;
        private bool _restoreColliderPending;
        private bool _canStand;

        public override PlayerState Id => PlayerState.Low;
        public override int ActivationPriority => 20;
        public override int AnimationValue => _rollDirection != 0 ? 1 : 0;

        public override void Initialize(PlayerInput input, Rigidbody2D rigidbody, AnimationController animationController)
        {
            base.Initialize(input, rigidbody, animationController);

            if(_standCollider == null || _lowCollider == null || _standCollider == _lowCollider ||
               _standCollider.attachedRigidbody != Rigidbody || _lowCollider.attachedRigidbody != Rigidbody ||
               _standCollider.isTrigger || _lowCollider.isTrigger)
                throw new InvalidOperationException("PlayerLowState: assign two different solid colliders on the player's Rigidbody2D.");

            _lowCollider.enabled = !_standCollider.enabled;
        }

        public override void EnterState()
        {
            base.EnterState();
            _restoreColliderPending = false;
            AnimationController.AnimationCompleted += OnAnimationEnded;
            _rollDirection = 0;
            UpdateCrouch(true);

            if(Input.ConsumeRoll())
                StartRoll();
            else
                MoveCrouch();
        }

        public override bool CanBeActivated(PlayerState? currentState)
        {
            if(currentState != Id)
            {
                if(_restoreColliderPending)
                {
                    UpdateStandCheck();
                    RestoreCollider();
                }

                return Input.RollBuffered ||
                       Input.Vertical < 0 && !Input.JumpBuffered;
            }

            UpdateStandCheck();
            return !Input.JumpBuffered &&
                   (_rollDirection != 0 || !_canStand || Input.Vertical < 0);
        }

        public override void UpdateState()
        {
            if(_rollDirection != 0)
            {
                Rigidbody.linearVelocityX = _rollDirection * _rollSpeed;
                return;
            }

            if(Input.ConsumeRoll())
            {
                StartRoll();
                return;
            }

            MoveCrouch();
        }

        public override void EndState()
        {
            base.EndState();
            AnimationController.AnimationCompleted -= OnAnimationEnded;

            if(_rollDirection != 0)
                Rigidbody.linearVelocityX = 0;

            _rollDirection = 0;

            if(_canStand || !gameObject.activeInHierarchy)
                UpdateCrouch(false);
            else
                _restoreColliderPending = true;
        }

        private void StartRoll()
        {
            _rollDirection = transform.forward.z > 0 ? 1 : -1;
            Rigidbody.linearVelocityX = _rollDirection * _rollSpeed;
            AnimationController.PlayAnimation(Id, 1, true);
        }

        private void MoveCrouch()
        {
            if(Input.Horizontal != 0 && Mathf.Sign(transform.forward.z) != Input.Horizontal)
                transform.Rotate(0, 180, 0);

            Rigidbody.linearVelocityX = Input.Horizontal * _moveSpeed * _crouchSpeedMultiplier;
        }

        private void OnAnimationEnded()
        {
            if(_rollDirection == 0)
                return;

            _rollDirection = 0;
            MoveCrouch();
        }

        private void RestoreCollider()
        {
            if(!_canStand)
                return;

            UpdateCrouch(false);
            _restoreColliderPending = false;
        }

        private void UpdateCrouch(bool crouch)
        {
            if(_lowCollider.enabled == crouch)
                return;

            _standCollider.enabled = !crouch;
            _lowCollider.enabled = crouch;
        }

        private void UpdateStandCheck()
        {
            GetHeadCheck(out var center, out var size);
            FindColliders(center, size, _obstacles);
            _canStand = Overlaps.Count == 0;
        }

        private void GetHeadCheck(out Vector2 center, out Vector2 size)
        {
            var standBounds = GetColliderBounds(_standCollider);
            var lowBounds = GetColliderBounds(_lowCollider);
            var bottom = lowBounds.max.y;
            var top = standBounds.max.y + HeadCheckPadding;
            var height = Mathf.Max(0.05f, top - bottom);
            center = new Vector2(standBounds.center.x, bottom + height * 0.5f);
            size = new Vector2(standBounds.size.x * 0.9f, height);
        }

        private static Bounds GetColliderBounds(Collider2D collider)
        {
            if(collider is CapsuleCollider2D capsule)
            {
                var lossy = capsule.transform.lossyScale;
                var center = (Vector2)capsule.transform.TransformPoint(capsule.offset);
                var size = new Vector2(Mathf.Abs(capsule.size.x * lossy.x), Mathf.Abs(capsule.size.y * lossy.y));
                return new Bounds(center, size);
            }

            return collider.bounds;
        }

        private void OnDrawGizmosSelected()
        {
            if(_standCollider == null || _lowCollider == null)
                return;

            GetHeadCheck(out var center, out var size);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
