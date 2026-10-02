using UnityEngine;

namespace TailsOfIron.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(InputReader))]
    public class PlayerMover : MonoBehaviour
    {
        [SerializeField] private PlayerAnimator _playerAnimator;
        [SerializeField] private float _horizontalSpeed;
        
        [Header("Roll")]
        [SerializeField] private float _rollImpulse;
        [SerializeField] private float _doubleRollImpulse;
        [SerializeField] private float _rollDuration;
        [SerializeField] private float _doubleRollDuration;
        [SerializeField] private float _rollDirectionLockTime = 0.5f;

        private Rigidbody2D _rigidbody;
        private InputReader _inputReader;
        
        private float _horizontalDirection;
        private float _faceDirection = 1;
        private float _doubleRollDirection;
        private float _rollTimer;
        private float _rollDirectionLockTimer;
        private bool _doubleRollQueued;
        private PlayerState _playerState;

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _inputReader = GetComponent<InputReader>();
           
            _inputReader.HorizontalDirectionChanged += HorizontalDirectionChanged;
            _inputReader.RollClicked += RollClicked;
        }

        private void FixedUpdate()
        {
            if (_playerState == PlayerState.Roll)
            {
                _rollTimer -= Time.fixedDeltaTime;
                _rollDirectionLockTimer -= Time.fixedDeltaTime;

                if (_rollDirectionLockTimer <= 0 && _doubleRollQueued)
                {
                    StartDoubleRoll();
                    return;
                }

                if (_rollTimer <= 0)
                    UpdateMoveState();

                return;
            }

            if (_playerState == PlayerState.DoubleRoll)
            {
                _rollTimer -= Time.fixedDeltaTime;

                if (_rollTimer <= 0)
                    UpdateMoveState();

                return;
            }

            Move();
        }

        private void Move()
        {
            _rigidbody.linearVelocityX = _horizontalDirection * _horizontalSpeed;
            
            if (_horizontalDirection != 0)
                UpdateDirection(_horizontalDirection);

            UpdateMoveState();
        }

        private void HorizontalDirectionChanged(float horizontalDirection)
        {
            _horizontalDirection = horizontalDirection;
        }

        private void RollClicked()
        {
            if (_playerState == PlayerState.DoubleRoll)
                return;

            if (_playerState == PlayerState.Roll)
            {
                _doubleRollDirection = _horizontalDirection != 0
                    ? _horizontalDirection
                    : _faceDirection;

                if (_rollDirectionLockTimer > 0)
                {
                    _doubleRollQueued = true;
                    return;
                }

                StartDoubleRoll();
                return;
            }

            var rollDirection = _horizontalDirection != 0
                ? _horizontalDirection
                : _faceDirection;

            UpdateDirection(rollDirection);
            SetState(PlayerState.Roll);

            _rollTimer = _rollDuration;
            _rollDirectionLockTimer = _rollDirectionLockTime;

            _rigidbody.linearVelocityX = 0;
            _rigidbody.AddForce(
                Vector2.right * rollDirection * _rollImpulse,
                ForceMode2D.Impulse);
        }

        private void StartDoubleRoll()
        {
            _doubleRollQueued = false;

            UpdateDirection(_doubleRollDirection);
            SetState(PlayerState.DoubleRoll);

            _rollTimer = _doubleRollDuration;

            _rigidbody.linearVelocityX = 0;
            _rigidbody.AddForce(
                Vector2.right * _doubleRollDirection * _doubleRollImpulse,
                ForceMode2D.Impulse);
        }

        private void UpdateMoveState()
        {
            _doubleRollQueued = false;
            
            var playerState = _horizontalDirection == 0
                ? PlayerState.Idle
                : PlayerState.Move;
            
            SetState(playerState);
        }

        private void UpdateDirection(float direction)
        {
            _faceDirection = direction;
            var angle = direction < 0 ? 180 : 0;
            transform.rotation = Quaternion.Euler(0, angle, 0);
        }

        private void SetState(PlayerState playerState)
        {
            _playerState = playerState;
            _playerAnimator.SetState(playerState);
        }

        private void OnDestroy()
        {
            _inputReader.HorizontalDirectionChanged -= HorizontalDirectionChanged;
            _inputReader.RollClicked -= RollClicked;
        }
    }
}