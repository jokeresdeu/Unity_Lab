using UnityEngine;

namespace DeadCells.Enemies.States
{
    [DisallowMultipleComponent]
    public sealed class EnemyChaseState : EnemyMovementState
    {
        [Header("Range")]
        [SerializeField] private float _stopDistance = 0.45f;
        [SerializeField] private float _maxDistance = 1;

        [Header("Movement")]
        [SerializeField] private float _chaseSpeed = 1.5f;

        public override EnemyState Id => EnemyState.Chase;
        public override int ActivationPriority => 20;
        protected override int AnimationValue => Mathf.Abs(Rigidbody.linearVelocityX) > 0.05f ? 1 : 0;

        public override bool CanBeActivated(EnemyState? currentState)
        {
            if(currentState == Id)
                return !IsComplete;

            return SeesPlayer && IsInChaseRange() && CanMoveTowardPlayer();
        }

        public override void UpdateState()
        {
            if(!SeesPlayer || !IsInChaseRange())
            {
                Stop();
                Complete();
                return;
            }

            var direction = DirectionToPlayer;
            Face(direction);

            if(!CanMoveOnPlatform(direction, _chaseSpeed * Time.fixedDeltaTime) ||
               PlayerDistance <= 0.05f)
            {
                Stop();
                PlayAnimation();
                return;
            }

            Move(direction, _chaseSpeed);
            PlayAnimation();
        }

        private bool IsInChaseRange() =>
            PlayerDistance > _stopDistance &&
            (_maxDistance <= 0f || PlayerDistance <= _maxDistance);

        private bool CanMoveTowardPlayer() =>
            CanMoveOnPlatform(DirectionToPlayer, _chaseSpeed * Time.fixedDeltaTime);

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            DrawPlatformGizmo();
            DrawForwardRangeGizmo(_stopDistance, _maxDistance, new Color(1f, 0.55f, 0f));
        }
    }
}
