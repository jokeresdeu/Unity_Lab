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
        [SerializeField] private Collider2D _standCollider;
        [SerializeField] private Collider2D _crouchCollider;
        [SerializeField] private Collider2D _rollCollider;

        [Header("Ground")]
        [SerializeField] private Transform _groundChecker;
        [SerializeField] private Vector2 _groundCheckRect = new(0.7f, 0.15f);
        [SerializeField] private LayerMask _ground;

        [Header("Stand")]
        [SerializeField] private Transform _standChecker;
        [SerializeField] private Vector2 _standCheckRect = new(0.7f, 0.7f);

        [Header("Roll")]
        [SerializeField] private KeyCode _rollKey = KeyCode.LeftShift;
        [SerializeField] private float _rollSpeed = 12;
        [SerializeField] private float _rollDuration = 0.28f;

        [Header("Jump")]
        [SerializeField] private float _jumpSpeed = 12;
        [SerializeField] private float _doubleJumpSpeed = 11;
        [SerializeField] private float _doubleJumpHorizontalSpeed = 7;
        [SerializeField] private float _doubleJumpDelay = 0.12f;
        [SerializeField] private float _doubleJumpWindow = 0.8f;

        private Rigidbody2D _rigidbody;
        private float _horizontal;
        private float _vertical;
        private bool _isCrouching;

        private bool _rollPressed;
        private bool _isRolling;
        private float _rollTimer;
        private float _rollDirection;

        private bool _jumpPressed;
        private bool _isGrounded;
        private bool _doubleJumpAvailable;
        private float _timeSinceJump;

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

            if(Input.GetKeyDown(_rollKey))
                _rollPressed = true;

            if(Input.GetButtonDown("Jump"))
                _jumpPressed = true;
        }

        private void FixedUpdate()
        {
            _isGrounded = IsGrounded() && _rigidbody.linearVelocityY <= 0;

            if(_isGrounded)
                _doubleJumpAvailable = false;
            else
                _timeSinceJump += Time.fixedDeltaTime;

            if(_rollPressed)
            {
                _rollPressed = false;

                if(_isGrounded && !_isRolling)
                    StartRoll();
            }

            if(_isRolling)
            {
                _jumpPressed = false;
                UpdateRoll();
                return;
            }

            if(_isGrounded)
                UpdateGround();
            else
                UpdateAir();

            _jumpPressed = false;
        }

        private void UpdateGround()
        {
            if(_jumpPressed)
            {
                if(!TryStand())
                {
                    _rigidbody.linearVelocityX = 0;
                    return;
                }

                StartJump();
                return;
            }

            if(_vertical < 0)
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
            if(_horizontal == 0)
            {
                _rigidbody.linearVelocityX = 0;
                return;
            }

            UpdateDirection(_horizontal);
            _rigidbody.linearVelocityX = _horizontal * _moveSpeed;
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

            if(!CanStand())
                return false;

            _isCrouching = false;
            _standCollider.enabled = true;
            _crouchCollider.enabled = false;
            _rollCollider.enabled = false;
            return true;
        }

        private bool CanStand() =>
            Physics2D.OverlapBox(_standChecker.position, _standCheckRect, 0, _ground) == null;

        private void StartJump()
        {
            _rigidbody.linearVelocityY = _jumpSpeed;
            _isGrounded = false;
            _doubleJumpAvailable = true;
            _timeSinceJump = 0;
        }

        private void UpdateAir()
        {
            if(!_standCollider.enabled)
                TryStand();

            UpdateAirMovement();

            if(!_jumpPressed || !CanDoubleJump())
                return;

            StartDoubleJump();
        }

        private void UpdateAirMovement()
        {
            if(_horizontal != 0)
                UpdateDirection(_horizontal);

            _rigidbody.linearVelocityX = _horizontal * _moveSpeed;
        }

        private bool CanDoubleJump() =>
            _doubleJumpAvailable && _timeSinceJump >= _doubleJumpDelay && _timeSinceJump <= _doubleJumpWindow;

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

        private bool IsGrounded() =>
            Physics2D.OverlapBox(_groundChecker.position, _groundCheckRect, 0, _ground) != null;

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
