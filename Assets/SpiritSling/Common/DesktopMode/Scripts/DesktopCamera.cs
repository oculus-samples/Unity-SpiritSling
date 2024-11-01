// Copyright (c) Meta Platforms, Inc. and affiliates.

using SpiritSling.TableTop;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpiritSling
{
    public class DesktopCamera : MonoBehaviour
    {
        public Vector3 menuCamPos;
        public Vector3 menuCamRot;
        public Vector3 gameCamPos;
        public Vector3 gameCamRot;

        void Update()
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                transform.SetLocalPositionAndRotation(menuCamPos, Quaternion.Euler(menuCamRot));
            }
            else if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                transform.SetLocalPositionAndRotation(gameCamPos, Quaternion.Euler(gameCamRot));
            }

            if (Keyboard.current.upArrowKey.isPressed)
            {
                transform.position += new Vector3(0, 0, 1) * Time.deltaTime;
            }
            else if (Keyboard.current.downArrowKey.isPressed)
            {
                transform.position += new Vector3(0, 0, -1) * Time.deltaTime;
            }
            else if (Keyboard.current.leftArrowKey.isPressed)
            {
                transform.position += new Vector3(-1, 0, 0) * Time.deltaTime;
            }
            else if (Keyboard.current.rightArrowKey.isPressed)
            {
                transform.position += new Vector3(1, 0, 0) * Time.deltaTime;
            }
            else if (Keyboard.current.pageDownKey.isPressed)
            {
                transform.position += new Vector3(0, -1, 0) * Time.deltaTime;
            }
            else if (Keyboard.current.pageUpKey.isPressed)
            {
                transform.position += new Vector3(0, 1, 0) * Time.deltaTime;
            }

            if (Keyboard.current.spaceKey.isPressed)
            {
                if (TabletopGameManager.Instance && TabletopGameManager.Instance.CurrentPlayerIndex == BaseTabletopPlayer.LocalPlayer.Index)
                {
                    TabletopGameManager.Instance.SkipPhase();
                }
            }
        }
    }
}