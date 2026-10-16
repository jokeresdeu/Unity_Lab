using System.Linq;
using UnityEngine;

namespace DeadCells.Enemies.States
{
    [DisallowMultipleComponent]
    public sealed class EnemyTeleportState : EnemyMovementState
    {
        [Header("Range")]
        [SerializeField] private float _maxDistance = 3;

        [Header("Teleport")]
        [SerializeField] private float _standOff = 2;
        [SerializeField] private float _searchStep = 0.25f;
        [SerializeField] private Vector2 _teleportCheckRect = new(0.8f, 1.5f);
        [SerializeField] private LayerMask _playerLayer;

        private bool _teleported;

        public override EnemyState Id => EnemyState.Teleport;
        public override int ActivationPriority => 40;

        public override bool CanBeActivated(EnemyState? currentState)
        {
            if(currentState == Id)
                return !IsComplete;

            if(!SeesPlayer || PlayerDistance > _maxDistance)
                return false;

            return !CanMoveOnPlatform(-DirectionToPlayer, BodyInset) && TryGetTeleportPosition(out _);
        }

        public override void EnterState()
        {
            _teleported = false;
            base.EnterState();
            Rigidbody.linearVelocity = Vector2.zero;
            AnimationController.AnimationActionInvoked += OnAnimationAction;
            AnimationController.AnimationCompleted += OnAnimationEnded;
        }

        public override void UpdateState()
        {
            Rigidbody.linearVelocity = Vector2.zero;
            if(_teleported && Player != null)
                Face(DirectionToPlayer);
        }

        public override void EndState()
        {
            UnbindAnimation();
            base.EndState();
        }

        public override void ResetState()
        {
            UnbindAnimation();
            _teleported = false;
            base.ResetState();
        }

        private void OnAnimationAction()
        {
            if(_teleported)
                return;

            _teleported = true;
            if (!TryGetTeleportPosition(out var position)) 
                return;
            
            Rigidbody.position = position;
            transform.position = position;
            Face(DirectionToPlayer);
        }

        private void OnAnimationEnded() => Complete();

        private void UnbindAnimation()
        {
            if(AnimationController == null)
                return;

            AnimationController.AnimationActionInvoked -= OnAnimationAction;
            AnimationController.AnimationCompleted -= OnAnimationEnded;
        }

        private bool TryGetTeleportPosition(out Vector2 position)
        {
            position = default;
            var direction = DirectionToPlayer;
            if(!HasMovementLimits || Mathf.Approximately(direction, 0f))
                return false;

            var minX = MovementMin;
            var maxX = MovementMax;
            var inset = GetBodySize().x * 0.5f;
            var pastPlayer = PlayerPosition.x + direction * inset;
            var desired = PlayerPosition.x + direction * Mathf.Max(_standOff, inset);
            var startX = Mathf.Clamp(desired, minX, maxX);
            if((startX - PlayerPosition.x) * direction <= 0f)
                startX = Mathf.Clamp(pastPlayer, minX, maxX);

            var endX = direction > 0f ? maxX : minX;
            if((startX - PlayerPosition.x) * direction <= 0f ||
               (endX - startX) * direction < 0f)
                return false;

            var step = Mathf.Max(0.05f, _searchStep);
            var distance = Mathf.Abs(endX - startX);
            var y = Rigidbody.position.y;

            for(var travelled = 0f; travelled <= distance; travelled += step)
            {
                var x = startX + direction * travelled;
                var pivot = new Vector2(x, y);
                if(!IsOnPlatform(pivot) || IsOccupiedAt(pivot))
                    continue;

                position = pivot;
                return true;
            }

            return false;
        }

        private bool IsOnPlatform(Vector2 pivot) =>
            pivot.x >= MovementMin && pivot.x <= MovementMax;

        private bool IsOccupiedAt(Vector2 pivot)
        {
            var size = GetBodySize();
            var center = pivot + GetBodyOffset();
            var hits = Physics2D.OverlapBoxAll(center, size, 0, _playerLayer);

            return hits.Any(hit => hit.transform != transform && !hit.transform.IsChildOf(transform));
        }

        private Vector2 GetBodySize() => Body == null ? _teleportCheckRect : Vector2.Scale(Body.bounds.size, new Vector2(0.9f, 0.75f));

        private Vector2 GetBodyOffset()
        {
            if(Body == null)
                return Vector2.up * (_teleportCheckRect.y * 0.5f);

            return (Vector2)Body.bounds.center - (Vector2)transform.position;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            DrawPlatformGizmo();
        }
    }
}
