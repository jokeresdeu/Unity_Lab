using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.Versions.Versions.V09_AirMovement
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Ground Movement")]
        [SerializeField] private float _moveSpeed = 7;
        [SerializeField] private float _faceDirection = 1;

        [Header("Ground")]
        [SerializeField] private Transform _groundChecker;
        [SerializeField] private Vector2 _groundCheckRect = new(0.7f, 0.15f);
        [SerializeField] private LayerMask _ground;

        [Header("Stand")]
        [SerializeField] private Transform _standChecker;
        [SerializeField] private Vector2 _standCheckRect = new(0.7f, 0.7f);

        [Header("Roll")]
        [SerializeField] private float _rollSpeed = 12;
        [SerializeField] private float _rollDuration = 0.28f;

        [Header("Jump")]
        [SerializeField] private float _jumpSpeed = 12;
        [SerializeField] private float _doubleJumpSpeed = 11;
        [SerializeField] private float _doubleJumpHorizontalSpeed = 7;
        [SerializeField] private float _doubleJumpDelay = 0.12f;
        [SerializeField] private float _doubleJumpWindow = 0.8f;

        [Header("Colliders")]
        [SerializeField] private List<StateCollider> _stateColliders = new();

        [Header("Air Movement")]
        [SerializeField] private float _airSpeed = 6;
        [SerializeField] private float _airAcceleration = 24;
        [SerializeField] private float _airTurnDeceleration = 30;

        private PlayerInput _input;
        private Rigidbody2D _rigidbody;
        private bool _isCrouching;

        private float _rollTimer;
        private float _rollDirection;

        private bool _doubleJumpAvailable;
        private float _timeSinceJump;

        private PlayerState _state;

        private ColliderState _colliderState;

        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            _rigidbody = GetComponent<Rigidbody2D>();

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

            UpdateState();
            _input.ConsumeJump();
        }

        private void UpdateGround()
        {
            _doubleJumpAvailable = false;

            if(_input.JumpPressed)
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

            if(!CanStand())
                return false;

            _isCrouching = false;
            SetCollider(ColliderState.Stand);
            return true;
        }

        private bool CanStand() =>
            Physics2D.OverlapBox(_standChecker.position, _standCheckRect, 0, _ground) == null;

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

            if(!_input.JumpPressed || !CanDoubleJump())
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
            Physics2D.OverlapBox(_groundChecker.position, _groundCheckRect, 0, _ground) != null && _rigidbody.linearVelocityY <= 0;

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

                case PlayerState.Air when IsGrounded():
                    SetState(PlayerState.Ground);
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

        private void UpdateDirection(float direction)
        {
            if(direction == 0 || _faceDirection == Mathf.Sign(direction))
                return;

            _faceDirection = Mathf.Sign(direction);
            transform.Rotate(0, 180, 0);
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
        }
    }
}
