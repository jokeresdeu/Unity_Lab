using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.Versions.Versions.V14_EnvironmentNavigator
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(EnvironmentNavigator))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Ground Movement")]
        [SerializeField] private float _moveSpeed = 7;
        [SerializeField] private float _faceDirection = 1;

        [Header("Roll")]
        [SerializeField] private float _rollSpeed = 12;
        [SerializeField] private float _rollDuration = 0.28f;

        [Header("Jump")]
        [SerializeField] private float _jumpSpeed = 12;
        [SerializeField] private float _doubleJumpSpeed = 11;
        [SerializeField] private float _doubleJumpHorizontalSpeed = 7;
        [SerializeField] private float _doubleJumpDelay = 0.12f;
        [SerializeField] private float _doubleJumpWindow = 0.8f;
        [SerializeField] private float _downJumpSpeed = 16;

        [Header("Colliders")]
        [SerializeField] private List<StateCollider> _stateColliders = new();

        [Header("Air Movement")]
        [SerializeField] private float _airSpeed = 6;
        [SerializeField] private float _airAcceleration = 24;
        [SerializeField] private float _airTurnDeceleration = 30;

        [Header("One Way Platform")]
        [SerializeField] private float _dropThroughTime = 0.5f;

        [Header("Ladder")]
        [SerializeField] private float _ladderSpeed = 5;
        [SerializeField] private float _ladderGroundExitDistance = 0.35f;

        private PlayerInput _input;
        private EnvironmentNavigator _environmentNavigator;
        private Rigidbody2D _rigidbody;
        private bool _isCrouching;

        private float _rollTimer;
        private float _rollDirection;

        private bool _doubleJumpAvailable;
        private float _timeSinceJump;

        private PlayerState _state;

        private ColliderState _colliderState;

        private float _initialGravityScale;

        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            _environmentNavigator = GetComponent<EnvironmentNavigator>();
            _rigidbody = GetComponent<Rigidbody2D>();
            _initialGravityScale = _rigidbody.gravityScale;

            SetCollider(ColliderState.Stand);
        }

        private void Start()
        {
            _state = IsGrounded() ? PlayerState.Ground : PlayerState.Air;
        }

        private void FixedUpdate()
        {
            UpdateTimers();
            CheckStateTransitions();

            if(_input.ConsumeRoll() && _state == PlayerState.Ground)
                StartRoll();

            if(_state == PlayerState.Roll)
                _input.ConsumeJump();

            UpdateState();
        }

        private void UpdateGround()
        {
            _doubleJumpAvailable = false;

            if(_input.JumpBuffered && _input.Vertical < 0 && _environmentNavigator.IsOnOneWayPlatform)
            {
                _input.ConsumeJump();
                _environmentNavigator.DropThroughOneWay(_dropThroughTime);
                SetState(PlayerState.Air);
                return;
            }

            if(_input.JumpBuffered)
            {
                if(!TryStand())
                {
                    _rigidbody.linearVelocityX = 0;
                    return;
                }

                _input.ConsumeJump();
                StartJump();
                return;
            }

            if(_input.Vertical < 0)
                Crouch();
            else
                TryStand();

            if(_isCrouching)
            {
                _rigidbody.linearVelocityX = 0;
                return;
            }

            MoveGround();
        }

        private void MoveGround()
        {
            if(_input.Horizontal == 0)
            {
                _rigidbody.linearVelocityX = 0;
                return;
            }

            UpdateDirection(_input.Horizontal);
            _rigidbody.linearVelocityX = _input.Horizontal * _moveSpeed;
        }

        private void Crouch()
        {
            _isCrouching = true;
            SetCollider(ColliderState.Crouch);
        }

        private bool TryStand()
        {
            if(_colliderState == ColliderState.Stand)
            {
                _isCrouching = false;
                return true;
            }

            if(!_environmentNavigator.CanStand())
                return false;

            _isCrouching = false;
            SetCollider(ColliderState.Stand);
            return true;
        }

        private void StartJump()
        {
            _rigidbody.linearVelocityY = _jumpSpeed;
            _doubleJumpAvailable = true;
            _timeSinceJump = 0;
            SetState(PlayerState.Air);
        }

        private void UpdateAir()
        {
            if(_colliderState != ColliderState.Stand)
                TryStand();

            UpdateAirMovement();

            if(!_input.JumpBuffered)
                return;

            if(_input.Vertical < 0)
            {
                _input.ConsumeJump();
                StartDownJump();
                return;
            }

            if(CanDoubleJump())
            {
                _input.ConsumeJump();
                StartDoubleJump();
            }
        }

        private void UpdateAirMovement()
        {
            var inputDirection = _input.Horizontal;
            var velocityX = _rigidbody.linearVelocityX;

            if(inputDirection == 0)
                return;

            UpdateDirection(inputDirection);

            var velocityDirection = Mathf.Sign(velocityX);

            if(Mathf.Abs(velocityX) < 0.01f || velocityDirection == Mathf.Sign(inputDirection))
                velocityX = Mathf.MoveTowards(velocityX, inputDirection * _airSpeed, _airAcceleration * Time.fixedDeltaTime);
            else
                velocityX = Mathf.MoveTowards(velocityX, 0, _airTurnDeceleration * Time.fixedDeltaTime);

            _rigidbody.linearVelocityX = velocityX;
        }

        private bool CanDoubleJump() =>
            _doubleJumpAvailable && _timeSinceJump >= _doubleJumpDelay && _timeSinceJump <= _doubleJumpWindow;

        private void StartDoubleJump()
        {
            if(_input.Horizontal != 0)
            {
                UpdateDirection(_input.Horizontal);
                _rigidbody.linearVelocityX = _input.Horizontal * _doubleJumpHorizontalSpeed;
            }

            _rigidbody.linearVelocityY = _doubleJumpSpeed;
            _doubleJumpAvailable = false;
            _timeSinceJump = 0;
        }

        private void StartRoll()
        {
            _rollDirection = _faceDirection;
            _rollTimer = _rollDuration;
            SetState(PlayerState.Roll);
            _isCrouching = false;

            SetCollider(ColliderState.Roll);
        }

        private void UpdateRoll()
        {
            _rigidbody.linearVelocityX = _rollDirection * _rollSpeed;
            _rollTimer -= Time.fixedDeltaTime;

            if(_rollTimer > 0)
                return;

            SetState(IsGrounded() ? PlayerState.Ground : PlayerState.Air);

            if(!TryStand())
                Crouch();
        }

        private bool IsGrounded() =>
            _environmentNavigator.IsGrounded && _rigidbody.linearVelocityY <= 0;

        private void UpdateTimers()
        {
            if(_state == PlayerState.Air)
                _timeSinceJump += Time.fixedDeltaTime;
        }

        private void CheckStateTransitions()
        {
            switch(_state)
            {
                case PlayerState.Roll:
                    return;

                case PlayerState.Ladder when !_environmentNavigator.HasLadder:
                    ResetGravity();
                    SetState(IsGrounded() ? PlayerState.Ground : PlayerState.Air);
                    return;

                case PlayerState.Ladder:
                    return;

                case PlayerState.Air when _environmentNavigator.HasLadder:
                    StartLadder();
                    return;

                case PlayerState.Air when IsGrounded():
                    SetState(PlayerState.Ground);
                    return;

                case PlayerState.Ground when _environmentNavigator.HasLadder && _input.Vertical > 0:
                    StartLadder();
                    return;

                case PlayerState.Ground when !IsGrounded():
                    SetState(PlayerState.Air);
                    return;
            }
        }

        private void UpdateState()
        {
            switch(_state)
            {
                case PlayerState.Ground:
                    UpdateGround();
                    break;

                case PlayerState.Air:
                    UpdateAir();
                    break;

                case PlayerState.Roll:
                    UpdateRoll();
                    break;

                case PlayerState.Ladder:
                    UpdateLadder();
                    break;
            }
        }

        private void SetState(PlayerState state)
        {
            _state = state;

            if(_state == PlayerState.Ground)
                _doubleJumpAvailable = false;
        }

        private void SetCollider(ColliderState state)
        {
            foreach(var stateCollider in _stateColliders)
                stateCollider.SetActive(state);

            _colliderState = state;
        }

        private void StartDownJump()
        {
            _rigidbody.linearVelocityY = -_downJumpSpeed;
            _doubleJumpAvailable = false;
        }

        private void StartLadder()
        {
            TryStand();
            _rigidbody.gravityScale = 0;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.position = new Vector2(_environmentNavigator.LadderCenterX, _rigidbody.position.y);
            SetState(PlayerState.Ladder);
        }

        private void UpdateLadder()
        {
            if(_environmentNavigator.GroundBelow(_ladderGroundExitDistance) && _input.Vertical <= 0)
            {
                ResetGravity();
                SetState(PlayerState.Ground);
                return;
            }

            _rigidbody.linearVelocityX = 0;
            _rigidbody.linearVelocityY = _input.Vertical * _ladderSpeed;
        }

        private void ResetGravity() => _rigidbody.gravityScale = _initialGravityScale;

        private void UpdateDirection(float direction)
        {
            if(direction == 0 || _faceDirection == Mathf.Sign(direction))
                return;

            _faceDirection = Mathf.Sign(direction);
            transform.Rotate(0, 180, 0);
        }

    }
}
