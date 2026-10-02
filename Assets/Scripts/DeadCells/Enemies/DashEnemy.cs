using UnityEngine;

namespace DeadCells.Enemies
{
    public class DashEnemy : BaseEnemy
    {
        [Header("Ragged Movement")]
        [SerializeField] private float _moveMinTime = 0.15f;
        [SerializeField] private float _moveMaxTime = 0.4f;
        [SerializeField] private float _stopMinTime = 0.05f;
        [SerializeField] private float _stopMaxTime = 0.15f;

        [Header("Dash")]
        [SerializeField] private float _dashSpeed = 8;

        private bool _isMoving;
        private float _movementTimer;
        private float _attackDirection;

        protected override void Awake()
        {
            base.Awake();
            StartMove();
        }

        protected override void UpdatePatrol()
        {
            if(TryDetectPlayer())
            {
                StartAttack();
                return;
            }

            var canPatrol = TryTurnAround(_patrolSpeed);
            UpdateRaggedMovement(canPatrol);
        }

        protected override void StartAttack()
        {
            _attackDirection = DirectionToPlayer;
            base.StartAttack();
        }

        protected override void UpdateAttack()
        {
            if(!CanMove(_attackDirection, _dashSpeed))
            {
                FinishAttack();
                return;
            }

            Move(_attackDirection, _dashSpeed, false);
            base.UpdateAttack();
        }

        private void UpdateRaggedMovement(bool canPatrol)
        {
            _movementTimer -= Time.fixedDeltaTime;

            if(_movementTimer <= 0)
            {
                if(_isMoving)
                    StartStop();
                else
                    StartMove();
            }

            if(_isMoving && canPatrol)
                Move(_faceDirection, _patrolSpeed);
            else
                Stop();
        }

        private void StartMove()
        {
            _isMoving = true;
            _movementTimer = Random.Range(_moveMinTime, _moveMaxTime);
        }

        private void StartStop()
        {
            _isMoving = false;
            _movementTimer = Random.Range(_stopMinTime, _stopMaxTime);
        }
    }
}
