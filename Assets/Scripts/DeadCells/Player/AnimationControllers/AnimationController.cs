using System;
using DeadCells.Player.States;
using UnityEngine;

namespace DeadCells.Player.AnimationControllers
{
    public abstract class AnimationController : MonoBehaviour
    {
        public event Action AnimationCompleted;
        public event Action AnimationActionInvoked;

        public abstract int StateValue { get; }

        public abstract void PlayAnimation(PlayerState state, int value = 0, bool restart = false);
        public abstract void PlayHurt();

        public virtual void AnimationEnded() => InvokeAnimationCompleted();
        public void OnAnimationActionInvoked() => AnimationActionInvoked?.Invoke();

        protected void InvokeAnimationCompleted() => AnimationCompleted?.Invoke();
    }
}
