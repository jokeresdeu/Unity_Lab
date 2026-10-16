using UnityEngine;

namespace DeadCells.Enemies.States
{
    public abstract class EnemyMovementState : BaseEnemyState
    {
        protected float MovementMin { get; private set; }
        protected float MovementMax { get; private set; }
        protected bool HasMovementLimits => MovementMax > MovementMin;

        protected float BodyInset => Body != null ? Body.bounds.extents.x * 0.9f : 0.2f;

        public void SetMovementLimits(float min, float max)
        {
            MovementMin = min;
            MovementMax = max;
        }

        protected void Move(float direction, float speed, bool face = true)
        {
            if(Mathf.Approximately(direction, 0f) || speed <= 0f)
            {
                Stop();
                return;
            }

            var sign = Sign(direction);
            if(face)
                Face(sign);

            Rigidbody.linearVelocityX = sign * speed;
        }

        private float DistanceToEdge(float direction)
        {
            if(!HasMovementLimits)
                return 0f;

            var x = transform.position.x;
            var sign = Sign(direction);
            return sign switch
            {
                > 0f => MovementMax - x,
                < 0f => x - MovementMin,
                _ => 0f
            };
        }

        protected bool CanMoveOnPlatform(float direction, float distance = 0.05f) =>
            DistanceToEdge(direction) > distance && !IsBlocked(direction);

        protected float ClampToMovementLimits(float x)
        {
            if(!HasMovementLimits)
                return x;

            return Mathf.Clamp(x, MovementMin, MovementMax);
        }

        private bool IsBlocked(float direction)
        {
            var sign = Sign(direction);
            if(sign == 0f)
                return false;

            Vector2 center;
            Vector2 size;
            if(Body != null)
            {
                center = (Vector2)Body.bounds.center + Vector2.right * sign * Body.bounds.extents.x;
                size = new Vector2(0.12f, Mathf.Max(0.1f, Body.bounds.size.y * 0.5f));
            }
            else
            {
                center = (Vector2)transform.position + Vector2.right * sign * 0.25f;
                size = new Vector2(0.12f, 0.4f);
            }

            var hits = Physics2D.OverlapBoxAll(center, size, 0f);
            foreach(var hit in hits)
            {
                if(hit == null || hit.isTrigger || hit == Body)
                    continue;
                if(hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;
                if(hit.attachedRigidbody != null && hit.attachedRigidbody != Rigidbody)
                    continue;

                return true;
            }

            return false;
        }

        protected void DrawPlatformGizmo()
        {
            if(!HasMovementLimits)
                return;

            var width = MovementMax - MovementMin;
            var center = new Vector3((MovementMin + MovementMax) * 0.5f, transform.position.y, 0f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(center, new Vector3(width, 0.2f, 0f));
        }
    }
}
