using UnityEngine;

namespace DeadCells.Versions
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerControllerV3 : MonoBehaviour
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

        [Header("Roll")]
        [SerializeField] private float _rollSpeed = 12;
        [SerializeField] private float _rollDuration = 0.28f;
        [SerializeField] private KeyCode _rollKey = KeyCode.LeftShift;

        private Rigidbody2D _rigidbody;
        private float _horizontal;
        private float _vertical;
        private bool _rollPressed;
        private bool _isRolling;
        private bool _isCrouching;

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

            if(Input.GetKeyDown(_rollKey))
                _rollPressed = true;
        }

        private void FixedUpdate()
        {
            if(_isRolling)
            {
                _rollPressed = false;
                UpdateRoll();
                return;
            }

            if(_rollPressed)
            {
                _rollPressed = false;
                StartRoll();
                UpdateRoll();
                return;
            }

            if(_vertical < 0)
                Crouch();
            else
                TryStand();

            MoveGround();
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

        private void UpdateDirection(float direction)
        {
            if(direction == 0 || _faceDirection == Mathf.Sign(direction))
                return;

            _faceDirection = Mathf.Sign(direction);
            transform.Rotate(0, 180, 0);
        }

        private void OnDrawGizmos()
        {
            if(_standChecker != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(_standChecker.position, _standCheckRect);
            }
        }
    }
}
