using UnityEngine;

namespace DeadCells.Player.States
{
    [DisallowMultipleComponent]
    public sealed class PlayerLightAttackState : PlayerMovementState
    {
        private bool _isComplete;

        public override PlayerState Id => PlayerState.LightAttack;
        public override int ActivationPriority => 60;

        public override bool CanBeActivated(PlayerState? currentState)
        {
            return currentState == Id ? !_isComplete : Input.LightAttackBuffered;
        }

        public override void EnterState()
        {
            base.EnterState();
            Input.ConsumeLightAttack();
            Rigidbody.linearVelocityX = 0;
            _isComplete = false;
            AnimationController.AnimationCompleted += OnAnimationEnded;
        }

        public override void UpdateState() => Rigidbody.linearVelocityX = 0;

        public override void EndState()
        {
            base.EndState();
            AnimationController.AnimationCompleted -= OnAnimationEnded;
        }

        private void OnAnimationEnded() => _isComplete = true;
    }
}
