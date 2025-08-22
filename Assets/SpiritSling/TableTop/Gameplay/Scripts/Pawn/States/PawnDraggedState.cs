// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// The pawn is being dragged by the player
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public abstract class PawnDraggedState : PawnState
    {
        [SerializeField]
        private AudioClip rangeAudioClip;

        [SerializeField]
        private AudioMixerGroups audioMixerGroup;

        private HexCellRenderer[] allCells;

        protected HexCell _previousDroppableCell;
        protected bool _firstHighlight;

        public override void Enter()
        {
            base.Enter();

            _previousDroppableCell = null;
            _firstHighlight = true;

            // Bots bypass the AllowGrab check
            if (!Movement.AllowGrab && Pawn.Owner != null && Pawn.Owner.IsHuman)
            {
                PawnStateMachine.ChangeState(PawnStateMachine.idleState);
                return;
            }

            PawnMovement.DraggedObject = Movement.gameObject;
            Pawn.StartDragging();
            TabletopGameEvents.OnPawnDragStart?.Invoke();

            Movement.InitialPosition = Movement.transform.position;
            Movement.ShowCellDetectionVFX(true);

            StartCoroutine(EnterCoroutine());
        }

        private IEnumerator EnterCoroutine()
        {
            // Waits for cells to be in the correct state
            yield return null;

            allCells ??= FindObjectsByType<HexCellRenderer>(FindObjectsSortMode.None);

            foreach (var cell in allCells)
            {
                if (cell.State is HexCellRenderer.CellState.Range or HexCellRenderer.CellState.RangeCLOC)
                {
                    AudioManager.Instance.Play(rangeAudioClip, audioMixerGroup, cell.transform);
                }
            }
        }

        public override void Update()
        {
            // Bots bypass the AllowGrab check
            if (!Movement.AllowGrab && Pawn.Owner != null && Pawn.Owner.IsHuman)
            {
                Movement.DroppableCell = null;
                PawnStateMachine.ChangeState(PawnStateMachine.dropState);
            }

            HighlightDroppableCells();
        }

        public override void Exit()
        {
            base.Exit();
            Movement.ShowCellDetectionVFX(false);
        }

        protected virtual void HighlightDroppableCells()
        {
            var closestCell = FindClosestCell();
            var centerCell = GetCenterCell();
            var range = GetMoveRange();
            var canDropInLava = CanDropInLava();

            var droppableCell = HighlightAndGetDroppableCell(centerCell, closestCell, range, canDropInLava);

            if (droppableCell != _previousDroppableCell || _firstHighlight)
            {
                _firstHighlight = false;
                _previousDroppableCell = droppableCell;
                Movement.DroppableCell = droppableCell;

                NotifyOtherPlayers(centerCell.Position, closestCell != null ? closestCell.Position : HexGridRenderer.OutOfBoardPosition);
            }
        }

        private HexCell FindClosestCell()
        {
            return TabletopGameManager.Instance.GridRenderer.FindClosestCell(Movement.transform.position, TabletopConfig.Get().kodamaUnsnapDistance);
        }

        private HexCell HighlightAndGetDroppableCell(HexCell centerCell, HexCell closestCell, int range, bool canDropInLava)
        {
            return TabletopGameManager.Instance.GridRenderer.HighlightDroppableCells(
                centerCell, closestCell, range, canDropInLava, HexCellRenderer.CellState.Range);
        }

        protected abstract HexCell GetCenterCell();
        protected abstract int GetMoveRange();
        protected abstract bool CanDropInLava();
        protected abstract void NotifyOtherPlayers(Vector3Int centerPosition, Vector3Int closestCellPosition);
    }
}
