using UnityEngine;

namespace TailsOfIron.Enemies
{
    public class DashAttackEnemy : PatrolEnemy
    {
        [SerializeField] private float _detectionDistance;
        [SerializeField] private float _loseTargetDistance;

        [Header("Dash")]
        [SerializeField] private float _dashStartSpeed;
        [SerializeField] private float _dashAcceleration;

        [Header("Backstep")]
        [SerializeField] private float _backstepImpulse;

        private float _dashDirection;
        private float _dashSpeed;
        private float _backstepDirection;

        protected override void UpdateState()
        {
            switch(State)
            {
                case EnemyState.Patrol:
                    if(TryDetectPlayer(_detectionDistance))
                        SetState(EnemyState.Prepare);
                    else
                        Patrol();
                    break;

                case EnemyState.Prepare:
                    if(PlayerOutOfRange(_loseTargetDistance))
                    {
                        ForgetPlayer();
                        SetState(EnemyState.Patrol);
                    }
                    break;

                case EnemyState.Dash:
                    _dashSpeed += _dashAcceleration * Time.fixedDeltaTime;

                    if(!TryMove(_dashDirection, _dashSpeed, false))
                        Stop();
                    break;

                case EnemyState.Backstep:
                    if(!HasGroundAhead(_backstepDirection))
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
                case EnemyState.Prepare:
                    Stop();
                    UpdateDirection(DirectionToPlayer);
                    break;

                case EnemyState.Dash:
                    _dashDirection = DirectionToPlayer;
                    _dashSpeed = _dashStartSpeed;

                    UpdateDirection(_dashDirection);
                    Attack("Dash Attack");
                    break;

                case EnemyState.Backstep:
                    Stop();
                    UpdateDirection(DirectionToPlayer);

                    _backstepDirection = -DirectionToPlayer;

                    if(HasGroundAhead(_backstepDirection))
                    {
                        Rigidbody.AddForce(
                            Vector2.right * _backstepDirection * _backstepImpulse,
                            ForceMode2D.Impulse);
                    }
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