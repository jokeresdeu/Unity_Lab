using System.Collections;
using UnityEngine;

namespace DeadCells.Enemies.States
{
    [DisallowMultipleComponent]
    public sealed class EnemyRetreatState : EnemyMovementState
    {
        [Header("Range")]
        [SerializeField] private float _maxDistance = 3;

        [Header("Retreat")]
        [SerializeField] private float _retreatSpeed = 5;
        [SerializeField] private float _retreatTime = 0.3f;

        public override EnemyState Id => EnemyState.Retreat;
        public override int ActivationPriority => 40;
        protected override int AnimationValue => 1;

        public override bool CanBeActivated(EnemyState? currentState)
        {
            if(currentState == Id)
                return !IsComplete;

            if(!SeesPlayer || PlayerDistance > _maxDistance)
                return false;

            return CanMoveOnPlatform(-DirectionToPlayer, BodyInset);
        }

        public override void EnterState()
        {
            base.EnterState();
            Face(DirectionToPlayer);
            StartCoroutine(Run(-DirectionToPlayer));
        }

        private IEnumerator Run(float direction)
        {
            var remaining = _retreatTime;
            while(remaining > 0f && CanMoveOnPlatform(direction, _retreatSpeed * Time.fixedDeltaTime))
            {
                Move(direction, _retreatSpeed, false);
                yield return new WaitForFixedUpdate();
                remaining -= Time.fixedDeltaTime;
            }

            Stop();
            Complete();
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            DrawPlatformGizmo();

            if(_maxDistance <= 0f)
                return;

            var body = Body != null ? Body : GetComponent<Collider2D>();
            var origin = body != null ? (Vector2)body.bounds.center : (Vector2)transform.position;
            var height = body != null ? Mathf.Max(0.3f, body.bounds.size.y) : 0.8f;
            var center = origin + Vector2.right * Facing * _maxDistance * 0.5f;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center, new Vector3(_maxDistance, height, 0f));
        }
    }
}
