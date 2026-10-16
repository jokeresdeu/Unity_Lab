using System;
using System.Collections.Generic;
using DeadCells.Enemies.States;
using UnityEngine;

namespace DeadCells.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        [SerializeField] private EnemyAnimationController _animationController;
        [SerializeField] private EnemyStateGraph _graph;

        [Header("Detection")]
        [SerializeField] private Vector2 _detectionSize = new(5f, 0.8f);
        [SerializeField] private LayerMask _playerLayer;
        [SerializeField] private LayerMask _occluders;

        private readonly Dictionary<EnemyState, BaseEnemyState> _states = new();
        private readonly List<BaseEnemyState> _activationCandidates = new();
        private readonly List<EnemyState> _allowedStates = new();
        private BaseEnemyState _currentState;
        private BaseEnemyState _requestedState;
        private Rigidbody2D _rigidbody;
        private Collider2D _body;
        private Transform _player;
        private Transform _rememberedPlayer;
        private float _rememberUntil;
        private float[] _lockedUntil;
        private EnemyState? _noRepeat;

        private float Facing => transform.forward.z >= 0f ? 1f : -1f;

        private void Awake()
        {
            if(_graph == null)
                throw new InvalidOperationException("EnemyBrain: state graph is missing.");

            _lockedUntil = new float[Enum.GetValues(typeof(EnemyState)).Length];
            _rigidbody = GetComponent<Rigidbody2D>();
            _body = GetComponent<Collider2D>();
            SetMovementLimits(out var movementMin, out var movementMax);

            foreach(var state in GetComponents<BaseEnemyState>())
            {
                if(!_states.TryAdd(state.Id, state))
                    throw new InvalidOperationException($"EnemyBrain: duplicate state {state.Id}.");

                state.Initialize(_rigidbody, _animationController);
                if(state is EnemyMovementState movement)
                    movement.SetMovementLimits(movementMin, movementMax);
            }
        }

        private void SetMovementLimits(out float min, out float max)
        {
            min = max = 0f;
            var inset = _body != null ? _body.bounds.extents.x * 0.9f : 0.2f;
            var origin = (Vector2)transform.position + Vector2.up * 0.1f;
            var hits = Physics2D.RaycastAll(origin, Vector2.down, 2f);

            foreach(var hit in hits)
            {
                var other = hit.collider;
                if(other == null || other.isTrigger || other.transform == transform || other.transform.IsChildOf(transform))
                    continue;

                min = other.bounds.min.x + inset;
                max = other.bounds.max.x - inset;
            }
        }

        private void OnEnable()
        {
            UpdateVision();
            CheckActivations();

            if(_requestedState == null)
                throw new InvalidOperationException("EnemyBrain: no initial state requested activation.");

            ChangeState(_requestedState);
        }

        private void OnDisable()
        {
            if(_currentState != null)
                _currentState.EndState();

            _currentState = null;
            _requestedState = null;
            _player = null;
            _rememberedPlayer = null;
            _rememberUntil = 0f;
            _noRepeat = null;
            if(_lockedUntil != null)
                Array.Clear(_lockedUntil, 0, _lockedUntil.Length);
            SetPlayer(null);

            foreach(var state in _states.Values)
                state.ResetState();
        }

        private void FixedUpdate()
        {
            UpdateVision();
            CheckActivations();
            var stateChanged = _requestedState != null && _requestedState != _currentState;

            switch(stateChanged)
            {
                case true:
                    ChangeState(_requestedState);
                    break;
                case false:
                    _currentState.UpdateState();
                    break;
            }
        }

        private void UpdateVision()
        {
            _player = null;
            FindOverlappingPlayer();
            if (_player == null)
            {
                FindPlayer(Facing);
                if(IsInCombat)
                    FindPlayer(-Facing);
            }
            
            if(_player != null)
            {
                _rememberedPlayer = _player;
                _rememberUntil = Time.time + 0.75f;
            }
            else if(IsInCombat && _rememberedPlayer != null)
                _player = _rememberedPlayer;

            SetPlayer(_player);
        }

        private bool IsInCombat =>
            _currentState != null && _currentState.Id != EnemyState.Patrol;

        private void SetPlayer(Transform player)
        {
            foreach(var state in _states.Values)
                state.SetPlayer(player);
        }

        private Vector2 DetectionCenter(float direction)
        {
            var origin = _body != null ? (Vector2)_body.bounds.center : (Vector2)transform.position;
            return origin + new Vector2(direction * _detectionSize.x * 0.5f, 0f);
        }

        private void FindPlayer(float direction)
        {
            if(_detectionSize.x <= 0f || _detectionSize.y <= 0f || Mathf.Approximately(direction, 0f))
                return;

            var sign = direction < 0f ? -1f : 1f;
            var hits = Physics2D.OverlapBoxAll(DetectionCenter(sign), _detectionSize, 0f, _playerLayer);
            Transform player = null;
            var closest = float.MaxValue;

            foreach(var hit in hits)
            {
                if(hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                var target = hit.attachedRigidbody != null
                    ? hit.attachedRigidbody.transform
                    : hit.transform;
                var distance = Mathf.Abs(target.position.x - transform.position.x);
                if(distance >= closest || IsOccluded(target.position))
                    continue;

                closest = distance;
                player = target;
            }
            
            _player = player;
        }

        private float OverlapSlack => _body != null ? _body.bounds.extents.x : 0.2f;

        private void FindOverlappingPlayer()
        {
            Vector2 center;
            Vector2 size;
            if(_body != null)
            {
                center = _body.bounds.center;
                size = _body.bounds.size;
            }
            else
            {
                center = transform.position;
                size = new Vector2(0.4f, 0.8f);
            }

            var hits = Physics2D.OverlapBoxAll(center, size, 0f, _playerLayer);
            foreach(var hit in hits)
            {
                if(hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                _player = hit.attachedRigidbody != null ? hit.attachedRigidbody.transform : hit.transform;
            }
        }

        private bool IsOccluded(Vector2 target)
        {
            var origin = _body != null ? (Vector2)_body.bounds.center : (Vector2)transform.position;
            if(Mathf.Abs(target.x - origin.x) <= OverlapSlack)
                return false;

            var end = new Vector2(target.x, origin.y);
            var hit = Physics2D.Linecast(origin, end, _occluders);
            return hit.collider != null &&
                   hit.collider.transform != transform &&
                   !hit.collider.transform.IsChildOf(transform);
        }

        private void ChangeState(BaseEnemyState state)
        {
            if(_currentState == state)
                return;

            if(_currentState != null)
            {
                if(BlocksRepeat(_currentState.Id))
                    _noRepeat = _currentState.Id;
                else if(state.Id != EnemyState.Recover && state.Id != _noRepeat)
                    _noRepeat = null;

                LockCooldowns(_currentState.Id);
                _currentState.EndState();
            }

            _currentState = state;
            _currentState.EnterState();
        }

        private void CheckActivations()
        {
            _activationCandidates.Clear();
            var currentState = _currentState == null ? EnemyState.Patrol : _currentState.Id;

            foreach(var state in _states.Values)
            {
                if(state.Id != currentState && IsLocked(state.Id))
                    continue;

                if(!state.CanBeActivated(currentState))
                    continue;

                _activationCandidates.Add(state);
            }

            _requestedState = SelectActivationCandidate();
        }

        private BaseEnemyState SelectActivationCandidate()
        {
            if(_currentState != null && !_currentState.HasCompleted && HasCandidate(_currentState))
                return _currentState;

            if(TryStartRecover(out var recover))
                return recover;

            var cycle = GetCombatCycleState();
            if(cycle != null)
                return cycle;

            return _activationCandidates.Count == 0 ? null : GetHighestPriorityCandidate();
        }

        private bool TryStartRecover(out BaseEnemyState recover)
        {
            recover = null;
            if(_currentState == null || _currentState.Id == EnemyState.Recover)
                return false;
            
            if(!_graph.TryGet(_currentState.Id, out var data) || data.RecoverTime <= 0f)
                return false;
            
            if(!_states.TryGetValue(EnemyState.Recover, out recover))
                return false;

            if(recover is EnemyRecoverState recoverState)
                recoverState.SetDuration(data.RecoverTime);

            return true;
        }

        private BaseEnemyState GetCombatCycleState()
        {
            if(_player == null)
                return null;

            var currentState = _currentState == null ? EnemyState.Patrol : _currentState.Id;
            _allowedStates.Clear();
            foreach(var candidate in _activationCandidates)
            {
                if(candidate.Id == EnemyState.Patrol)
                    continue;
                if(!_graph.CanTransit(currentState, candidate.Id))
                    continue;
                if(IsRepeatBlocked(candidate.Id))
                    continue;

                _allowedStates.Add(candidate.Id);
            }

            if(_allowedStates.Count == 0)
            {
                foreach(var candidate in _activationCandidates)
                {
                    if(candidate.Id == EnemyState.Patrol)
                        continue;
                    if(!_graph.CanTransit(currentState, candidate.Id))
                        continue;

                    _allowedStates.Add(candidate.Id);
                }
            }

            if(_allowedStates.Count == 0)
                return null;

            var nextId = _graph.Pick(currentState, _allowedStates);
            if(_states.TryGetValue(nextId, out var allowed) && HasCandidate(allowed))
                return allowed;

            return GetHighestPriorityCombatCandidate(currentState);
        }

        private BaseEnemyState GetHighestPriorityCombatCandidate(EnemyState from)
        {
            BaseEnemyState selected = null;

            foreach(var candidate in _activationCandidates)
            {
                if(candidate.Id == EnemyState.Patrol)
                    continue;
                if(!_graph.CanTransit(from, candidate.Id))
                    continue;
                if(IsRepeatBlocked(candidate.Id))
                    continue;

                if(selected == null ||
                   candidate.ActivationPriority > selected.ActivationPriority)
                    selected = candidate;
            }

            return selected;
        }

        private BaseEnemyState GetHighestPriorityCandidate()
        {
            BaseEnemyState selected = null;

            foreach(var candidate in _activationCandidates)
            {
                if(selected == null ||
                   candidate.ActivationPriority > selected.ActivationPriority)
                    selected = candidate;
            }

            return selected;
        }

        private static bool BlocksRepeat(EnemyState id) =>
            id is EnemyState.HeavyAttack or EnemyState.SpecialAttack;

        private bool IsRepeatBlocked(EnemyState id) => _noRepeat == id;

        private void LockCooldowns(EnemyState from)
        {
            if(!_graph.TryGet(from, out var data) || data.Cooldown <= 0f)
                return;

            var until = Time.time + data.Cooldown;
            Lock(data.State, until);
            foreach(var state in data.CooldownedStates)
                Lock(state, until);
        }

        private void Lock(EnemyState state, float until)
        {
            var index = (int)state;
            if(_lockedUntil == null || index < 0 || index >= _lockedUntil.Length)
                return;

            _lockedUntil[index] = Mathf.Max(_lockedUntil[index], until);
        }

        private bool IsLocked(EnemyState state)
        {
            var index = (int)state;
            return _lockedUntil != null &&
                   index >= 0 &&
                   index < _lockedUntil.Length &&
                   Time.time < _lockedUntil[index];
        }

        private bool HasCandidate(BaseEnemyState state)
        {
            foreach(var candidate in _activationCandidates)
            {
                if(candidate == state)
                    return true;
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if(_detectionSize.x <= 0f || _detectionSize.y <= 0f)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(DetectionCenter(Facing), _detectionSize);
        }
    }
}
