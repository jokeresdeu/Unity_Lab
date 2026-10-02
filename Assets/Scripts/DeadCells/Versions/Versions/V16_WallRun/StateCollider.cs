using System;
using UnityEngine;

namespace DeadCells.Versions.Versions.V16_WallRun
{
    [Serializable]
    public class StateCollider
    {
        [SerializeField] private ColliderState _state;
        [field: SerializeField] public Collider2D Collider { get; private set; }
        public bool SetActive(ColliderState state) => Collider.enabled = state == _state;
    }

    public enum ColliderState
    {
        Stand = 0,
        Crouch = 1,
        Roll = 2
    }
}
