using System.Collections;
using UnityEngine;

namespace DeadCells.Enemies.States
{
    [DisallowMultipleComponent]
    public sealed class EnemyRecoverState : BaseEnemyState
    {
        private float _duration;

        public override EnemyState Id => EnemyState.Recover;
        public override int ActivationPriority => 20;

        public void SetDuration(float duration) => _duration = Mathf.Max(0f, duration);

        public override bool CanBeActivated(EnemyState? currentState)
        {
            if(currentState == Id)
                return !IsComplete;

            return false;
        }

        public override void EnterState()
        {
            base.EnterState();
            Stop();
            StartCoroutine(Run());
        }

        public override void ResetState()
        {
            base.ResetState();
            _duration = 0f;
        }

        private IEnumerator Run()
        {
            var remaining = _duration;
            while(remaining > 0f)
            {
                Stop();
                if(Player != null)
                    Face(DirectionToPlayer);

                yield return new WaitForFixedUpdate();
                remaining -= Time.fixedDeltaTime;
            }

            Complete();
        }
    }
}
