// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace SpiritSling.TableTop
{
    public class RoomLoadingState : TabletopMenuBaseState
    {
        public override void Enter()
        {
            FadeIn();
        }

        public override void Exit()
        {
            FadeOut();
        }
    }
}