using UnityEngine;

namespace TailsOfIron.Enemies
{
    public class SlamAttackEnemy : PatrolEnemy
    {
        [SerializeField] private float _detectionDistance;
        [SerializeField] private float _loseTargetDistance;

        [Header("Rise")]
        [SerializeField] private float _riseHeight;
        [SerializeField] private float _riseSpeed;
        [SerializeField] private float _airHorizontalSpeed;

        [Header("Slam")]
        [SerializeField] private float _slamTime;

        [Header("Backstep")]
        [SerializeField] private Vector2 _backstepImpulse;

        private float _riseTargetY;
        private float _backstepDirection;
        private bool _leftGround;

        protected override void UpdateState()
        {
            switch(State)
            {
                case EnemyState.Patrol:
                    if(TryDetectPlayer(_detectionDistance))
                        SetState(EnemyState.Rise);
                    else
                        Patrol();
                    break;

                case EnemyState.Rise:
                    Rigidbody.linearVelocityX =
                        DirectionToPlayer * _airHorizontalSpeed;

                    Rigidbody.linearVelocityY = _riseSpeed;

                    UpdateDirection(DirectionToPlayer);

                    if(transform.position.y >= _riseTargetY)
                        SetState(EnemyState.Slam);
                    break;

                case EnemyState.Slam:
                    if(!IsGrounded())
                        _leftGround = true;

                    if(_leftGround && IsGrounded())
                        SetState(EnemyState.Stuck);
                    break;

                case EnemyState.Backstep:
                    if(IsGrounded() && !HasGroundAhead(_backstepDirection))
                        Stop();
                    break;

                case EnemyState.Recover:
                    if(PlayerOutOfRange(_loseTargetDistance))
                    {
                        ForgetPlayer();
                        SetState(EnemyState.Patrol);
                    }
                    break;
            }
        }

        protected override void OnStateStarted(EnemyState state)
        {
            switch(state)
            {
                case EnemyState.Rise:
                    _riseTargetY = transform.position.y + _riseHeight;
                    UpdateDirection(DirectionToPlayer);
                    break;

                case EnemyState.Slam:
                    UpdateDirection(DirectionToPlayer);
                    LaunchTo(PlayerPosition, _slamTime);

                    _leftGround = false;
                    Attack("Slam Attack");
                    break;

                case EnemyState.Stuck:
                    Stop();
                    Rigidbody.linearVelocityY = 0;
                    break;

                case EnemyState.Backstep:
                    Stop();
                    UpdateDirection(DirectionToPlayer);

                    _backstepDirection = -DirectionToPlayer;

                    Rigidbody.AddForce(
                        new Vector2(
                            _backstepDirection * _backstepImpulse.x,
                            _backstepImpulse.y),
                        ForceMode2D.Impulse);
                    break;

                case EnemyState.Recover:
                    Stop();
                    UpdateDirection(DirectionToPlayer);
                    break;
            }
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            DrawDetectionGizmos(_detectionDistance);
        }
    }
}