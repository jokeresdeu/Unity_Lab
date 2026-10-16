using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.Enemies.States
{
    [DisallowMultipleComponent]
    public sealed class EnemyAttackState : BaseEnemyState
    {
        [Header("Range")]
        [SerializeField] private float _minDistance;
        [SerializeField] private float _maxDistance = 1.5f;

        [Header("Attack")]
        [SerializeField] private bool _facePlayer = true;
        [SerializeField] private List<AttackVariant> _variants = new();

        private AttackVariant _variant;
        private int _actionCount;
        private bool _facingLocked;

        public override EnemyState Id => EnemyState.Attack;
        public override int ActivationPriority => 50;
        protected override int AnimationValue => _variant != null ? _variant.AnimationValue : 0;

        public override bool CanBeActivated(EnemyState? currentState)
        {
            if(currentState == Id)
                return !IsComplete;

            return SeesPlayer && IsInRange();
        }

        public override void EnterState()
        {
            _variant = PickVariant();
            _actionCount = 0;
            _facingLocked = false;
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
            _variant = null;
            _actionCount = 0;
            _facingLocked = false;
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

            if(_actionCount != 2)
                return;

            Debug.Log($"{name}: Attack value={AnimationValue} damage={(_variant != null ? _variant.Damage : 0f)}");
        }

        private void OnAnimationEnded() => Complete();

        private AttackVariant PickVariant()
        {
            if(_variants == null || _variants.Count == 0)
                return null;

            var total = 0f;
            foreach(var variant in _variants)
            {
                if(variant != null && variant.Weight > 0f)
                    total += variant.Weight;
            }

            if(total <= 0f)
                return _variants[0];

            var roll = UnityEngine.Random.Range(0f, total);
            var accumulated = 0f;
            foreach(var variant in _variants)
            {
                if(variant == null || variant.Weight <= 0f)
                    continue;

                accumulated += variant.Weight;
                if(roll <= accumulated)
                    return variant;
            }

            return _variants[0];
        }

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
            DrawForwardRangeGizmo(_minDistance, _maxDistance, Color.red);
        }

        [Serializable]
        public sealed class AttackVariant
        {
            [SerializeField] private int _animationValue;
            [SerializeField] private float _damage = 10;
            [SerializeField] private float _weight = 1;

            public int AnimationValue => _animationValue;
            public float Damage => _damage;
            public float Weight => _weight;
        }
    }
}
