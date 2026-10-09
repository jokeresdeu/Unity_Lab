using DeadCells.Player.States;
using UnityEngine;

namespace DeadCells.Player.AnimationControllers
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class ParametredAnimationController : AnimationController
    {
        private static readonly int StateParameter = Animator.StringToHash("State");
        private static readonly int StateValueParameter = Animator.StringToHash("StateValue");
        private static readonly int HurtParameter = Animator.StringToHash("Hurt");

        private Animator _animator;
        private bool _restartRequested;
        private PlayerState _restartState;

        public override int StateValue => _animator.GetInteger(StateValueParameter);

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.updateMode = AnimatorUpdateMode.Normal;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        public override void PlayAnimation(PlayerState state, int value = 0, bool restart = false)
        {
            if(restart && state == PlayerState.Low)
                restart = _animator.GetInteger(StateParameter) == (int)state &&
                          _animator.GetInteger(StateValueParameter) == value;

            _animator.SetInteger(StateParameter, (int)state);
            _animator.SetInteger(StateValueParameter, value);

            if(restart)
            {
                _restartRequested = true;
                _restartState = state;
            }
            else if(state != _restartState)
                _restartRequested = false;
        }

        public override void PlayHurt() => _animator.SetTrigger(HurtParameter);

        private void LateUpdate()
        {
            if(!_restartRequested)
                return;

            _restartRequested = false;
            var currentAnimation = _animator.IsInTransition(0)
                ? _animator.GetNextAnimatorStateInfo(0)
                : _animator.GetCurrentAnimatorStateInfo(0);
            _animator.Play(currentAnimation.fullPathHash, 0, 0);
        }

        private void OnDisable() => _restartRequested = false;
    }
}
