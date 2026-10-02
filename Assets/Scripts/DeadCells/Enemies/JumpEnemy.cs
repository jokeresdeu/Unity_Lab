using UnityEngine;

namespace DeadCells.Enemies
{
    public class JumpEnemy : BaseEnemy
    {
        [Header("Attack")]
        [SerializeField] private float _attackDistance = 1.5f;

        [Header("Heavy Attack")]
        [SerializeField] private float _jumpSpeed = 8;

        private bool _leftGround;
        private float _airborneDelay;
        private float _flightTimer;

        protected override void SwitchState()
        {
            switch(State)
            {
                case EnemyState.HeavyAttack:
                    UpdateHeavyAttack();
                    break;

                default:
                    base.SwitchState();
                    break;
            }
        }

        protected override void UpdatePatrol()
        {
            if(TryDetectPlayer())
            {
                if(PlayerDistance <= _attackDistance)
                {
                    UpdateDirection(DirectionToPlayer);
                    StartRecover();
                }
                else
                    StartHeavyAttack();

                return;
            }

            if(!TryTurnAround(_patrolSpeed))
                return;

            Move(_faceDirection, _patrolSpeed);
        }

        protected override void StartAttack()
        {
            if(PlayerDistance > _attackDistance)
            {
                StartHeavyAttack();
                return;
            }

            base.StartAttack();
        }

        private void StartHeavyAttack()
        {
            var direction = DirectionToPlayer;
            var target = GetJumpTarget(direction);

            if(Mathf.Abs(target.x - Rigidbody.position.x) < 0.2f)
            {
                base.StartAttack();
                return;
            }

            _leftGround = false;
            _airborneDelay = 0.15f;
            _flightTimer = Vector2.Distance(target, Rigidbody.position) / _jumpSpeed;

            UpdateDirection(direction);
            LaunchTo(target, _jumpSpeed);
            SetState(EnemyState.HeavyAttack);

            Debug.Log($"{name}: Heavy Attack");
        }

        private void UpdateHeavyAttack()
        {
            if(!IsGrounded)
                _leftGround = true;

            if(!_leftGround)
            {
                _airborneDelay -= Time.fixedDeltaTime;

                if(_airborneDelay > 0f)
                    return;

                Rigidbody.linearVelocity = Vector2.zero;
                StartRecover();
                return;
            }

            _flightTimer -= Time.fixedDeltaTime;

            var falling = Rigidbody.linearVelocity.y <= 0.05f;
            var landed = IsGrounded && falling;

            if(!landed && _flightTimer > -0.75f)
                return;

            Rigidbody.linearVelocity = Vector2.zero;
            StartRecover();
        }

        private Vector2 GetJumpTarget(float direction)
        {
            var standOff = Mathf.Max(0.2f, _attackDistance - 0.15f);
            var desired = new Vector2(PlayerPosition.x - direction * standOff, PlayerPosition.y);
            var origin = Rigidbody.position;

            if(HasGroundAt(desired))
                return desired;

            var steps = 8;

            for(var i = 1; i <= steps; i++)
            {
                var point = Vector2.Lerp(desired, origin, i / (float)steps);

                if(HasGroundAt(point))
                    return point;
            }

            return origin;
        }
    }
}
