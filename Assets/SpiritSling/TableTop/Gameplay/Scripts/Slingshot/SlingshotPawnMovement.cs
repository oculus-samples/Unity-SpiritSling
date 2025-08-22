// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Linq;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class SlingshotPawnMovement : PawnMovement
    {
        protected override bool CheckAllowGrab()
        {
            var allowed = base.CheckAllowGrab();
            if (allowed)
            {
                // Move existing slingshots only if all players slingshots has already been placed
                var allPlaced = TabletopGameManager.Instance.Slingshots.
                    Where(s => s != null && s.OwnerId == BaseTabletopPlayer.LocalPlayer.PlayerId).
                    All(s => s.IsOnGrid);

                allowed &= (stateMachine.pawn.CurrentCell == null || allPlaced);
                allowed &= DraggedObject == gameObject || DraggedObject == null;
            }

            return allowed;
        }
    }
}
