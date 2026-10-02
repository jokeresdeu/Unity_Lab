using UnityEngine;

namespace DeadCells.Versions
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerControllerV4 : MonoBehaviour
    {
        [Header("Ground Movement")]
        [SerializeField] private float _moveSpeed = 7;
        [SerializeField] private float _faceDirection = 1;

        [Header("Colliders")]
        [SerializeField] private Collider2D _standCollider;
        [SerializeField] private Collider2D _crouchCollider;
        [SerializeField] private Collider2D _rollCollider;

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

        [Header("Roll")]
        [SerializeField] private float _rollSpeed = 12;
        [SerializeField] private float _rollDuration = 0.28f;
        [SerializeField] private KeyCode _rollKey = KeyCode.LeftShift;

        private Rigidbody2D _rigidbody;
        private float _horizontal;
        private float _vertical;
        private bool _rollPressed;
        private bool _isRolling;
        private bool _jumpPressed;
        private bool _isGrounded;
        private bool _isCrouching;

        private bool _doubleJumpAvailable;
        private float _timeSinceJump;

        private float _rollTimer;
        private float _rollDirection;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();

            _standCollider.enabled = true;
            _crouchCollider.enabled = false;
            _rollCollider.enabled = false;
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
            _isGrounded = (Physics2D.OverlapBox(_groundChecker.position, _groundCheckRect, 0, _ground) != null) && _rigidbody.linearVelocityY <= 0;

            if(!_isGrounded)
                _timeSinceJump += Time.fixedDeltaTime;

            if(_isRolling)
            {
                UpdateRoll();
                _rollPressed = false;
                _jumpPressed = false;
                return;
            }

            if(_isGrounded)
                UpdateGround();
            else
                UpdateAir();

            _rollPressed = false;
            _jumpPressed = false;
        }

        private void StartJump()
        {
            _rigidbody.linearVelocityY = _jumpSpeed;

            _doubleJumpAvailable = true;
            _timeSinceJump = 0;

            _isGrounded = false;
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

        private void UpdateAir()
        {
            if(!_standCollider.enabled)
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
            _standCollider.enabled = false;
            _crouchCollider.enabled = true;
            _rollCollider.enabled = false;
        }

        private bool TryStand()
        {
            if(_standCollider.enabled)
            {
                _isCrouching = false;
                return true;
            }

            if(!(Physics2D.OverlapBox(_standChecker.position, _standCheckRect, 0, _ground) == null))
                return false;

            _isCrouching = false;
            _standCollider.enabled = true;
            _crouchCollider.enabled = false;
            _rollCollider.enabled = false;
            return true;
        }

        private void StartRoll()
        {
            _rollDirection = _faceDirection;
            _rollTimer = _rollDuration;

            _isRolling = true;
            _isCrouching = false;

            _standCollider.enabled = false;
            _crouchCollider.enabled = false;
            _rollCollider.enabled = true;
        }

        private void UpdateRoll()
        {
            _rigidbody.linearVelocityX = _rollDirection * _rollSpeed;
            _rollTimer -= Time.fixedDeltaTime;

            if(_rollTimer > 0)
                return;

            _isRolling = false;

            if(!TryStand())
                Crouch();
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
