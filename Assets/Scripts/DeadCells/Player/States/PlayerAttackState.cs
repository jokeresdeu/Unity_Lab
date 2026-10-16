using UnityEngine;

namespace DeadCells.Player.States
{
    [DisallowMultipleComponent]
    public sealed class PlayerAttackState : PlayerMovementState
    {
        private bool _isComplete;
        private int _attackValue;

        public override PlayerState Id => PlayerState.Attack;
        public override int ActivationPriority => 60;
        public override int AnimationValue => _attackValue;

        public override bool CanBeActivated(PlayerState? currentState)
        {
            return currentState == Id ? !_isComplete : Input.LightAttackBuffered || Input.HeavyAttackBuffered;
        }

        public override void EnterState()
        {
            base.EnterState();
            _isComplete = false;
            _attackValue = Input.HeavyAttackBuffered ? 1 : 0;

            if(_attackValue == 1)
                Input.ConsumeHeavyAttack();
            else
                Input.ConsumeLightAttack();

            Rigidbody.linearVelocityX = 0;
            AnimationController.AnimationCompleted += OnAnimationEnded;
        }

        public override void UpdateState() => Rigidbody.linearVelocityX = 0;

        public override void EndState()
        {
            base.EndState();
            AnimationController.AnimationCompleted -= OnAnimationEnded;
        }

        public override void ResetState()
        {
            _isComplete = false;
            _attackValue = 0;
        }

        private void OnAnimationEnded() => _isComplete = true;
    }
}
