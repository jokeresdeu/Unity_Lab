using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.Enemies.States
{
    [CreateAssetMenu(fileName = "EnemyStateGraph", menuName = "DeadCells/Enemies/State Graph")]
    public sealed class EnemyStateGraph : ScriptableObject
    {
        [SerializeField] private List<StateData> _states = new();

        public bool TryGet(EnemyState state, out StateData data)
        {
            foreach(var row in _states)
            {
                if(row != null && row.State == state)
                {
                    data = row;
                    return true;
                }
            }

            data = null;
            return false;
        }

        public bool CanTransit(EnemyState from, EnemyState to)
        {
            if(!TryGet(from, out var data))
                return true;

            return data.CanTransit(to);
        }

        public EnemyState Pick(EnemyState from, ICollection<EnemyState> candidates)
        {
            if(!TryGet(from, out var data))
                return from;

            return data.Pick(candidates);
        }
    }
}
