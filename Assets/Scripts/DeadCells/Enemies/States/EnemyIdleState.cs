using UnityEngine;

namespace DeadCells.Enemies.States
{
    [DisallowMultipleComponent]
    public sealed class EnemyIdleState : BaseEnemyState
    {
        private bool _combatStarted;

        public override EnemyState Id => EnemyState.Idle;
        public override int ActivationPriority => 10;

        public override bool CanBeActivated(EnemyState? currentState)
        {
            if(_combatStarted)
                return false;

            return !SeesPlayer;
        }

        public override void EnterState()
        {
            base.EnterState();
            Stop();
        }

        public override void UpdateState()
        {
            Stop();
            if(SeesPlayer)
                _combatStarted = true;
        }

        public override void EndState()
        {
            if(SeesPlayer)
                _combatStarted = true;

            base.EndState();
        }

        public override void ResetState()
        {
            _combatStarted = false;
            base.ResetState();
        }
    }
}
