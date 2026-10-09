using DeadCells.Player.States;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace DeadCells.Player.AnimationControllers
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkeletonAnimation))]
    public sealed class SpineAnimationController : AnimationController
    {
        [Header("Movement")]
        [SerializeField, SpineAnimation] private string _idle;
        [SerializeField, SpineAnimation] private string _run;
        [SerializeField, SpineAnimation] private string _jump;
        [SerializeField, SpineAnimation] private string _fall;
        [SerializeField, SpineAnimation] private string _ladder;
        [SerializeField, SpineAnimation] private string _wallRun;
        [SerializeField, SpineAnimation] private string _wallHold;
        [SerializeField, SpineAnimation] private string _wallSlide;
        [SerializeField, SpineAnimation] private string _ledgeClimb;
        [SerializeField, SpineAnimation] private string _crouch;
        [SerializeField, SpineAnimation] private string _roll;

        [Header("Combat")]
        [SerializeField, SpineAnimation] private string _lightAttack;
        [SerializeField, SpineAnimation] private string _heavyAttack;
        [SerializeField, SpineAnimation] private string _staggered;
        [SerializeField, SpineAnimation] private string _death;
        [SerializeField, SpineAnimation] private string _hurt;
        [SerializeField] private string _animationActionEvent = "AnimationAction";

        private SkeletonAnimation _skeletonAnimation;
        private TrackEntry _currentAnimation;
        private PlayerState _state;
        private int _stateValue;
        private bool _hasAnimation;

        public override int StateValue => _stateValue;

        private void Awake()
        {
            _skeletonAnimation = GetComponent<SkeletonAnimation>();
            _skeletonAnimation.Initialize(false);
            _skeletonAnimation.AnimationState.Event += OnAnimationEvent;
        }

        public override void PlayAnimation(PlayerState state, int value = 0, bool restart = false)
        {
            if(_hasAnimation && !restart && state == _state && value == _stateValue)
                return;

            _state = state;
            _stateValue = value;
            _hasAnimation = true;
            var animationName = GetAnimationName(state, value);

            if(string.IsNullOrEmpty(animationName))
                return;

            if(_currentAnimation != null)
                _currentAnimation.Complete -= OnAnimationCompleted;

            var loop = IsLooping(state, value);
            _currentAnimation = _skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);

            if(!loop)
                _currentAnimation.Complete += OnAnimationCompleted;
        }

        public override void PlayHurt()
        {
            if(!string.IsNullOrEmpty(_hurt))
                _skeletonAnimation.AnimationState.SetAnimation(1, _hurt, false).Complete += OnHurtCompleted;
        }

        private string GetAnimationName(PlayerState state, int value)
        {
            switch(state)
            {
                case PlayerState.Ground:
                    return value == 0 ? _idle : _run;
                case PlayerState.Air:
                    return value > 0 ? _jump : _fall;
                case PlayerState.Ladder:
                    return _ladder;
                case PlayerState.Wall:
                    return value > 0 ? _wallRun : value == 0 ? _wallHold : _wallSlide;
                case PlayerState.LedgeClimb:
                    return _ledgeClimb;
                case PlayerState.Low:
                    return value == 0 ? _crouch : _roll;
                case PlayerState.LightAttack:
                    return _lightAttack;
                case PlayerState.HeavyAttack:
                    return _heavyAttack;
                case PlayerState.Staggered:
                    return _staggered;
                case PlayerState.Death:
                    return _death;
                default:
                    return null;
            }
        }

        private static bool IsLooping(PlayerState state, int value)
        {
            return state is PlayerState.Ground or PlayerState.Air or PlayerState.Ladder or PlayerState.Wall ||
                   state == PlayerState.Low && value == 0;
        }

        private void OnAnimationCompleted(TrackEntry trackEntry) => InvokeAnimationCompleted();
        private void OnHurtCompleted(TrackEntry trackEntry) => _skeletonAnimation.AnimationState.ClearTrack(1);

        private void OnAnimationEvent(TrackEntry trackEntry, Spine.Event animationEvent)
        {
            if(animationEvent.Data.Name == _animationActionEvent)
                OnAnimationActionInvoked();
        }

        private void OnDestroy()
        {
            if(_skeletonAnimation != null && _skeletonAnimation.AnimationState != null)
                _skeletonAnimation.AnimationState.Event -= OnAnimationEvent;
        }
    }
}
