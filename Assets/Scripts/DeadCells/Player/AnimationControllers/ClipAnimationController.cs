using DeadCells.Player.States;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DeadCells.Player.AnimationControllers
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class ClipAnimationController : AnimationController
    {
        [Min(0.01f)]
        [SerializeField] private float _speed = 1;

        [Header("Movement")]
        [SerializeField] private AnimationClip _idle;
        [SerializeField] private AnimationClip _run;
        [SerializeField] private AnimationClip _jump;
        [SerializeField] private AnimationClip _fall;
        [SerializeField] private AnimationClip _ladder;
        [SerializeField] private AnimationClip _wallRun;
        [SerializeField] private AnimationClip _wallHold;
        [SerializeField] private AnimationClip _wallSlide;
        [SerializeField] private AnimationClip _ledgeClimb;
        [SerializeField] private AnimationClip _crouch;
        [SerializeField] private AnimationClip _roll;

        [Header("Combat")]
        [SerializeField] private AnimationClip _lightAttack;
        [SerializeField] private AnimationClip _heavyAttack;
        [SerializeField] private AnimationClip _staggered;
        [SerializeField] private AnimationClip _death;
        [SerializeField] private AnimationClip _hurt;

        private PlayableGraph _graph;
        private AnimationLayerMixerPlayable _mixer;
        private AnimationClipPlayable _animation;
        private PlayerState _state;
        private int _stateValue;
        private bool _hasAnimation;
        private float _completionTime;

        public override int StateValue => _stateValue;
        public override void PlayHurt()
        {
            if(_hurt == null)
                return;

            _state = PlayerState.Hurt;
            _stateValue = 0;
            _hasAnimation = true;
            SetClip(0, _hurt, ref _animation);
            _completionTime = _hurt.length / GetSpeed();
        }

        public override void AnimationEnd() { }

        private void Awake()
        {
            var animator = GetComponent<Animator>();
            _graph = PlayableGraph.Create(name);
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _mixer = AnimationLayerMixerPlayable.Create(_graph, 1);
            AnimationPlayableOutput.Create(_graph, name, animator).SetSourcePlayable(_mixer);
            _graph.Play();
        }

        public override void PlayAnimation(PlayerState state, int value = 0, bool restart = false)
        {
            if(_hasAnimation && !restart && state == _state && value == _stateValue)
                return;

            _state = state;
            _stateValue = value;
            _hasAnimation = true;
            var clip = GetClip(state, value);

            if(clip == null)
                return;

            SetClip(0, clip, ref _animation);
            _completionTime = IsLooping(state, value) ? 0 : clip.length / GetSpeed();
        }

        private void Update()
        {
            if(_completionTime <= 0)
                return;

            _completionTime -= Time.deltaTime;

            if(_completionTime <= 0)
                InvokeAnimationCompleted();
        }

        private void SetClip(int input, AnimationClip clip, ref AnimationClipPlayable playable)
        {
            if(playable.IsValid())
            {
                _mixer.DisconnectInput(input);
                _graph.DestroyPlayable(playable);
            }

            playable = AnimationClipPlayable.Create(_graph, clip);
            playable.SetSpeed(GetSpeed());
            _graph.Connect(playable, 0, _mixer, input);
            _mixer.SetInputWeight(input, 1);
        }

        private AnimationClip GetClip(PlayerState state, int value)
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
                case PlayerState.Attack:
                    return value == 0 ? _lightAttack : _heavyAttack;
                case PlayerState.Hurt:
                    return value == 2 ? _death : value == 1 ? _staggered : _hurt;
                default:
                    return null;
            }
        }

        private static bool IsLooping(PlayerState state, int value)
        {
            return state is PlayerState.Ground or PlayerState.Air or PlayerState.Ladder or PlayerState.Wall ||
                   state == PlayerState.Low && value == 0;
        }

        private float GetSpeed() => Mathf.Max(Mathf.Abs(_speed), Mathf.Epsilon);

        private void OnDestroy()
        {
            if(_graph.IsValid())
                _graph.Destroy();
        }
    }
}
