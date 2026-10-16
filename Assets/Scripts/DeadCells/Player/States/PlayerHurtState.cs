using UnityEngine;

namespace DeadCells.Player.States
{
    [DisallowMultipleComponent]
    public sealed class PlayerHurtState : PlayerMovementState
    {
        private int _requestedValue = -1;
        private int _hurtValue;
        private bool _isComplete;

        public override PlayerState Id => PlayerState.Hurt;
        public override int ActivationPriority => 90;
        public override int AnimationValue => _hurtValue;
        private bool IsDead => _hurtValue == 2;

        public void Hurt() => Request(0);
        public void Stagger() => Request(1);
        public void Die() => Request(2);

        public override bool CanBeActivated(PlayerState? currentState)
        {
            if(currentState == Id)
                return IsDead || !_isComplete || _requestedValue > _hurtValue;

            return _requestedValue >= 0;
        }

        public override void EnterState()
        {
            base.EnterState();
            ApplyHurt(Mathf.Max(_requestedValue, 0));
            Rigidbody.linearVelocity = default;
            AnimationController.AnimationCompleted += OnAnimationEnded;
        }

        public override void UpdateState()
        {
            Rigidbody.linearVelocity = default;

            if(_requestedValue > _hurtValue)
                ApplyHurt(_requestedValue);
        }

        public override void EndState()
        {
            base.EndState();
            AnimationController.AnimationCompleted -= OnAnimationEnded;

            if(!IsDead)
                _requestedValue = -1;
        }

        public override void ResetState()
        {
            _requestedValue = -1;
            _hurtValue = 0;
            _isComplete = false;
        }

        private void Request(int value)
        {
            if(value >= _requestedValue)
                _requestedValue = value;
        }

        private void ApplyHurt(int value)
        {
            _hurtValue = value;
            _requestedValue = value;
            _isComplete = IsDead;
        }

        private void OnAnimationEnded()
        {
            if(!IsDead)
                _isComplete = true;
        }
    }
}
