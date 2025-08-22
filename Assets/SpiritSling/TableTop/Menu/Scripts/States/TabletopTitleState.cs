// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class TabletopTitleState : TabletopMenuBaseState
    {
        [Header("title menu")]
        [SerializeField]
        private float titleTimeSec = 2f;

        private float currentTimerSec;

        public override void Awake()
        {
            base.Awake();
        }

        public override void Enter()
        {
            base.Enter();
            currentTimerSec = titleTimeSec;
        }

        public override void Update()
        {
            currentTimerSec -= Time.deltaTime;
            if (currentTimerSec < 0f)
            {
                ChangeToNextState();
            }
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}
