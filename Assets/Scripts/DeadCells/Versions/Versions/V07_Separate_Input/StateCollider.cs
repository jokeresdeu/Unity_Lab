using System;
using UnityEngine;

namespace DeadCells.Versions.V07
{
    [Serializable]
    public class StateCollider
    {
        [SerializeField] private ColliderState _state;
        [field: SerializeField] public Collider2D Collider { get; private set; }

        public bool SetActive(ColliderState state)
        {
            Collider.enabled = state == _state;
            return Collider.enabled;
        }
    }

    public enum ColliderState
    {
        Stand = 0,
        Crouch = 1,
        Roll = 2
    }
}
