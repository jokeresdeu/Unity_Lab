using UnityEngine;

namespace DeadCells.Enemies.States
{
    public abstract class BaseEnemyState : MonoBehaviour
    {
        protected Rigidbody2D Rigidbody;
        protected Collider2D Body { get; private set; }
        protected EnemyAnimationController AnimationController;
        protected Transform Player { get; private set; }
        protected Collider2D PlayerBody { get; private set; }
        protected bool IsComplete;

        private const float NoRotationDistance = 0.05f;

        private float _initialGravityScale;

        public abstract EnemyState Id { get; }
        public abstract int ActivationPriority { get; }
        protected virtual int AnimationValue => 0;
        public bool HasCompleted => IsComplete;

        protected bool SeesPlayer => Player != null;
        protected float Facing => transform.forward.z >= 0f ? 1f : -1f;

        protected Bounds BodyBounds => Body != null ? Body.bounds : new Bounds(transform.position, Vector3.one * 0.4f);

        protected Bounds PlayerBounds => PlayerBody != null
            ? PlayerBody.bounds
            : Player != null
                ? new Bounds(Player.position, Vector3.one * 0.4f)
                : BodyBounds;

        protected Vector2 PlayerPosition => SeesPlayer ? (Vector2)PlayerBounds.center : Rigidbody.position;

        protected float PlayerDistance
        {
            get
            {
                if(!SeesPlayer)
                    return float.MaxValue;

                return HorizontalGap(BodyBounds, PlayerBounds);
            }
        }

        protected float DirectionToPlayer
        {
            get
            {
                if(!SeesPlayer)
                    return Facing;

                var delta = PlayerBounds.center.x - BodyBounds.center.x;
                if(Mathf.Abs(delta) <= NoRotationDistance)
                    return Facing;

                return delta < 0f ? -1f : 1f;
            }
        }

        public virtual void Initialize(Rigidbody2D rigidbody, EnemyAnimationController animationController)
        {
            Rigidbody = rigidbody;
            Body = GetComponent<Collider2D>();
            AnimationController = animationController;
            _initialGravityScale = rigidbody.gravityScale;
        }

        public void SetPlayer(Transform player)
        {
            Player = player;
            PlayerBody = player != null ? player.GetComponent<Collider2D>() : null;
        }

        protected Vector2 GetFrontOfPlayer()
        {
            var bounds = PlayerBounds;
            var direction = DirectionToPlayer;
            var edgeX = direction > 0f ? bounds.min.x : bounds.max.x;
            var standOff = Body != null ? Body.bounds.extents.x : 0.2f;
            return new Vector2(edgeX - direction * standOff, Rigidbody.position.y);
        }

        protected static float HorizontalGap(Bounds a, Bounds b)
        {
            if(a.max.x < b.min.x)
                return b.min.x - a.max.x;
            if(b.max.x < a.min.x)
                return a.min.x - b.max.x;

            return 0f;
        }

        public virtual void EnterState()
        {
            IsComplete = false;
            Rigidbody.gravityScale = _initialGravityScale;
            PlayAnimation();
        }

        public abstract bool CanBeActivated(EnemyState? currentState);
        public virtual void UpdateState() { }

        public virtual void EndState()
        {
            StopAllCoroutines();
            Rigidbody.gravityScale = _initialGravityScale;
        }

        public virtual void ResetState()
        {
            StopAllCoroutines();
            IsComplete = false;
        }

        protected void Complete() => IsComplete = true;

        protected void PlayAnimation(bool restart = false) =>
            AnimationController.PlayAnimation(Id, AnimationValue, restart);
        
        protected void Face(float direction)
        {
            var sign = Sign(direction);
            if(sign == 0f || Mathf.Sign(transform.forward.z) == sign)
                return;

            transform.Rotate(0, 180, 0);
        }

        protected void Stop() => Rigidbody.linearVelocityX = 0;

        protected virtual void OnDrawGizmosSelected() => DrawNoRotationDistanceGizmo();

        protected void DrawNoRotationDistanceGizmo()
        {
            var body = GetComponent<Collider2D>();
            var center = body != null ? body.bounds.center : transform.position;
            var height = body != null ? Mathf.Max(0.3f, body.bounds.size.y) : 0.8f;

            Gizmos.color = new Color(1f, 0.35f, 0.8f);
            Gizmos.DrawWireCube(center, new Vector3(NoRotationDistance * 2f, height, 0f));
        }

        protected void DrawForwardRangeGizmo(float minDistance, float maxDistance, Color color)
        {
            if(maxDistance <= 0f)
                return;

            var body = GetComponent<Collider2D>();
            var origin = body != null ? (Vector2)body.bounds.center : (Vector2)transform.position;
            var height = body != null ? Mathf.Max(0.3f, body.bounds.size.y) : 0.8f;
            var min = Mathf.Max(0f, minDistance);
            var length = maxDistance - min;
            if(length <= 0f)
                return;

            var center = origin + Vector2.right * Facing * (min + length * 0.5f);
            Gizmos.color = color;
            Gizmos.DrawWireCube(center, new Vector3(length, height, 0f));
        }

        protected static float Sign(float direction)
        {
            if(Mathf.Approximately(direction, 0f))
                return 0f;

            return direction < 0f ? -1f : 1f;
        }
    }
}
