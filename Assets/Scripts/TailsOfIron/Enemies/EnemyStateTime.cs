using System;
using System.Collections.Generic;
using UnityEngine;

namespace TailsOfIron.Enemies
{
    [Serializable]
    public class EnemyStateTime
    {
        [field: SerializeField] public EnemyState State { get; private set; }
        [field: SerializeField] public float Time { get; private set; }
        [field: SerializeField] public List<EnemyState> NextStates { get; private set; } = new();
    }
}