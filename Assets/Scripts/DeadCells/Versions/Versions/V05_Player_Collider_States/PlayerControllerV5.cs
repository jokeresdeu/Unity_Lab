using DeadCells.Versions.V05;
using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.Versions
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerControllerV5 : MonoBehaviour
    {
        [Header("Ground Movement")]
        [SerializeField] private float _moveSpeed = 7;
        [SerializeField] private float _faceDirection = 1;

        [Header("Colliders")]
        [SerializeField] private List<StateCollider> _stateColliders = new();

        [Header("Stand")]
        [SerializeField] private Transform _standChecker;
        [SerializeField] private Vector2 _standCheckRect = new(0.7f, 0.7f);
        [SerializeField] private LayerMask _ground;
        [Range(0.1f, 1)]
        [SerializeField] private float _crouchMoveModificator = 0.4f;

        [Header("Ground")]
        [SerializeField] private Transform _groundChecker;
        [SerializeField] private Vector2 _groundCheckRect = new(0.7f, 0.15f);

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

        [Header("Roll")]
        [SerializeField] private float _rollSpeed = 12;
        [SerializeField] private float _rollDuration = 0.28f;
        [SerializeField] private KeyCode _rollKey = KeyCode.LeftShift;

        private Rigidbody2D _rigidbody;

        private StateCollider _activeCollider;
        private ColliderState _colliderState;
        private PlayerState _state;
        private float _horizontal;
        private float _vertical;
        private bool _rollPressed;

        private bool _jumpPressed;
        private bool _isCrouching;

        private bool _doubleJumpAvailable;
        private float _timeSinceJump;

        private float _rollTimer;
        private float _rollDirection;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();

            SetCollider(ColliderState.Stand);
        }

        private void Start()
        {
            _state = (Physics2D.OverlapBox(_groundChecker.position, _groundCheckRect, 0, _ground) != null) && _rigidbody.linearVelocityY <= 0 ? PlayerState.Ground : PlayerState.Air;

            if(_state == PlayerState.Air)
                _timeSinceJump = _jumpStateTime + 1;
        }

        private void Update()
        {
            _horizontal = Input.GetAxisRaw("Horizontal");
            _vertical = Input.GetAxisRaw("Vertical");

            if(Input.GetButtonDown("Jump"))
                _jumpPressed = true;

            if(Input.GetKeyDown(_rollKey))
                _rollPressed = true;
        }

        private void FixedUpdate()
        {
            UpdateTimers();
            CheckStateTransitions();

            UpdateState();

            _rollPressed = false;
            _jumpPressed = false;
        }

        private void UpdateTimers()
        {
            if(_state == PlayerState.Air)
                _timeSinceJump += Time.fixedDeltaTime;
        }

        private void CheckStateTransitions()
        {
            var isGrounded = (Physics2D.OverlapBox(_groundChecker.position, _groundCheckRect, 0, _ground) != null) && _rigidbody.linearVelocityY <= 0;

            switch(_state)
            {
                case PlayerState.Roll:
                    return;

                case PlayerState.Air when isGrounded:
                    SetState(PlayerState.Ground);
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

        private void UpdateGround()
        {
            _doubleJumpAvailable = false;

            if(_rollPressed)
            {
                _rollPressed = false;
                StartRoll();
                return;
            }

            if(_jumpPressed)
            {
                if(!TryStand())
                    return;

                _jumpPressed = false;
                StartJump();
                return;
            }

            if(_vertical < 0)
                Crouch();
            else
                TryStand();

            MoveGround();
        }

        private void MoveGround()
        {
            if(_horizontal == 0)
            {
                _rigidbody.linearVelocityX = 0;
                return;
            }

            UpdateDirection(_horizontal);

            var moveSpeed = _isCrouching ? _moveSpeed * _crouchMoveModificator : _moveSpeed;
            _rigidbody.linearVelocityX = _horizontal * moveSpeed;
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

            if(Physics2D.OverlapBox(_standChecker.position, _standCheckRect, 0, _ground) != null)
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

            if(!_jumpPressed || !_doubleJumpAvailable ||
               _timeSinceJump < _doubleJumpDelay || _timeSinceJump > _doubleJumpWindow)
                return;

            _jumpPressed = false;
            StartDoubleJump();
        }

        private void UpdateAirMovement()
        {
            var inputDirection = _horizontal;
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
            if(_horizontal != 0)
            {
                UpdateDirection(_horizontal);
                _rigidbody.linearVelocityX = _horizontal * _doubleJumpHorizontalSpeed;
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
        }

        private void UpdateRoll()
        {
            _rigidbody.linearVelocityX = _rollDirection * _rollSpeed;
            _rollTimer -= Time.fixedDeltaTime;

            if(_rollTimer > 0)
                return;

            SetState((Physics2D.OverlapBox(_groundChecker.position, _groundCheckRect, 0, _ground) != null) && _rigidbody.linearVelocityY <= 0 ? PlayerState.Ground : PlayerState.Air);

            if(_state == PlayerState.Air)
                _timeSinceJump = _jumpStateTime + 1;
        }

        private void SetState(PlayerState state)
        {
            if(_state == state)
                return;

            _state = state;

            EnterState(_state);
        }

        private void EnterState(PlayerState state)
        {
            switch(state)
            {
                case PlayerState.Ground:
                    _doubleJumpAvailable = false;

                    if(!TryStand())
                        Crouch();
                    break;

                case PlayerState.Air:
                    TryStand();
                    break;

                case PlayerState.Roll:
                    _isCrouching = false;
                    SetCollider(ColliderState.Roll);
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
