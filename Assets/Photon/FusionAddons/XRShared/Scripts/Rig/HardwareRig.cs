using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Fusion.XR.Shared.Rig
{
    public enum RigPart
    {
        None,
        Headset,
        LeftController,
        RightController,
        Undefined
    }

    // Include all rig parameters in a structure
    public struct RigState
    {
        public Vector3 playAreaPosition;
        public Quaternion playAreaRotation;
        public Vector3 leftHandPosition;
        public Quaternion leftHandRotation;
        public Vector3 rightHandPosition;
        public Quaternion rightHandRotation;
        public Vector3 headsetPosition;
        public Quaternion headsetRotation;
        public HandCommand leftHandCommand;
        public HandCommand rightHandCommand;
    }

    /**
     * 
     * Hardware rig gives access to the various rig parts: head, left hand, right hand, and the play area, represented by the hardware rig itself
     *  
     * Can be moved, either instantanesously, or with a camera fade
     * 
     **/

    public class HardwareRig : MonoBehaviour
    {
        public HardwareHand leftHand;
        public HardwareHand rightHand;
        public HardwareHeadset headset;

        [Serializable]
        public class TeleportEvent : UnityEvent<Vector3, Vector3> { }
        public TeleportEvent onTeleport = new TeleportEvent();

        RigState _rigState = default;

        public virtual RigState RigState
        {
            get
            {
                _rigState.playAreaPosition = transform.position;
                _rigState.playAreaRotation = transform.rotation;
                _rigState.leftHandPosition = leftHand.transform.position;
                _rigState.leftHandRotation = leftHand.transform.rotation;
                _rigState.rightHandPosition = rightHand.transform.position;
                _rigState.rightHandRotation = rightHand.transform.rotation;
                _rigState.headsetPosition = headset.transform.position;
                _rigState.headsetRotation = headset.transform.rotation;
                _rigState.leftHandCommand = leftHand.handCommand;
                _rigState.rightHandCommand = rightHand.handCommand;
                return _rigState;
            }
        }

        #region Locomotion
        // Update the hardware rig rotation. 
        public virtual void Rotate(float angle)
        {
            transform.RotateAround(headset.transform.position, transform.up, angle);
        }

        // Update the hardware rig position. 
        public virtual void Teleport(Vector3 position)
        {
            Vector3 headsetOffet = headset.transform.position - transform.position;
            headsetOffet.y = 0;
            Vector3 previousPosition = transform.position;
            transform.position = position - headsetOffet;
            if (onTeleport != null) onTeleport.Invoke(previousPosition, transform.position);
        }

        // Teleport the rig with a fader
        public virtual void FadedTeleport(Vector3 position)
        {
            Teleport(position);
        }

        // Rotate the rig with a fader
        public virtual void FadedRotate(float angle)
        {
            Rotate(angle);
        }
        #endregion

    }
}
