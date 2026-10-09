using UnityEngine;

namespace DeadCells.Player.States
{
    [DisallowMultipleComponent]
    public sealed class PlayerHeavyAttackState : PlayerMovementState
    {
        private bool _isComplete;

        public override PlayerState Id => PlayerState.HeavyAttack;
        public override int ActivationPriority => 70;

        public override bool CanBeActivated(PlayerState? currentState)
        {
            return currentState == Id ? !_isComplete : Input.HeavyAttackBuffered;
        }

        public override void EnterState()
        {
            base.EnterState();
            Input.ConsumeHeavyAttack();
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
