using System;
using System.Collections.Generic;
using DeadCells.Player.AnimationControllers;
using DeadCells.Player.States;
using UnityEngine;

namespace DeadCells.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInput))]
    public sealed class PlayerController : MonoBehaviour
    {
        private readonly Dictionary<PlayerState, PlayerMovementState> _states = new();
        private readonly List<PlayerMovementState> _activationCandidates = new();
        private PlayerMovementState _currentState;
        private PlayerMovementState _requestedState;
        private AnimationController _animationController;

        private void Awake()
        {
            var input = GetComponent<PlayerInput>();
            var rigidbody = GetComponent<Rigidbody2D>();
            _animationController = GetComponent<AnimationController>();

            if(_animationController == null)
                throw new InvalidOperationException("PlayerController: add an AnimationController implementation.");

            foreach(var state in GetComponents<PlayerMovementState>())
            {
                if(!_states.TryAdd(state.Id, state))
                    throw new InvalidOperationException($"PlayerController: duplicate state {state.Id}.");

                state.Initialize(input, rigidbody, _animationController);
            }
        }

        private void OnEnable()
        {
            CheckActivations();

            if(_requestedState == null)
                throw new InvalidOperationException("PlayerController: no initial state requested activation.");

            ChangeState(_requestedState);
        }

        private void OnDisable()
        {
            _currentState?.EndState();
            _currentState = null;
            _requestedState = null;

            foreach(var state in _states.Values)
                state.ResetState();
        }

        private void FixedUpdate()
        {
            CheckActivations();
            var stateChanged = _requestedState != null && _requestedState != _currentState;

            switch (stateChanged)
            {
                case true:
                    ChangeState(_requestedState);
                    break;
                case false:
                    _currentState.UpdateState();
                    break;
            }

            _animationController.PlayAnimation(_currentState.Id, _currentState.AnimationValue);
        }

        private void ChangeState(PlayerMovementState state)
        {
            if(_currentState == state)
                return;

            if(_currentState != null)
                _currentState.EndState();
            
            _currentState = state;
            _currentState.EnterState();
        }

        private void CheckActivations()
        {
            _requestedState = null;
            _activationCandidates.Clear();
            var currentState = _currentState == null ? PlayerState.Ground : _currentState.Id;

            foreach(var state in _states.Values)
            {
                if(!state.CanBeActivated(currentState))
                    continue;

                _activationCandidates.Add(state);
            }

            _requestedState = SelectActivationCandidate();
        }

        private PlayerMovementState SelectActivationCandidate()
        {
            PlayerMovementState selectedState = null;

            foreach(var candidate in _activationCandidates)
            {
                if(selectedState == null ||
                   candidate.ActivationPriority > selectedState.ActivationPriority)
                    selectedState = candidate;
            }

            return selectedState;
        }
    }
}
