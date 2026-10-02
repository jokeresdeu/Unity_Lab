using DeadCells.Versions.V09;
using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.Versions
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(EnvironmentNavigator))]
    public class PlayerControllerV9 : MonoBehaviour
    {
        [Header("Ground Movement")]
        [SerializeField] private float _moveSpeed = 7;
        [SerializeField] private float _faceDirection = 1;

        [Header("Colliders")]
        [SerializeField] private List<StateCollider> _stateColliders = new();

        [Header("Crouch")]
        [Range(0.1f, 1)]
        [SerializeField] private float _crouchMoveModificator = 0.4f;

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
        [SerializeField] private float _jumpStateTime = 0.25f;
        [SerializeField] private float _downJumpSpeed = 16;

        [Header("Ladder")]
        [SerializeField] private float _ladderSpeed = 5;
        [SerializeField] private float _ladderGroundExitDistance = 0.35f;

        [Header("Roll")]
        [SerializeField] private float _rollSpeed = 12;
        [SerializeField] private float _rollDuration = 0.28f;

        [Header("Wall")]
        [SerializeField] private float _wallRunSpeed = 5;
        [SerializeField] private float _wallRunTime = 0.35f;
        [SerializeField] private float _wallHoldTime = 0.2f;
        [SerializeField] private float _wallSlideSpeed = 2;

        private PlayerInput _input;
        private EnvironmentNavigator _environmentNavigator;
        private Rigidbody2D _rigidbody;

        private StateCollider _activeCollider;
        private ColliderState _colliderState;
        private PlayerState _state;
        private bool _isCrouching;

        private float _initialGravityScale;

        private bool _doubleJumpAvailable;
        private bool _canWallAttach;
        private float _timeSinceJump;

        private float _rollTimer;
        private float _rollDirection;

        private float _wallRunTimer;
        private float _wallHoldTimer;
        private float _wallDirection;

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
            _state = (_environmentNavigator.IsGrounded) && _rigidbody.linearVelocityY <= 0 ? PlayerState.Ground : PlayerState.Air;

            if(_state == PlayerState.Air)
                _timeSinceJump = _jumpStateTime + 1;
        }

        private void FixedUpdate()
        {
            UpdateTimers();
            CheckStateTransitions();

            if(_state == PlayerState.Roll)
                _input.ConsumeJump();

            if(_state == PlayerState.Wall)
            {
                _input.ConsumeRoll();
                _input.ConsumeJump();

                _rigidbody.linearVelocityX = 0;

                if(_wallRunTimer > 0)
                {
                    _wallRunTimer -= Time.fixedDeltaTime;
                    _rigidbody.linearVelocityY = _wallRunSpeed;
                    return;
                }

                if(_wallHoldTimer > 0)
                {
                    _wallHoldTimer -= Time.fixedDeltaTime;
                    _rigidbody.linearVelocityY = 0;
                    return;
                }

                _rigidbody.linearVelocityY = -_wallSlideSpeed;
                return;
            }

            UpdateState();

            _input.ConsumeRoll();
        }

        private void UpdateTimers()
        {
            if(_state == PlayerState.Air)
                _timeSinceJump += Time.fixedDeltaTime;
        }

        private void CheckStateTransitions()
        {
            var isGrounded = (_environmentNavigator.IsGrounded) && _rigidbody.linearVelocityY <= 0;

            switch(_state)
            {
                case PlayerState.Roll:
                    return;

                case PlayerState.Ladder when !(_environmentNavigator.HasLadder):
                    SetState(isGrounded ? PlayerState.Ground : PlayerState.Air);
                    return;

                case PlayerState.Ladder:
                    return;

                case PlayerState.Wall when isGrounded:
                    SetState(PlayerState.Ground);
                    return;

                case PlayerState.Wall when !_environmentNavigator.IsTouchingWall:
                    SetState(PlayerState.Air);
                    return;

                case PlayerState.Wall:
                    return;

                case PlayerState.Air when isGrounded:
                    SetState(PlayerState.Ground);
                    return;

                case PlayerState.Air when _environmentNavigator.HasLadder:
                    SetState(PlayerState.Ladder);
                    return;

                case PlayerState.Air when _canWallAttach && _environmentNavigator.IsTouchingWall:
                    SetState(PlayerState.Wall);
                    return;

                case PlayerState.Ground when _environmentNavigator.HasLadder && _input.Vertical > 0:
                    SetState(PlayerState.Ladder);
                    return;

                case PlayerState.Ground when !isGrounded:
                    _timeSinceJump = _jumpStateTime + 1;
                    SetState(PlayerState.Air);
                    return;
            }
        }

        private void UpdateState()
        {
            switch(_state)
            {
                case PlayerState.Ladder:
                    UpdateLadder();
                    break;

                case PlayerState.Ground:
                    UpdateGround();
                    break;

                case PlayerState.Air:
                    UpdateAir();
                    break;

                case PlayerState.Roll:
                    UpdateRoll();
                    break;
            }
        }

        private void UpdateLadder()
        {
            var groundBelow = _environmentNavigator.GroundBelow(_ladderGroundExitDistance);

            if(groundBelow && _input.Vertical <= 0)
            {
                SetState(PlayerState.Ground);
                return;
            }

            _rigidbody.linearVelocityX = 0;
            _rigidbody.linearVelocityY = _input.Vertical * _ladderSpeed;
        }

        private void UpdateGround()
        {
            _doubleJumpAvailable = false;
            _canWallAttach = false;

            if(_input.ConsumeRoll())
            {
                StartRoll();
                return;
            }

            if(_input.JumpBuffered)
            {
                if(!TryStand())
                    return;

                _input.ConsumeJump();
                StartJump();
                return;
            }

            if(_input.Vertical < 0)
                Crouch();
            else
                TryStand();

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

            var moveSpeed = _isCrouching ? _moveSpeed * _crouchMoveModificator : _moveSpeed;
            _rigidbody.linearVelocityX = _input.Horizontal * moveSpeed;
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
            _canWallAttach = true;
            _timeSinceJump = 0;

            SetState(PlayerState.Air);
        }

        private void UpdateAir()
        {
            if(_input.JumpBuffered && _input.Vertical < 0)
            {
                _input.ConsumeJump();
                StartDownJump();
                return;
            }

            if(_colliderState != ColliderState.Stand)
                TryStand();

            UpdateAirMovement();

            if(!_input.JumpBuffered || !_doubleJumpAvailable ||
               _timeSinceJump < _doubleJumpDelay || _timeSinceJump > _doubleJumpWindow)
                return;

            _input.ConsumeJump();
            StartDoubleJump();
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

        private void StartDoubleJump()
        {
            if(_input.Horizontal != 0)
            {
                UpdateDirection(_input.Horizontal);
                _rigidbody.linearVelocityX = _input.Horizontal * _doubleJumpHorizontalSpeed;
            }

            _rigidbody.linearVelocityY = _doubleJumpSpeed;

            _doubleJumpAvailable = false;
            _canWallAttach = true;
            _timeSinceJump = 0;
        }

        private void StartDownJump()
        {
            _rigidbody.linearVelocityY = -_downJumpSpeed;

            _doubleJumpAvailable = false;
            _canWallAttach = false;
            _timeSinceJump = _jumpStateTime + 1;
        }

        private void StartRoll()
        {
            _rollDirection = _faceDirection;
            _rollTimer = _rollDuration;

            SetState(PlayerState.Roll);
        }

        private void UpdateRoll()
        {
            _rigidbody.linearVelocityX = _rollDirection * _rollSpeed;
            _rollTimer -= Time.fixedDeltaTime;

            if(_rollTimer > 0)
                return;

            SetState((_environmentNavigator.IsGrounded) && _rigidbody.linearVelocityY <= 0 ? PlayerState.Ground : PlayerState.Air);

            if(_state == PlayerState.Air)
                _timeSinceJump = _jumpStateTime + 1;
        }

        private void SetState(PlayerState state)
        {
            if(_state == state)
                return;

            ExitState(_state);

            _state = state;

            EnterState(_state);
        }

        private void EnterState(PlayerState state)
        {
            switch(state)
            {
                case PlayerState.Wall:
                    TryStand();

                    _wallDirection = _faceDirection;
                    _wallRunTimer = _wallRunTime;
                    _wallHoldTimer = _wallHoldTime;

                    _rigidbody.gravityScale = 0;
                    _rigidbody.linearVelocity = Vector2.zero;
                    break;

                case PlayerState.Ladder:
                    TryStand();

                    _canWallAttach = false;
                    _rigidbody.gravityScale = 0;
                    _rigidbody.linearVelocity = Vector2.zero;
                    _rigidbody.position = new Vector2(_environmentNavigator.LadderCenterX, _rigidbody.position.y);
                    break;

                case PlayerState.Ground:
                    _rigidbody.gravityScale = _initialGravityScale;
                    _doubleJumpAvailable = false;
                    _canWallAttach = false;

                    if(!TryStand())
                        Crouch();
                    break;

                case PlayerState.Air:
                    _rigidbody.gravityScale = _initialGravityScale;
                    TryStand();
                    break;

                case PlayerState.Roll:
                    _isCrouching = false;
                    _canWallAttach = false;
                    SetCollider(ColliderState.Roll);
                    break;
            }
        }

        private void ExitState(PlayerState state)
        {
            switch(state)
            {
                case PlayerState.Ladder:
                    _rigidbody.gravityScale = _initialGravityScale;
                    break;

                case PlayerState.Wall:
                    _rigidbody.gravityScale = _initialGravityScale;
                    _canWallAttach = false;
                    break;
            }
        }

        private void SetCollider(ColliderState state)
        {
            if(_activeCollider != null && _colliderState == state)
                return;

            foreach(var stateCollider in _stateColliders)
            {
                if(stateCollider.SetActive(state))
                    _activeCollider = stateCollider;
            }

            _colliderState = state;
        }

        private void UpdateDirection(float direction)
        {
            if(direction == 0 || _faceDirection == Mathf.Sign(direction))
                return;

            _faceDirection = Mathf.Sign(direction);
            transform.Rotate(0, 180, 0);
        }

        private void OnDisable()
        {
            if(_rigidbody == null)
                return;

            _rigidbody.gravityScale = _initialGravityScale;
            _state = PlayerState.Air;
        }
    }
}
