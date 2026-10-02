using UnityEngine;

namespace DeadCells.Versions.Versions.V13_Ladder
{
    public class PlayerInput : MonoBehaviour
    {
        [SerializeField] private float _jumpBufferTime = 0.12f;
        [SerializeField] private KeyCode _rollKey = KeyCode.LeftShift;

        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }

        public bool JumpBuffered => _jumpBufferTimer > 0;

        private float _jumpBufferTimer;
        private bool _rollPressed;

        private void Update()
        {
            Horizontal = Input.GetAxisRaw("Horizontal");
            Vertical = Input.GetAxisRaw("Vertical");

            if(Input.GetButtonDown("Jump"))
                _jumpBufferTimer = _jumpBufferTime;

            if(Input.GetKeyDown(_rollKey))
                _rollPressed = true;

            _jumpBufferTimer -= Time.deltaTime;
        }

        public bool ConsumeJump()
        {
            if(!JumpBuffered)
                return false;

            _jumpBufferTimer = 0;
            return true;
        }

        public bool ConsumeRoll()
        {
            if(!_rollPressed)
                return false;

            _rollPressed = false;
            return true;
        }
    }
}
