using System.Collections.Generic;
using UnityEngine;

namespace TailsOfIron.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class PatrolEnemy : MonoBehaviour
    {
        [field: Header("Patrol")]
        [field: SerializeField] protected Vector2 LeftPoint { get; private set; } = new(-3, 0);
        [field: SerializeField] protected Vector2 RightPoint { get; private set; } = new(3, 0);
        [field: SerializeField] protected float PatrolSpeed { get; private set; }
        [field: SerializeField] protected float FaceDirection { get; private set; } = 1;

        [field: Header("Player")]
        [field: SerializeField] protected Vector2 DetectionOffset { get; private set; }
        [field: SerializeField] protected LayerMask VisibilityMask { get; private set; }
        [field: SerializeField] protected LayerMask PlayerLayer { get; private set; }

        [field: Header("Ground")]
        [field: SerializeField] protected Transform GroundChecker { get; private set; }
        [field: SerializeField] protected Vector2 GroundCheckRect { get; private set; }
        [field: SerializeField] protected LayerMask Ground { get; private set; }
        [field: SerializeField] protected float EdgeCheckOffset { get; private set; } = 0.5f;
        [field: SerializeField] protected float EdgeCheckDistance { get; private set; } = 0.8f;

        [field: Header("State Machine")]
        [field: SerializeField] protected List<EnemyStateTime> StateTimes { get; private set; } = new();

        protected Rigidbody2D Rigidbody { get; private set; }
        protected EnemyState State { get; private set; } = EnemyState.None;

        protected Vector2 PlayerPosition => _player.position;
        protected float PlayerDistance => _player == null
            ? float.MaxValue
            : Vector2.Distance(transform.position, _player.position);
        protected float DirectionToPlayer => _player.position.x < transform.position.x ? -1 : 1;

        private Vector2 DetectionOrigin => transform.TransformPoint(DetectionOffset);

        private Transform _player;
        private Vector2 _patrolOrigin;
        private float _stateTimer = -1;
        private float _currentMoveSpeed;

        protected virtual void Start()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
            _patrolOrigin = transform.position;

            SetState(EnemyState.Patrol);
        }

        protected virtual void FixedUpdate()
        {
            UpdateStateTime();
            UpdateState();
        }

        protected abstract void UpdateState();

        protected virtual void OnStateStarted(EnemyState state)
        {
        }

        protected void SetState(EnemyState state)
        {
            if(State == state)
                return;

            State = state;

            var stateTime = StateTimes.Find(element => element.State == state);
            _stateTimer = stateTime == null ? -1 : stateTime.Time;

            OnStateStarted(state);
        }

        private void UpdateStateTime()
        {
            if(_stateTimer < 0)
                return;

            _stateTimer -= Time.fixedDeltaTime;

            if(_stateTimer > 0)
                return;

            var stateTime = StateTimes.Find(element => element.State == State);
            _stateTimer = -1;

            if(stateTime == null || stateTime.NextStates.Count == 0)
                return;

            SetState(GetNextState(stateTime));
        }

        protected virtual EnemyState GetNextState(EnemyStateTime stateTime) =>
            stateTime.NextStates[Random.Range(0, stateTime.NextStates.Count)];

        protected void Patrol()
        {
            if(ReachedPatrolBorder())
            {
                Stop();
                UpdateDirection(-FaceDirection);
                return;
            }

            if(!TryMove(FaceDirection, PatrolSpeed))
                UpdateDirection(-FaceDirection);
        }

        protected bool TryMove(float direction, float speed, bool updateDirection = true)
        {
            if(!HasGroundAhead(direction))
            {
                Stop();
                return false;
            }

            Move(direction, speed, updateDirection);
            return true;
        }

        protected void Move(float direction, float speed, bool updateDirection = true)
        {
            Rigidbody.linearVelocityX = direction * speed;

            if(updateDirection)
                UpdateDirection(direction);
        }

        protected void StartDecelerationMove(float speed) =>
            _currentMoveSpeed = speed;

        protected void DecelerationMove(float direction, float deceleration, bool updateDirection = true)
        {
            TryMove(direction, _currentMoveSpeed, updateDirection);

            _currentMoveSpeed = Mathf.MoveTowards(
                _currentMoveSpeed,
                0,
                deceleration * Time.fixedDeltaTime);
        }

        protected void Stop() =>
            Rigidbody.linearVelocityX = 0;

        protected bool TryDetectPlayer(float distance)
        {
            var hit = Physics2D.Raycast(
                DetectionOrigin,
                transform.forward,
                distance,
                VisibilityMask);

            if(hit.collider == null)
                return false;

            var playerLayer = 1 << hit.collider.gameObject.layer;

            if((PlayerLayer.value & playerLayer) == 0)
                return false;

            _player = hit.transform;
            return true;
        }

        protected bool PlayerOutOfRange(float distance) =>
            _player == null || PlayerDistance > distance;

        protected void ForgetPlayer() =>
            _player = null;

        protected bool IsGrounded() =>
            Physics2D.OverlapBox(
                GroundChecker.position,
                GroundCheckRect,
                0,
                Ground) != null;

        protected bool HasGroundAhead(float direction)
        {
            var origin = (Vector2)GroundChecker.position +
                         Vector2.right * direction * EdgeCheckOffset;

            return Physics2D.Raycast(
                origin,
                Vector2.down,
                EdgeCheckDistance,
                Ground);
        }

        protected void LaunchTo(Vector2 target, float time)
        {
            var distance = target - (Vector2)transform.position;
            var gravity = Physics2D.gravity.y * Rigidbody.gravityScale;

            var velocityX = distance.x / time;
            var velocityY = (distance.y - gravity * time * time / 2) / time;

            Rigidbody.linearVelocity = new Vector2(velocityX, velocityY);
        }

        protected void UpdateDirection(float direction)
        {
            if(direction == 0 || FaceDirection == direction)
                return;

            FaceDirection = direction;
            transform.Rotate(0, 180, 0);
        }

        protected void Attack(string attackName = "Attack") =>
            Debug.Log($"{name}: {attackName}");

        private bool ReachedPatrolBorder()
        {
            var left = _patrolOrigin.x + Mathf.Min(LeftPoint.x, RightPoint.x);
            var right = _patrolOrigin.x + Mathf.Max(LeftPoint.x, RightPoint.x);

            return FaceDirection > 0
                ? transform.position.x >= right
                : transform.position.x <= left;
        }

        protected void DrawDetectionGizmos(float distance)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(DetectionOrigin, 0.1f);
            Gizmos.DrawLine(
                DetectionOrigin,
                DetectionOrigin + Vector2.right * FaceDirection * distance);
        }

        protected virtual void OnDrawGizmos()
        {
            var origin = Application.isPlaying
                ? _patrolOrigin
                : (Vector2)transform.position;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, origin + LeftPoint);
            Gizmos.DrawLine(origin, origin + RightPoint);

            if(GroundChecker == null)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(GroundChecker.position, GroundCheckRect);
        }
    }
}