// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;

namespace SpiritSling
{
    /// <summary>
    /// A very simple state machine
    /// </summary>
    public abstract class StateMachine : MonoBehaviour
    {
        protected State _currentState;

        public event Action<State, State> StateChanged;

        public virtual void ChangeState(State newState)
        {
            SetState(newState);
        }

        protected void SetState(State newState)
        {
            if (newState == _currentState)
                return;

            var previousState = _currentState;
            if (_currentState != null)
            {
                _currentState.Exit();
                _currentState.gameObject.SetActive(false);
            }

            _currentState = newState;

            if (_currentState != null)
            {
                _currentState.StateMachine = this;
                _currentState.gameObject.SetActive(true);
                _currentState.Enter();
            }

            StateChanged?.Invoke(previousState, _currentState);
        }

        public void Clear()
        {
            if (_currentState)
            {
                _currentState.Exit();
                _currentState.gameObject.SetActive(false);
                _currentState = null;
            }
        }

        public virtual void Restart()
        {
        }

        public State CurrentState => _currentState;
    }
}