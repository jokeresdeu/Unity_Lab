using System;
using System.Collections.Generic;
using UnityEngine;

namespace Owlboy
{
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public class Player : MonoBehaviour
    {
        [SerializeField] private float _horizontalSpeed;
    
        [Header("Flying")]
        [SerializeField] private float _verticalSpeed;
        [SerializeField] private float _flyAcceleration;
        [SerializeField] private float _flyDeceleration;

        [Header("Jump")] 
        [SerializeField] private float _jumpForce;
        [SerializeField] private Vector2 _groundCheckRect;
        [SerializeField] private Transform _groundChecker;
        [SerializeField] private LayerMask _ground;
    
        [Header("Crouch")] 
        [SerializeField] private Vector2 _cellCheckRect;
        [SerializeField] private Transform _cellChecker;
        [SerializeField, Range(0.1f, 1)] private float _crouchModificator;
        [SerializeField] private GameObject _crouchCollider;
        [SerializeField] private GameObject _casualCollider;
        [SerializeField] private GameObject _flyCollider;

        [SerializeField] private List<PlayerStateSprite> _playerStateSprites;

        private Rigidbody2D _rigidbody;
        private SpriteRenderer _spriteRenderer;

        private float _horizontalDirection;
        private bool _jump;
        private float _verticalDirection;
        private bool _isFlying;
        private bool _faceRight;
        private bool _isGrounded;
        private bool _canStand;
        private bool _crouch;
        private PlayerState _currentState;
        private float _gravityScale;

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _gravityScale = _rigidbody.gravityScale;
        }

        private void Update()
        {
            _horizontalDirection = Input.GetAxisRaw("Horizontal");
            _verticalDirection = Input.GetAxisRaw("Vertical");

            if (Input.GetButtonDown("Jump"))
                _jump = true;
        
            _crouch = Input.GetButton("Crouch");
        }

        private void FixedUpdate()
        {
            var currentState = PlayerState.Idle;
        
            _isGrounded =  Physics2D.OverlapBox(_groundChecker.position, _groundCheckRect, 0, _ground) != null;
        
            if (_isGrounded)
            {
                _isFlying = false;
                Move(ref currentState);
            }
            else
            {
                if(_jump)
                    _isFlying = true;
            
                if(_isFlying)
                    Fly(ref currentState);
            }

            _jump = false;
            ChangeDirectionForward();
            UpdatePlayerState(currentState);
        }

        private void Move(ref PlayerState currentState)
        {
            if (_flyCollider.activeInHierarchy)
            {
                _flyCollider.SetActive(false);
                _casualCollider.SetActive(true);
            }
        
            _rigidbody.gravityScale = _gravityScale;
        
            if (_jump)
            {
                _rigidbody.linearVelocityY = _jumpForce;
                currentState = PlayerState.Jump;
            }
            else
            {
                _rigidbody.linearVelocityX = _horizontalDirection * _horizontalSpeed;
                if (_horizontalDirection != 0)
                    currentState = PlayerState.Run;
            }

            var currentCrouch = _crouchCollider.activeInHierarchy;
            if (currentCrouch != _crouch)
            {
                if(_crouch)
                    Crouch(true);
                else
                {
                    var canStand = Physics2D.OverlapBox(_cellChecker.position, _cellCheckRect, 0, _ground) == null;
                    Crouch(!canStand);
                }
            }
        
            if (_crouchCollider.activeInHierarchy)
                currentState = PlayerState.Crouch;
        }

        private void Fly(ref PlayerState currentState)
        {
            _rigidbody.gravityScale = 0;
        
            _flyCollider.SetActive(true);
            _crouchCollider.SetActive(false);
            _casualCollider.SetActive(false);

            var targetVelocity = new Vector2(
                _horizontalDirection * _horizontalSpeed,
                _verticalDirection * _verticalSpeed);

            var acceleration = _horizontalDirection == 0 && _verticalDirection == 0
                ? _flyDeceleration
                : _flyAcceleration;

            _rigidbody.linearVelocity = Vector2.MoveTowards(
                _rigidbody.linearVelocity,
                targetVelocity,
                acceleration * Time.fixedDeltaTime);

            currentState = PlayerState.Fly;
        }

        private void ChangeDirectionForward()
        {
            if (_horizontalDirection == 0) 
                return;
        
            var angle = _horizontalDirection < 0 ? 180 : 0;
            if (_isFlying)
                angle += 180;
            
            transform.rotation = Quaternion.Euler(0, angle, 0);
        }

        private void Crouch(bool crouch)
        {
            _crouchCollider.SetActive(crouch);
            _casualCollider.SetActive(!crouch);
        }

        private void UpdatePlayerState(PlayerState newState)
        {
            if(_currentState == newState)
                return;

            var stateSprite = _playerStateSprites.Find(element => element.State == newState).Sprite;
            _spriteRenderer.sprite = stateSprite;
            _currentState = newState;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_groundChecker.position, _groundCheckRect);
            Gizmos.DrawWireCube(_cellChecker.position, _cellCheckRect);
        }

        [Serializable]
        public class PlayerStateSprite
        {
            [field: SerializeField] public Sprite Sprite { get; private set; }
            [field: SerializeField] public PlayerState State { get; private set; }
        }

        public enum PlayerState
        {
            Idle = 0,
            Run = 1,
            Jump = 2,
            Crouch = 3,
            Fly = 4
        }
    }
}