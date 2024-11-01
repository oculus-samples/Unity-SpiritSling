// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace SpiritSling
{
    /// <summary>
    /// A single state
    /// </summary>
    public abstract class State : MonoBehaviour
    {
        public StateMachine StateMachine { get; set; }

        public virtual void Awake()
        {
            gameObject.SetActive(false);
        }

        public abstract void Enter();
        public abstract void Update();
        public abstract void Exit();
    }
}