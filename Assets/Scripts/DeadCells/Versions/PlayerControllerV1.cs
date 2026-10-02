using UnityEngine;

namespace DeadCells.Versions
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerControllerV1 : MonoBehaviour
    {
        [Header("Ground Movement")]
        [SerializeField] private float _moveSpeed;
        [SerializeField] private float _faceDirection;

        private Rigidbody2D _rigidbody;
        private float _horizontal;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            _horizontal = Input.GetAxisRaw("Horizontal"); //1 -1
        }

        private void FixedUpdate()
        {
            if(_horizontal == 0)
            {
                _rigidbody.linearVelocityX = 0;
                return;
            }

            UpdateDirection(_horizontal);
            _rigidbody.linearVelocityX = _horizontal * _moveSpeed;
        }

        private void UpdateDirection(float direction)
        {
            if(direction == 0 || _faceDirection == direction)
                return;

            _faceDirection = direction;
            transform.Rotate(0, 180, 0);
        }
    }
}
