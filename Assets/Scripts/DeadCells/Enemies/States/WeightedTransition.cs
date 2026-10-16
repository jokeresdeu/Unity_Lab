using System;
using UnityEngine;

namespace DeadCells.Enemies.States
{
    [Serializable]
    public sealed class WeightedTransition
    {
        [SerializeField] private EnemyState _state;
        [SerializeField] private float _weight = 1f;

        public EnemyState State => _state;
        public float Weight => _weight;
    }
}
