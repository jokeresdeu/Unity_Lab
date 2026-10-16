using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.Enemies.States
{
    [Serializable]
    public sealed class StateData
    {
        [SerializeField] private EnemyState _state;
        [SerializeField] private float _cooldown;
        [SerializeField] private float _recoverTime;
        [SerializeField] private List<WeightedTransition> _weightedTransitions = new();
        [SerializeField] private List<EnemyState> _forbiddenStates = new();
        [SerializeField] private List<EnemyState> _cooldownedStates = new();

        public EnemyState State => _state;
        public float Cooldown => _cooldown;
        public float RecoverTime => _recoverTime;
        public IReadOnlyList<EnemyState> CooldownedStates => _cooldownedStates;

        public bool CanTransit(EnemyState to)
        {
            foreach(var state in _forbiddenStates)
            {
                if(state == to)
                    return false;
            }

            return true;
        }

        public EnemyState Pick(ICollection<EnemyState> candidates)
        {
            if(_weightedTransitions.Count == 0 || candidates == null || candidates.Count == 0)
                return _state;

            var totalWeight = 0f;
            foreach(var option in _weightedTransitions)
            {
                if(option == null || option.Weight <= 0f || !candidates.Contains(option.State))
                    continue;

                totalWeight += option.Weight;
            }

            if(totalWeight <= 0f)
                return _state;

            var roll = UnityEngine.Random.Range(0f, totalWeight);
            var accumulated = 0f;
            foreach(var option in _weightedTransitions)
            {
                if(option == null || option.Weight <= 0f || !candidates.Contains(option.State))
                    continue;

                accumulated += option.Weight;
                if(roll <= accumulated)
                    return option.State;
            }

            return _state;
        }
    }
}
