using UnityEngine;

namespace DeadCells.Versions.Versions.V08_StateCollider
{
    public class PlayerInput : MonoBehaviour
    {
        [SerializeField] private KeyCode _rollKey = KeyCode.LeftShift;

        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public bool JumpPressed => _jumpPressed;

        private bool _jumpPressed;
        private bool _rollPressed;

        private void Update()
        {
            Horizontal = Input.GetAxisRaw("Horizontal");
            Vertical = Input.GetAxisRaw("Vertical");

            if(Input.GetButtonDown("Jump"))
                _jumpPressed = true;

            if(Input.GetKeyDown(_rollKey))
                _rollPressed = true;
        }

        public bool ConsumeJump()
        {
            if(!_jumpPressed)
                return false;

            _jumpPressed = false;
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
