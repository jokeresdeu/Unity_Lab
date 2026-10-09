using UnityEngine;

namespace DeadCells.Player.States
{
    [DisallowMultipleComponent]
    public sealed class PlayerDeathState : PlayerMovementState
    {
        private bool _activationRequested;

        public override PlayerState Id => PlayerState.Death;
        public override int ActivationPriority => 90;

        public void Die() => _activationRequested = true;

        public override bool CanBeActivated(PlayerState? currentState)
        {
            return currentState == Id || _activationRequested;
        }

        public override void EnterState()
        {
            base.EnterState();
            _activationRequested = false;
            Rigidbody.linearVelocity = default;
        }

        public override void UpdateState() => Rigidbody.linearVelocity = default;
    }
}
