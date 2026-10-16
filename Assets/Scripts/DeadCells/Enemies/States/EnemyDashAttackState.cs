using System.Collections;
using UnityEngine;

namespace DeadCells.Enemies.States
{
    [DisallowMultipleComponent]
    public sealed class EnemyDashAttackState : EnemyMovementState
    {
        [Header("Range")]
        [SerializeField] private float _minDistance = 1.2f;
        [SerializeField] private float _maxDistance = 8;

        [Header("Dash")]
        [SerializeField] private float _dashSpeed = 8;
        [SerializeField] private float _overshoot = 0.5f;
        [SerializeField] private float _windupTime = 0.5f;
        
        private float _attackDirection;
        private float _targetX;

        public override EnemyState Id => EnemyState.Attack;
        public override int ActivationPriority => 50;

        public override bool CanBeActivated(EnemyState? currentState)
        {
            if(currentState == Id)
                return !IsComplete;

            return SeesPlayer && IsInRange();
        }

        public override void EnterState()
        {
            base.EnterState();
            Stop();
            Face(DirectionToPlayer);
            StartCoroutine(Run());
        }
        
        private IEnumerator Run()
        {
            var remaining = _windupTime;
            while(remaining > 0f)
            {
                Stop();
                if(Player != null)
                    Face(DirectionToPlayer);

                yield return new WaitForFixedUpdate();
                remaining -= Time.fixedDeltaTime;
            }

            ChooseDashTarget();
            StartCharge();

            while(!HasReachedTarget() &&
                  CanMoveOnPlatform(_attackDirection, _dashSpeed * Time.fixedDeltaTime))
            {
                Move(_attackDirection, _dashSpeed, false);
                yield return new WaitForFixedUpdate();
            }

            Stop();
            Complete();
        }

        private void ChooseDashTarget()
        {
            _attackDirection = DirectionToPlayer;
            Face(_attackDirection);

            _targetX = ClampToMovementLimits(PlayerPosition.x + _attackDirection * Mathf.Max(0f, _overshoot));

            var toTarget = _targetX - transform.position.x;
            if(!Mathf.Approximately(toTarget, 0f))
                _attackDirection = Sign(toTarget);
        }

        private bool HasReachedTarget() =>
            (_targetX - transform.position.x) * _attackDirection <= 0f;

        private void StartCharge() => PlayAnimation(true);

        private bool IsInRange() => PlayerDistance >= _minDistance && PlayerDistance <= _maxDistance;

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            DrawPlatformGizmo();
            DrawForwardRangeGizmo(_minDistance, _maxDistance, Color.red);
        }
    }
}
