using UnityEngine;

namespace DeadCells.Enemies.States
{
    [DisallowMultipleComponent]
    public sealed class EnemyPatrolState : EnemyMovementState
    {
        [Header("Movement")]
        [SerializeField] private float _patrolSpeed = 1.5f;

        public override EnemyState Id => EnemyState.Patrol;
        public override int ActivationPriority => 10;
        protected override int AnimationValue => Mathf.Abs(Rigidbody.linearVelocityX) > 0.05f ? 1 : 0;

        public override bool CanBeActivated(EnemyState? currentState) => !SeesPlayer;

        public override void UpdateState()
        {
            if(!CanPatrol(Facing))
            {
                var opposite = -Facing;
                if(!CanPatrol(opposite))
                {
                    Stop();
                    PlayAnimation();
                    return;
                }

                Face(opposite);
            }

            Move(Facing, _patrolSpeed);
            PlayAnimation();
        }

        private bool CanPatrol(float direction)
        {
            if(!CanMoveOnPlatform(direction, _patrolSpeed * Time.fixedDeltaTime))
                return false;

            if(Player == null)
                return true;

            var towardsPlayer = DirectionToPlayer == Sign(direction);
            if(!towardsPlayer)
                return true;

            return PlayerDistance > 0.05f;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            DrawPlatformGizmo();
        }
    }
}
