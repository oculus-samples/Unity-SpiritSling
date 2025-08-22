// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// This class is responsible for spawning the local player objects on the game board
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class PlayerBoardObjects : NetworkBehaviour
    {
        [SerializeField]
        private GameObject kodamaPrefab;

        [SerializeField]
        private GameObject humanPlayerBoardPrefab;

        [SerializeField]
        private GameObject botBoardPrefab;

        [SerializeField]
        private GameObject slingShotPrefab;

        /// <summary>
        /// Spawn the kodama prefab for the given player
        /// </summary>
        public IEnumerator SpawnPlayerKodama(HexCellRenderer spawnCellRenderer, BaseTabletopPlayer owner)
        {
            var centerPosition = TabletopGameManager.Instance.GridRenderer.
                GetCell(TabletopGameManager.Instance.Grid.Get(new Vector3Int(0, 0))).transform.position;
            var spawnPosition = spawnCellRenderer.transform.position;
            var fwdToCenter = (centerPosition - spawnPosition).SetY(0);
            var spawnRotation = Quaternion.LookRotation(fwdToCenter);

            var asyncOp = Runner.SpawnAsync(
                kodamaPrefab, spawnPosition,
                spawnRotation, Runner.LocalPlayer, (runner, obj) =>
                {
                    var p = obj.GetComponent<Pawn>();
                    p.OwnerId = owner.PlayerId;
                    p.Position = spawnCellRenderer.Cell.Position;
                });
            yield return new WaitUntil(() => asyncOp.IsSpawned);
            yield return null;
        }

        /// <summary>
        /// spawns the player board in front of this init tile
        /// </summary>
        public IEnumerator SpawnPlayerBoard(HexCellRenderer spawnCellRenderer, BaseTabletopPlayer owner)
        {
            var playerBoardSpawn = GameVolume.Instance.GetClosestPlayerBoard(spawnCellRenderer.transform.position);
            var prefab = owner.IsHuman ? humanPlayerBoardPrefab : botBoardPrefab;

            var asyncOp = Runner.SpawnAsync(
                prefab, playerBoardSpawn.position, playerBoardSpawn.rotation, Runner.LocalPlayer, (runner, obj) =>
                {
                    var p = obj.GetComponent<Playerboard>();
                    p.OwnerId = owner.PlayerId;
                });
            yield return new WaitUntil(() => asyncOp.IsSpawned);
            yield return null;
        }

        public IEnumerator SpawnSlingshots(BaseTabletopPlayer owner)
        {
            for (var i = 0; i < owner.Board.SlingshotsCount; i++)
            {
                var tcs = new TaskCompletionSource<bool>();
                var slot = i + 1;
                var asyncOp = Runner.SpawnAsync(
                    slingShotPrefab, Vector3.zero, Quaternion.identity, Runner.LocalPlayer, (runner, o) =>
                    {
                        var s = o.GetComponent<Slingshot>();
                        s.OwnerId = owner.PlayerId;
                        s.BoardSlot = slot;
                        tcs.SetResult(true);
                    });

                yield return new WaitUntil(() => asyncOp.IsSpawned);
                yield return null;
            }
        }
    }
}
