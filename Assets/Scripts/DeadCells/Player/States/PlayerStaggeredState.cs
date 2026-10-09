using UnityEngine;

namespace DeadCells.Player.States
{
    [DisallowMultipleComponent]
    public sealed class PlayerStaggeredState : PlayerMovementState
    {
        private bool _activationRequested;
        private bool _isComplete;

        public override PlayerState Id => PlayerState.Staggered;
        public override int ActivationPriority => 80;

        public void Stagger() => _activationRequested = true;

        public override bool CanBeActivated(PlayerState? currentState)
        {
            return currentState == Id ? !_isComplete : _activationRequested;
        }

        public override void EnterState()
        {
            base.EnterState();
            _activationRequested = false;
            _isComplete = false;
            Rigidbody.linearVelocity = default;
            AnimationController.AnimationCompleted += OnAnimationEnded;
        }

        public override void UpdateState() => Rigidbody.linearVelocity = default;

        public override void EndState()
        {
            base.EndState();
            AnimationController.AnimationCompleted -= OnAnimationEnded;
        }

        private void OnAnimationEnded() => _isComplete = true;
    }
}
