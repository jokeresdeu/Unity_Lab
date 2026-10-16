using DeadCells.Enemies;
using UnityEngine;

namespace DeadCells.Enemies.States
{
    [DisallowMultipleComponent]
    public sealed class EnemySpecialAttackState : BaseEnemyState
    {
        [Header("Range")]
        [SerializeField] private float _minDistance;
        [SerializeField] private float _maxDistance = 6;

        [Header("Attack")]
        [SerializeField] private float _hitDistance = 1.2f;
        [SerializeField] private float _hitDamage = 10;
        [SerializeField] private EnemyProjectile _projectile;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private float _projectileSpeed = 8;
        [SerializeField] private float _projectileDamage = 10;
        [SerializeField] private bool _facePlayer = true;

        private int _actionCount;
        private bool _facingLocked;
        private bool _launched;

        public override EnemyState Id => EnemyState.SpecialAttack;
        public override int ActivationPriority => 50;

        public override bool CanBeActivated(EnemyState? currentState)
        {
            if(currentState == Id)
                return !IsComplete;

            return SeesPlayer && IsInRange();
        }

        public override void EnterState()
        {
            _actionCount = 0;
            _facingLocked = false;
            _launched = false;
            base.EnterState();
            Stop();

            if(_facePlayer)
                Face(DirectionToPlayer);

            AnimationController.AnimationActionInvoked += OnAnimationAction;
            AnimationController.AnimationCompleted += OnAnimationEnded;
        }

        public override void UpdateState()
        {
            Stop();
            if(_facePlayer && !_facingLocked && Player != null)
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
            _actionCount = 0;
            _facingLocked = false;
            _launched = false;
            base.ResetState();
        }

        private void OnAnimationAction()
        {
            _actionCount++;
            if(_actionCount == 1)
            {
                _facingLocked = true;
                return;
            }

            if(_actionCount != 2 || _launched)
                return;

            Debug.Log($"{name}: SpecialAttack hitDamage={_hitDamage} projectileDamage={_projectileDamage}");
            LaunchProjectile();
        }

        private void LaunchProjectile()
        {
            if(_projectile == null || _launched)
                return;

            _launched = true;

            var origin = _muzzle != null ? (Vector2)_muzzle.position : (Vector2)transform.position;
            var projectile = Instantiate(_projectile, origin, Quaternion.identity);
            projectile.Launch(Facing, _projectileSpeed, _projectileDamage);
        }

        private void OnAnimationEnded() => Complete();

        private void UnbindAnimation()
        {
            if(AnimationController == null)
                return;

            AnimationController.AnimationActionInvoked -= OnAnimationAction;
            AnimationController.AnimationCompleted -= OnAnimationEnded;
        }

        private bool IsInRange() => PlayerDistance >= _minDistance && PlayerDistance <= _maxDistance;

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            DrawForwardRangeGizmo(_minDistance, _maxDistance, new Color(0.7f, 0.2f, 1f));
            DrawForwardRangeGizmo(0f, _hitDistance, Color.red);
        }
    }
}
