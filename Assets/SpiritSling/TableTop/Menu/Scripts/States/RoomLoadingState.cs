// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
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
