// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Game end state
    /// </summary>
    public class GameOverState : TabletopGameState
    {
        public override void Enter()
        {
            base.Enter();
            StartCoroutine(ReleaseStateAuthority());
        }

        private IEnumerator ReleaseStateAuthority()
        {
            // Waits a bit before releasing the authority, because RPCs may be sending
            yield return new WaitForSeconds(0.2f);

            yield return ReleaseStateAuthorityOnBoardObjects();
        }
    }
}