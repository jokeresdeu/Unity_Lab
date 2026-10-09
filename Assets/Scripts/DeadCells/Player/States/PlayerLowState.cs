using System;
using DeadCells.Player.AnimationControllers;
using UnityEngine;

namespace DeadCells.Player.States
{
    [DisallowMultipleComponent]
    public sealed class PlayerLowState : PlayerMovementState
    {
        [Header("Colliders")]
        [SerializeField] private Collider2D _standCollider;
        [SerializeField] private Collider2D _lowCollider;

        [Header("Stand Check")]
        [SerializeField] private Transform _standChecker;
        [SerializeField] private Vector2 _standCheckRect = new (0.7f, 0.8f);
        [SerializeField] private LayerMask _obstacles;

        [Header("Roll")]
        [SerializeField] private float _rollSpeed = 12;

        private int _rollDirection;
        private bool _restoreColliderPending;

        public override PlayerState Id => PlayerState.Low;
        public override int ActivationPriority => 20;
        public override int AnimationValue => _rollDirection != 0 ? 1 : 0;
        
        private bool _canStand;

        public override void Initialize(PlayerInput input, Rigidbody2D rigidbody, AnimationController animationController)
        {
            base.Initialize(input, rigidbody, animationController);

            if(_standChecker == null)
                throw new InvalidOperationException("PlayerLowState: assign Stand Checker.");

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
            Rigidbody.linearVelocityX = 0;

            if(Input.ConsumeRoll())
                StartRoll();
        }

        public override bool CanBeActivated(PlayerState? currentState)
        {
            FindColliders(_standChecker, _standCheckRect, _obstacles);
            _canStand = Overlaps.Count == 0;

            if(_restoreColliderPending)
                RestoreCollider();

            if(currentState != Id)
                return Input.RollBuffered || !_canStand ||
                       Input.Vertical < 0 && !Input.JumpBuffered;

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

            Rigidbody.linearVelocityX = 0;
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

        private void OnAnimationEnded()
        {
            if(_rollDirection == 0)
                return;

            _rollDirection = 0;
            Rigidbody.linearVelocityX = 0;
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

        private void OnDrawGizmosSelected()
        {
            if(_standChecker == null)
                return;

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(_standChecker.position, _standCheckRect);
        }
    }
}
