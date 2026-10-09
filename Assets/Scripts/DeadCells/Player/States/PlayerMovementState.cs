using System.Collections.Generic;
using DeadCells.Player.AnimationControllers;
using UnityEngine;

namespace DeadCells.Player.States
{
    public abstract class PlayerMovementState : MonoBehaviour
    {
        protected PlayerInput Input;
        protected Rigidbody2D Rigidbody;
        protected AnimationController AnimationController;
        protected readonly List<Collider2D> Overlaps = new (8);
        
        private float _initialGravityScale;

        public abstract PlayerState Id { get; }
        public abstract int ActivationPriority { get; }
        public virtual int AnimationValue => 0;

        public virtual void Initialize(PlayerInput input, Rigidbody2D rigidbody, AnimationController animationController)
        {
            Input = input;
            Rigidbody = rigidbody;
            AnimationController = animationController;
            _initialGravityScale = rigidbody.gravityScale;
        }

        public virtual void EnterState() => Rigidbody.gravityScale = _initialGravityScale;
        public abstract bool CanBeActivated(PlayerState? currentState);
        public abstract void UpdateState();
        public virtual void EndState() => Rigidbody.gravityScale = _initialGravityScale;
        public virtual void ResetState() { }

        protected void FindColliders(Transform checker, Vector2 size, LayerMask mask, bool triggers = false)
        {
            Overlaps.Clear();

            if(checker == null)
                return;

            FindColliders(checker.position, size, mask, triggers);
        }

        protected void FindColliders(Vector2 position, Vector2 size, LayerMask mask, bool triggers = false)
        {
            var filter = new ContactFilter2D();
            filter.SetLayerMask(mask);
            filter.useTriggers = triggers;
            Physics2D.OverlapBox(position, size, 0, filter, Overlaps);

            for(var i = Overlaps.Count - 1; i >= 0; i--)
            {
                if(Overlaps[i].attachedRigidbody == Rigidbody)
                    Overlaps.RemoveAt(i);
            }
        }
    }
}
