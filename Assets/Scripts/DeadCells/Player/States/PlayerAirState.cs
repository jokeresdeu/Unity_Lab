using UnityEngine;

namespace DeadCells.Player.States
{
    [DisallowMultipleComponent]
    public sealed class PlayerAirState : PlayerMovementState
    {
        [Header("Air Movement")]
        [SerializeField] private float _airSpeed = 6;
        [SerializeField] private float _airAcceleration = 24;
        [SerializeField] private float _airTurnDeceleration = 30;

        [Header("Jump")]
        [SerializeField] private float _jumpSpeed = 12;
        [SerializeField] private float _doubleJumpSpeed = 11;
        [SerializeField] private float _doubleJumpHorizontalSpeed = 7;
        [SerializeField] private float _doubleJumpDelay = 0.12f;
        [SerializeField] private float _doubleJumpWindow = 0.8f;
        [SerializeField] private float _downJumpSpeed = 16;

        private bool _startJumpOnEnter;
        private bool _doubleJumpAvailable;
        private float _jumpStartedAt;

        public override PlayerState Id => PlayerState.Air;
        public override int ActivationPriority => 0;
        public override int AnimationValue => (int)Mathf.Clamp(Rigidbody.linearVelocityY, -1, 1);

        public override bool CanBeActivated(PlayerState? currentState)
        {
            if(currentState == Id)
                return true;

            ResetJump();
            _startJumpOnEnter = Input.JumpBuffered;
            return true;
        }

        public override void ResetState() => ResetJump();

        private void ResetJump()
        {
            _startJumpOnEnter = false;
            _doubleJumpAvailable = false;
        }

        public override void EnterState()
        {
            base.EnterState();

            if(!_startJumpOnEnter || !Input.ConsumeJump())
                return;

            Rigidbody.linearVelocityY = _jumpSpeed;
            _doubleJumpAvailable = true;
            _jumpStartedAt = Time.time;
        }

        public override void UpdateState()
        {
            UpdateAirMovement();

            if(!Input.JumpBuffered)
                return;

            if(Input.Vertical < 0)
            {
                Input.ConsumeJump();
                Rigidbody.linearVelocityY = -_downJumpSpeed;
                _doubleJumpAvailable = false;
                return;
            }

            var timeSinceJump = Time.time - _jumpStartedAt;

            if(_doubleJumpAvailable && timeSinceJump >= _doubleJumpDelay && timeSinceJump <= _doubleJumpWindow)
            {
                Input.ConsumeJump();
                StartDoubleJump();
            }
        }

        private void UpdateAirMovement()
        {
            var inputDirection = Input.Horizontal;
            var velocityX = Rigidbody.linearVelocityX;

            if(inputDirection == 0)
                return;

            if(Mathf.Sign(transform.forward.z) != inputDirection)
                transform.Rotate(0, 180, 0);

            if(Mathf.Abs(velocityX) < 0.01f || Mathf.Sign(velocityX) == inputDirection)
                velocityX = Mathf.MoveTowards(velocityX, inputDirection * _airSpeed, _airAcceleration * Time.fixedDeltaTime);
            else
                velocityX = Mathf.MoveTowards(velocityX, 0, _airTurnDeceleration * Time.fixedDeltaTime);

            Rigidbody.linearVelocityX = velocityX;
        }

        private void StartDoubleJump()
        {
            if(Input.Horizontal != 0)
            {
                if(Mathf.Sign(transform.forward.z) != Input.Horizontal)
                    transform.Rotate(0, 180, 0);

                Rigidbody.linearVelocityX = Input.Horizontal * _doubleJumpHorizontalSpeed;
            }

            Rigidbody.linearVelocityY = _doubleJumpSpeed;
            _doubleJumpAvailable = false;
            _jumpStartedAt = Time.time;
            AnimationController.PlayAnimation(Id, (int)Mathf.Clamp(Rigidbody.linearVelocityY, -1, 1), true);
        }
    }
}
