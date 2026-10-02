using UnityEngine;

namespace DeadCells.Versions.V12
{
    public class PlayerInput : MonoBehaviour
    {
        [SerializeField] private float _jumpBufferTime = 0.12f;
        [SerializeField] private KeyCode _rollKey = KeyCode.LeftShift;

        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public bool JumpHeld { get; private set; }

        public bool JumpBuffered => _jumpBufferTimer > 0;

        private float _jumpBufferTimer;
        private bool _rollPressed;

        private void Update()
        {
            Horizontal = Input.GetAxisRaw("Horizontal");
            Vertical = Input.GetAxisRaw("Vertical");
            JumpHeld = Input.GetButton("Jump");

            _jumpBufferTimer = Mathf.Max(0, _jumpBufferTimer - Time.deltaTime);

            if(Input.GetButtonDown("Jump"))
                _jumpBufferTimer = _jumpBufferTime;

            if(Input.GetKeyDown(_rollKey))
                _rollPressed = true;
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

        private void OnDisable()
        {
            Horizontal = 0;
            Vertical = 0;
            JumpHeld = false;
            _jumpBufferTimer = 0;
            _rollPressed = false;
        }
    }
}
