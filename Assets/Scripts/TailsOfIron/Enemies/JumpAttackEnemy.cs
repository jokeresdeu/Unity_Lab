using UnityEngine;

namespace TailsOfIron.Enemies
{
    public class JumpAttackEnemy : PatrolEnemy
    {
        [SerializeField] private float _detectionDistance;
        [SerializeField] private float _loseTargetDistance;

        [Header("Retreat")]
        [SerializeField] private float _retreatStartSpeed;
        [SerializeField] private float _retreatDeceleration;

        [Header("Jump")]
        [SerializeField] private float _jumpTime;

        private bool _leftGround;

        protected override void UpdateState()
        {
            switch(State)
            {
                case EnemyState.Patrol:
                    if(TryDetectPlayer(_detectionDistance))
                        SetState(EnemyState.Retreat);
                    else
                        Patrol();
                    break;

                case EnemyState.Retreat:
                    if(PlayerOutOfRange(_loseTargetDistance))
                    {
                        ForgetPlayer();
                        SetState(EnemyState.Patrol);
                        return;
                    }

                    DecelerationMove(
                        -DirectionToPlayer,
                        _retreatDeceleration,
                        false);
                    break;

                case EnemyState.Jump:
                    if(!IsGrounded())
                        _leftGround = true;

                    if(_leftGround && IsGrounded())
                        SetState(EnemyState.Recover);
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
                case EnemyState.Retreat:
                    StartDecelerationMove(_retreatStartSpeed);
                    UpdateDirection(DirectionToPlayer);
                    break;

                case EnemyState.Jump:
                    UpdateDirection(DirectionToPlayer);
                    LaunchTo(PlayerPosition, _jumpTime);

                    _leftGround = false;
                    Attack("Jump Attack");
                    break;

                case EnemyState.Recover:
                    Stop();
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