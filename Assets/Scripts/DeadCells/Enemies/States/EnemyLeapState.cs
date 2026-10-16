using System.Collections;
using UnityEngine;

namespace DeadCells.Enemies.States
{
    [DisallowMultipleComponent]
    public sealed class EnemyLeapState : EnemyMovementState
    {
        private const int ArcPoints = 16;
        private const float MinHorizontal = 0.75f;

        [Header("Range")]
        [SerializeField] private float _minDistance = 1.5f;
        [SerializeField] private float _maxDistance = 8;

        [Header("Leap")]
        [SerializeField] private float _verticalLift = 1.2f;
        [SerializeField] private float _chargeTime = 0.4f;
        [SerializeField] private float _launchTime = 0.6f;
        [SerializeField] private bool _landOnPlayer;

        [Header("Ground")]
        [SerializeField] private Transform _groundChecker;
        [SerializeField] private Vector2 _groundCheckRect = new(0.5f, 0.15f);
        [SerializeField] private LayerMask _ground;

        private int _phase;
        private int _animationValue;
        private Vector2 _target;
        private readonly Vector2[] _arc = new Vector2[ArcPoints];

        public override EnemyState Id => EnemyState.HeavyAttack;
        public override int ActivationPriority => 50;
        protected override int AnimationValue => _animationValue;

        public override bool CanBeActivated(EnemyState? currentState)
        {
            if(currentState == Id)
                return !IsComplete;

            return SeesPlayer && IsInRange();
        }

        public override void EnterState()
        {
            _phase = 0;
            _animationValue = 0;
            base.EnterState();
            Stop();
            _target = Rigidbody.position + Vector2.right * Facing;
            RememberTarget();
            StartCoroutine(Run());
        }

        public override void UpdateState()
        {
            if(_phase == 0)
                Stop();
        }

        public override void ResetState()
        {
            _phase = 0;
            _animationValue = 0;
            base.ResetState();
        }

        private IEnumerator Run()
        {
            var remaining = Mathf.Max(0f, _chargeTime);
            while(remaining > 0f)
            {
                Stop();
                RememberTarget();

                yield return new WaitForFixedUpdate();
                remaining -= Time.fixedDeltaTime;
            }

            yield return Fly();
        }

        private IEnumerator Fly()
        {
            if(_phase != 0)
                yield break;

            RememberTarget();
            Face(_target.x - Rigidbody.position.x);
            var from = Rigidbody.position;
            var to = GetLeapTarget(from);
            var span = Mathf.Abs(to.x - from.x);
            var height = Mathf.Min(Mathf.Max(0f, _verticalLift), span * 0.45f);
            BuildArc(from, to, height);

            _phase = 1;
            Rigidbody.gravityScale = 0f;

            var duration = Mathf.Max(0.05f, _launchTime);
            var elapsed = 0f;
            var previous = Rigidbody.position;
            while(elapsed < duration)
            {
                elapsed += Time.fixedDeltaTime;
                var next = SampleArc(Mathf.Clamp01(elapsed / duration));
                var velocity = Time.fixedDeltaTime > 0f
                    ? (next - previous) / Time.fixedDeltaTime
                    : Vector2.zero;
                Rigidbody.linearVelocity = velocity;
                Rigidbody.position = next;
                previous = next;
                SetVerticalAnimation(velocity.y);
                yield return new WaitForFixedUpdate();
            }

            Rigidbody.position = _arc[ArcPoints - 1];
            Land();
        }

        private void RememberTarget()
        {
            if(Player == null)
                return;

            _target = _landOnPlayer ? PlayerPosition : GetFrontOfPlayer();
            Face(DirectionToPlayer);
        }

        private Vector2 GetLeapTarget(Vector2 from)
        {
            if(_landOnPlayer)
                return _target;

            var to = _target;
            if(Mathf.Abs(to.x - from.x) >= MinHorizontal)
                return to;

            to.x = from.x + Facing * MinHorizontal;
            return to;
        }

        private void BuildArc(Vector2 from, Vector2 to, float height)
        {
            for(var i = 0; i < ArcPoints; i++)
            {
                var t = i / (ArcPoints - 1f);
                var point = Vector2.Lerp(from, to, t);
                point.y += 4f * height * t * (1f - t);
                _arc[i] = point;
            }
        }

        private Vector2 SampleArc(float t)
        {
            var scaled = t * (ArcPoints - 1);
            var index = Mathf.Min(Mathf.FloorToInt(scaled), ArcPoints - 2);
            return Vector2.Lerp(_arc[index], _arc[index + 1], scaled - index);
        }

        private void SetVerticalAnimation(float velocityY)
        {
            var value = velocityY > 0.01f ? 1 : velocityY < -0.01f ? -1 : 0;
            if(value == _animationValue)
                return;

            _animationValue = value;
            PlayAnimation();
        }

        private void Land()
        {
            Rigidbody.linearVelocity = Vector2.zero;
            _animationValue = 0;
            Complete();
        }

        private bool IsInRange() => PlayerDistance >= _minDistance && PlayerDistance <= _maxDistance;

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            DrawForwardRangeGizmo(_minDistance, _maxDistance, Color.red);
            DrawLiftRayGizmo();

            if(_groundChecker == null)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_groundChecker.position, _groundCheckRect);
        }

        private void DrawLiftRayGizmo()
        {
            if(_verticalLift <= 0f)
                return;

            var origin = (Vector2)transform.position;
            var hit = Physics2D.Raycast(origin, Vector2.up, _verticalLift, _ground);
            var end = hit.collider != null ? hit.point : origin + Vector2.up * _verticalLift;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, end);
        }
    }
}
