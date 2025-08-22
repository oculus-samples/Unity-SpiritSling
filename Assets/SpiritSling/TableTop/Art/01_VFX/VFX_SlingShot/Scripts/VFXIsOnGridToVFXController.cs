// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class VFXIsOnGridToVFXController : MonoBehaviour
    {
        [SerializeField]
        private Pawn _pawn;

        public UnityEvent OnIsOnGrid;
        public UnityEvent OnIsNotOnGrid;
        public UnityEvent OnMoveOutOfFirstPosition;

        bool currentState = true;

        public bool InitialStateIsOnGrid;

        void Start()
        {
            _pawn = GetComponent<Pawn>();
            _pawn ??= GetComponentInParent<Pawn>();
            if (InitialStateIsOnGrid)
            {
                currentState = false;
            }
        }

        private HexCell _FirstPosition;
        private bool _hasMovedOutOfFirstPosition;

        void Update()
        {
            //Debug.Log("<color=purple>Grid STATE: " + _slingshot.IsOnGrid + "==</color> " + currentState);
            if (_pawn && currentState != _pawn.IsOnGrid)
            {
                //Debug.Log("<color=red>IS ON GRID ?? " + _slingshot.IsOnGrid + "==</color> " + currentState);

                if (_pawn.IsOnGrid)
                {
                    OnIsOnGrid?.Invoke();
                    _FirstPosition = _pawn.CurrentCell;
                }
                else if (!_pawn.IsOnGrid)
                {
                    OnIsNotOnGrid?.Invoke();
                    _hasMovedOutOfFirstPosition = false;
                }

                currentState = _pawn.IsOnGrid;
            }

            if (_hasMovedOutOfFirstPosition == false && _FirstPosition != null && _pawn.CurrentCell != _FirstPosition)
            {
                _hasMovedOutOfFirstPosition = true;
                OnMoveOutOfFirstPosition?.Invoke();
            }
        }
    }
}
