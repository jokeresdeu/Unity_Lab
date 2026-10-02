using UnityEngine;

namespace DeadCells.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BaseEnemy : MonoBehaviour
    {
        [Header("Patrol")]
        [SerializeField] protected float _patrolSpeed = 1.5f;
        [SerializeField] protected float _faceDirection = 1;

        [Header("Detection")]
        [SerializeField] private float _detectionDistance = 5;
        [SerializeField] private float _combatDetectionDistance = 8;
        [SerializeField] private float _detectionHeight = 0.45f;
        [SerializeField] private float _detectionRadius = 0.12f;
        [SerializeField] private float _stopDistance = 0.7f;
        [SerializeField] private LayerMask _playerLayer;

        [Header("Path")]
        [SerializeField] private Transform _pathChecker;
        [SerializeField] private Vector2 _pathCheckRect = new(0.3f, 0.3f);
        [SerializeField] private LayerMask _ground;
        [SerializeField] private LayerMask _wall;

        [Header("Ground")]
        [SerializeField] private Transform _groundChecker;
        [SerializeField] private Vector2 _groundCheckRect = new(0.5f, 0.15f);

        [Header("Attack")]
        [SerializeField] protected float _attackTime = 0.5f;

        [Header("Recover")]
        [SerializeField] protected float _recoverTime = 0.5f;

        protected Rigidbody2D Rigidbody { get; private set; }
        protected Transform Player { get; private set; }
        protected EnemyState State { get; private set; }

        protected Vector2 PlayerPosition => Player != null ? Player.position : transform.position;

        protected float DirectionToPlayer
        {
            get
            {
                if(Player == null)
                    return _faceDirection;

                var delta = Player.position.x - transform.position.x;

                if(Mathf.Abs(delta) <= 0.001f)
                    return _faceDirection;

                return delta < 0f ? -1f : 1f;
            }
        }

        protected float PlayerDistance => Player == null
            ? float.MaxValue
            : Mathf.Abs(Player.position.x - transform.position.x);

        protected LayerMask GroundMask => _ground;
        protected LayerMask WallMask => _wall;
        protected LayerMask PlayerLayer => _playerLayer;

        protected bool IsGrounded =>
            _groundChecker != null &&
            Physics2D.OverlapBox(_groundChecker.position, _groundCheckRect, 0, _ground) != null;

        private float _attackTimer;
        private float _recoverTimer;

        private Vector2 DetectionOrigin =>
            (Vector2)transform.position + Vector2.up * _detectionHeight;

        private int SightMask => _playerLayer.value | _ground.value | _wall.value;

        protected virtual void Awake()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
            State = EnemyState.Patrol;
        }

        protected virtual void FixedUpdate() =>
            SwitchState();

        protected virtual void SwitchState()
        {
            switch(State)
            {
                case EnemyState.Patrol:
                    UpdatePatrol();
                    break;

                case EnemyState.Attack:
                    UpdateAttack();
                    break;

                case EnemyState.Recover:
                    UpdateRecover();
                    break;
            }
        }

        protected virtual void UpdatePatrol()
        {
            if(TryDetectPlayer())
            {
                StartAttack();
                return;
            }

            if(!TryTurnAround(_patrolSpeed))
                return;

            Move(_faceDirection, _patrolSpeed);
        }

        protected virtual bool TryDetectPlayer() =>
            TryFindPlayer(_faceDirection, _detectionDistance);

        protected virtual bool TryDetectPlayerInCombat()
        {
            if(TryFindPlayer(_faceDirection, _combatDetectionDistance))
                return true;

            return TryFindPlayer(-_faceDirection, _combatDetectionDistance);
        }

        protected virtual void StartAttack()
        {
            Stop();

            if(Player != null)
                UpdateDirection(DirectionToPlayer);

            _attackTimer = _attackTime;
            SetState(EnemyState.Attack);

            Debug.Log($"{name}: Attack");
        }

        protected virtual void UpdateAttack()
        {
            _attackTimer -= Time.fixedDeltaTime;

            if(_attackTimer <= 0)
                FinishAttack();
        }

        protected virtual void FinishAttack() =>
            StartRecover();

        protected void StartRecover()
        {
            Stop();

            _recoverTimer = _recoverTime;
            SetState(EnemyState.Recover);
        }

        protected virtual void UpdateRecover()
        {
            Stop();

            _recoverTimer -= Time.fixedDeltaTime;

            if(_recoverTimer > 0)
                return;

            if(!TryDetectPlayerInCombat())
            {
                Player = null;
                SetState(EnemyState.Patrol);
                return;
            }

            StartAttack();
        }

        protected bool CanMove(float direction, float speed = 0f)
        {
            if(_pathChecker == null || Mathf.Approximately(direction, 0f))
                return false;

            var sign = Sign(direction);
            var position = GetPathCheckPosition(sign, speed);

            var hasGround = Physics2D.OverlapBox(position, _pathCheckRect, 0, _ground) != null;
            var hasWall = Physics2D.OverlapBox(position, _pathCheckRect, 0, _wall) != null;

            if(!hasGround || hasWall)
                return false;

            var bodyOrigin = (Vector2)transform.position + Vector2.up * _detectionHeight;
            var reach = Mathf.Abs(position.x - transform.position.x);
            var wallAhead = Physics2D.Raycast(bodyOrigin, Vector2.right * sign, reach, _wall);

            return wallAhead.collider == null;
        }

        protected bool TryTurnAround(float speed)
        {
            if(CanMove(_faceDirection, speed))
                return true;

            var opposite = -_faceDirection;

            if(!CanMove(opposite, speed))
            {
                Stop();
                return false;
            }

            UpdateDirection(opposite);
            return true;
        }

        protected bool HasGroundAt(Vector2 position)
        {
            var verticalOffset = _groundChecker != null
                ? _groundChecker.position.y - transform.position.y
                : 0f;

            var origin = new Vector2(position.x, position.y + verticalOffset);

            return Physics2D.OverlapBox(origin, _groundCheckRect, 0, _ground) != null;
        }

        protected void SetState(EnemyState state) =>
            State = state;

        protected void Move(float direction, float speed, bool updateDirection = true)
        {
            if(Mathf.Approximately(direction, 0f) || speed <= 0f)
            {
                Stop();
                return;
            }

            var sign = Sign(direction);

            if(updateDirection)
                UpdateDirection(sign);

            if(!TryClampTowardPlayer(sign, ref speed))
            {
                Stop();
                return;
            }

            Rigidbody.linearVelocityX = sign * speed;
        }

        protected void Stop() =>
            Rigidbody.linearVelocityX = 0;

        protected void LaunchTo(Vector2 target, float speed)
        {
            var distance = target - Rigidbody.position;
            var length = distance.magnitude;

            if(length < 0.01f || speed <= 0f)
            {
                Rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            var time = length / speed;
            var gravity = Physics2D.gravity.y * Rigidbody.gravityScale;

            var velocityX = distance.x / time;
            var velocityY = (distance.y - 0.5f * gravity * time * time) / time;

            Rigidbody.linearVelocity = new Vector2(velocityX, velocityY);
        }

        protected void UpdateDirection(float direction)
        {
            var sign = Sign(direction);

            if(sign == 0f || _faceDirection == sign)
                return;

            _faceDirection = sign;
            transform.Rotate(0, 180, 0);
        }

        protected virtual void OnDrawGizmos()
        {
            var origin = (Vector2)transform.position + Vector2.up * _detectionHeight;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, _detectionRadius);
            Gizmos.DrawLine(origin, origin + Vector2.right * _faceDirection * _detectionDistance);

            if(_pathChecker != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(GetPathCheckPosition(_faceDirection, 0f), _pathCheckRect);
            }

            if(_groundChecker != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(_groundChecker.position, _groundCheckRect);
            }
        }

        private bool TryFindPlayer(float direction, float distance)
        {
            if(distance <= 0f || Mathf.Approximately(direction, 0f))
                return false;

            var sign = Sign(direction);
            var found = false;
            RaycastHit2D closest = default;

            for(var i = 0; i < 2; i++)
            {
                var origin = DetectionOrigin + Vector2.up * (i * _detectionRadius * 2f);
                var hit = Physics2D.CircleCast(origin, _detectionRadius, Vector2.right * sign, distance, SightMask);

                if(hit.collider == null)
                    continue;

                if(!IsPlayerHit(hit) && hit.distance <= 0.02f)
                    continue;

                if(found && hit.distance >= closest.distance)
                    continue;

                closest = hit;
                found = true;
            }

            if(!found || !IsPlayerHit(closest))
                return false;

            if(closest.collider.transform == transform || closest.collider.transform.IsChildOf(transform))
                return false;

            Player = closest.rigidbody != null ? closest.rigidbody.transform : closest.collider.transform;
            return true;
        }

        private bool IsPlayerHit(RaycastHit2D hit)
        {
            var layerBit = 1 << hit.collider.gameObject.layer;
            return (_playerLayer.value & layerBit) != 0;
        }

        private bool TryClampTowardPlayer(float sign, ref float speed)
        {
            if(Player == null)
                return true;

            var delta = Player.position.x - transform.position.x;

            if(delta * sign <= 0f)
                return true;

            var allowed = Mathf.Abs(delta) - _stopDistance;

            if(allowed <= 0.001f)
                return false;

            var step = speed * Time.fixedDeltaTime;

            if(step > allowed)
                speed = allowed / Time.fixedDeltaTime;

            return true;
        }

        private Vector2 GetPathCheckPosition(float direction, float speed)
        {
            var sign = Sign(direction);
            var local = _pathChecker.localPosition;
            var ahead = Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(speed) * Time.fixedDeltaTime);

            return (Vector2)transform.position + new Vector2(ahead * sign, local.y);
        }

        private static float Sign(float direction)
        {
            if(Mathf.Approximately(direction, 0f))
                return 0f;

            return direction < 0f ? -1f : 1f;
        }
    }
}
