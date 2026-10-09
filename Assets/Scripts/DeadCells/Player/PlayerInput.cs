using UnityEngine;

namespace DeadCells.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInput : MonoBehaviour
    {
        [SerializeField] private float _jumpBufferTime = 0.12f;
        [SerializeField] private float _rollBufferTime = 0.12f;
        [SerializeField] private float _attackBufferTime = 0.12f;
        [SerializeField] private KeyCode _rollKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode _lightAttackKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode _heavyAttackKey = KeyCode.Mouse1;

        public int Horizontal { get; private set; }
        public int Vertical { get; private set; }
        public bool JumpBuffered => _jumpBufferTimer > 0;
        public bool RollBuffered => _rollBufferTimer > 0;
        public bool LightAttackBuffered => _lightAttackBufferTimer > 0;
        public bool HeavyAttackBuffered => _heavyAttackBufferTimer > 0;

        private float _jumpBufferTimer;
        private float _rollBufferTimer;
        private float _lightAttackBufferTimer;
        private float _heavyAttackBufferTimer;

        private void Update()
        {
            Horizontal = (int)Input.GetAxisRaw("Horizontal");
            Vertical = (int)Input.GetAxisRaw("Vertical");

            _jumpBufferTimer = Mathf.Max(0, _jumpBufferTimer - Time.deltaTime);
            _rollBufferTimer = Mathf.Max(0, _rollBufferTimer - Time.deltaTime);
            _lightAttackBufferTimer = Mathf.Max(0, _lightAttackBufferTimer - Time.deltaTime);
            _heavyAttackBufferTimer = Mathf.Max(0, _heavyAttackBufferTimer - Time.deltaTime);

            if(Input.GetButtonDown("Jump"))
                _jumpBufferTimer = _jumpBufferTime;

            if(Input.GetKeyDown(_rollKey))
                _rollBufferTimer = _rollBufferTime;

            if(Input.GetKeyDown(_lightAttackKey))
                _lightAttackBufferTimer = _attackBufferTime;

            if(Input.GetKeyDown(_heavyAttackKey))
                _heavyAttackBufferTimer = _attackBufferTime;
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
            if(_rollBufferTimer <= 0)
                return false;

            _rollBufferTimer = 0;
            return true;
        }

        public bool ConsumeLightAttack()
        {
            if(!LightAttackBuffered)
                return false;

            _lightAttackBufferTimer = 0;
            return true;
        }

        public bool ConsumeHeavyAttack()
        {
            if(!HeavyAttackBuffered)
                return false;

            _heavyAttackBufferTimer = 0;
            return true;
        }

        private void OnDisable()
        {
            Horizontal = 0;
            Vertical = 0;
            _jumpBufferTimer = 0;
            _rollBufferTimer = 0;
            _lightAttackBufferTimer = 0;
            _heavyAttackBufferTimer = 0;
        }
    }
}
