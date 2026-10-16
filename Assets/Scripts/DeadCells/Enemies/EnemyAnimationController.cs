using System;
using DeadCells.Enemies.States;
using UnityEngine;

namespace DeadCells.Enemies
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class EnemyAnimationController : MonoBehaviour
    {
        private static readonly int StateParameter = Animator.StringToHash("State");
        private static readonly int StateValueParameter = Animator.StringToHash("StateValue");
        private static readonly int HurtParameter = Animator.StringToHash("Hurt");

        [SerializeField] private Animator _animator;

        public event Action AnimationCompleted;
        public event Action AnimationActionInvoked;

        private bool _restartRequested;
        private EnemyState _restartState;
        private bool _endNotified;

        public void PlayAnimation(EnemyState state, int value = 0, bool restart = false)
        {
            if(_animator == null)
                return;

            _animator.SetInteger(StateParameter, (int)state);
            _animator.SetInteger(StateValueParameter, value);
            _endNotified = false;

            if(restart)
            {
                _restartRequested = true;
                _restartState = state;
            }
            else if(state != _restartState)
                _restartRequested = false;
        }

        public void PlayHurt()
        {
            if(_animator == null)
                return;

            _animator.SetTrigger(HurtParameter);
        }

        public void AnimationEnd() => NotifyCompleted();
        public void AnimationAction() => AnimationActionInvoked?.Invoke();

        private void NotifyCompleted()
        {
            if(_endNotified)
                return;

            _endNotified = true;
            AnimationCompleted?.Invoke();
        }

        private void Awake()
        {
            _animator.updateMode = AnimatorUpdateMode.Normal;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            BindEventRelay();
        }

        private void BindEventRelay()
        {
            if(_animator == null || _animator.gameObject == gameObject)
                return;

            var relay = _animator.GetComponent<EnemyAnimationEvents>();
            if(relay == null)
                relay = _animator.gameObject.AddComponent<EnemyAnimationEvents>();

            relay.Bind(this);
        }

        private void LateUpdate()
        {
            if(_animator == null || !_restartRequested)
                return;

            _restartRequested = false;
            var currentAnimation = _animator.IsInTransition(0)
                ? _animator.GetNextAnimatorStateInfo(0)
                : _animator.GetCurrentAnimatorStateInfo(0);
            _animator.Play(currentAnimation.fullPathHash, 0, 0);
        }

        private void OnDisable()
        {
            _restartRequested = false;
            _endNotified = false;
        }
    }
}
