using UnityEngine;

namespace DeadCells.Enemies
{
    public class RangeEnemy : BaseEnemy
    {
        [Header("Retreat")]
        [SerializeField] private float _dangerDistance = 3;
        [SerializeField] private float _retreatSpeed = 5;
        [SerializeField] private float _retreatTime = 0.3f;

        [Header("Teleport")]
        [SerializeField] private float _teleportDistance = 2;
        [SerializeField] private Vector2 _teleportCheckRect = new(0.8f, 1.5f);
        [SerializeField] private Vector2 _teleportGroundCheckRect = new(0.8f, 0.2f);

        private float _retreatDirection;
        private float _retreatTimer;

        protected override void SwitchState()
        {
            switch(State)
            {
                case EnemyState.Retreat:
                    UpdateRetreat();
                    break;

                case EnemyState.Teleport:
                    UpdateTeleport();
                    break;

                default:
                    base.SwitchState();
                    break;
            }
        }

        protected override void UpdateAttack()
        {
            if(Player != null)
                UpdateDirection(DirectionToPlayer);

            base.UpdateAttack();
        }

        protected override void FinishAttack()
        {
            if(Player == null || PlayerDistance > _dangerDistance)
            {
                base.FinishAttack();
                return;
            }

            UpdateDirection(DirectionToPlayer);
            _retreatDirection = -DirectionToPlayer;

            if(CanMove(_retreatDirection, _retreatSpeed))
                StartRetreat();
            else
                StartTeleport();
        }

        private void StartRetreat()
        {
            _retreatTimer = _retreatTime;

            SetState(EnemyState.Retreat);
            Debug.Log($"{name}: Retreat");
        }

        private void UpdateRetreat()
        {
            if(!CanMove(_retreatDirection, _retreatSpeed))
            {
                StartTeleport();
                return;
            }

            Move(_retreatDirection, _retreatSpeed, false);

            _retreatTimer -= Time.fixedDeltaTime;

            if(_retreatTimer <= 0)
                StartRecover();
        }

        private void StartTeleport()
        {
            Rigidbody.linearVelocity = Vector2.zero;

            SetState(EnemyState.Teleport);
            Debug.Log($"{name}: Teleport");
        }

        private void UpdateTeleport()
        {
            if(TryGetTeleportPosition(out var position))
            {
                Rigidbody.linearVelocity = Vector2.zero;
                Rigidbody.position = position;
                UpdateDirection(DirectionToPlayer);
            }

            StartRecover();
        }

        private bool TryGetTeleportPosition(out Vector2 position)
        {
            position = default;

            var direction = DirectionToPlayer;
            var target = new Vector2(
                PlayerPosition.x + direction * _teleportDistance,
                Rigidbody.position.y);

            if(!HasTeleportGround(target))
                return false;

            var body = GetComponent<Collider2D>();
            var center = body != null
                ? target + ((Vector2)body.bounds.center - (Vector2)transform.position)
                : target + Vector2.up * (_teleportCheckRect.y * 0.5f);

            var size = body != null
                ? Vector2.Scale(body.bounds.size, new Vector2(0.9f, 0.75f))
                : _teleportCheckRect;

            if(IsOccupied(center, size))
                return false;

            position = target;
            return true;
        }

        private bool HasTeleportGround(Vector2 pivot)
        {
            if(HasGroundAt(pivot))
                return true;

            var origin = pivot + Vector2.down * (_teleportGroundCheckRect.y * 0.5f);
            return Physics2D.OverlapBox(origin, _teleportGroundCheckRect, 0, GroundMask) != null;
        }

        private bool IsOccupied(Vector2 center, Vector2 size)
        {
            var mask = WallMask.value | PlayerLayer.value;
            var hits = Physics2D.OverlapBoxAll(center, size, 0, mask);

            foreach(var hit in hits)
            {
                if(hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                return true;
            }

            return false;
        }
    }
}
