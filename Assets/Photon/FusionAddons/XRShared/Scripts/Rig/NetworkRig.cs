using UnityEngine;


namespace Fusion.XR.Shared.Rig
{
    /**
     * 
     * Networked VR user
     * 
     * Handle the synchronisation of the various rig parts: headset, left hand, right hand, and playarea (represented here by the NetworkRig)
     * Use the local HardwareRig rig parts position info when this network rig is associated with the local user 
     * 
     * Note: this class (and all network classes in this sample) is heavily focused on shared mode, hence not using InputAuthority, and cannot be used in Host or Server topologies 
     * 
     **/

    [RequireComponent(typeof(NetworkTransform))]
    // We ensure to run after the NetworkTransform or NetworkRigidbody, to be able to override the interpolation target behavior in Render()
    [DefaultExecutionOrder(NetworkRig.EXECUTION_ORDER)]
    public class NetworkRig : NetworkBehaviour
    {
        public const int EXECUTION_ORDER = 100;
        
        public HardwareRig hardwareRig;
        public NetworkHand leftHand;
        public NetworkHand rightHand;
        public NetworkHeadset headset;

        [HideInInspector]
        public NetworkTransform networkTransform;

        protected virtual void Awake()
        {
            networkTransform = GetComponent<NetworkTransform>();
        }

        // As we are in shared topology, having the StateAuthority means we are the local user
        public virtual bool IsLocalNetworkRig => Object && Object.HasStateAuthority;

        public override void Spawned()
        {
            base.Spawned();
            if (IsLocalNetworkRig)
            {
                hardwareRig = FindObjectOfType<HardwareRig>();
                if (hardwareRig == null) Debug.LogError("Missing HardwareRig in the scene");
            }
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            // Update the rig at each network tick for local player. The NetworkTransform will forward this to other players
            if (IsLocalNetworkRig && hardwareRig)
            {
                RigState rigState = hardwareRig.RigState;
                ApplyLocalStateToRigParts(rigState);
                ApplyLocalStateToHandPoses(rigState);
            }
        }

        protected virtual void ApplyLocalStateToRigParts(RigState rigState)
        {
            transform.position = rigState.playAreaPosition;
            transform.rotation = rigState.playAreaRotation;
            leftHand.transform.position = rigState.leftHandPosition;
            leftHand.transform.rotation = rigState.leftHandRotation;
            rightHand.transform.position = rigState.rightHandPosition;
            rightHand.transform.rotation = rigState.rightHandRotation;
            headset.transform.position = rigState.headsetPosition;
            headset.transform.rotation = rigState.headsetRotation;
        }
        protected virtual void ApplyLocalStateToHandPoses(RigState rigState)
        {
            // we update the hand pose info. It will trigger on network hands OnHandCommandChange on all clients, and update the hand representation accordingly
            leftHand.HandCommand = rigState.leftHandCommand;
            rightHand.HandCommand = rigState.rightHandCommand;
        }

        public override void Render()
        {
            base.Render();
            if (IsLocalNetworkRig)
            {
                // Extrapolate for local user :
                // we want to have the visual at the good position as soon as possible, so we force the visuals to follow the most fresh hardware positions

                RigState rigState = hardwareRig.RigState;

                transform.position = rigState.playAreaPosition;
                transform.rotation = rigState.playAreaRotation;
                leftHand.transform.position = rigState.leftHandPosition;
                leftHand.transform.rotation = rigState.leftHandRotation;
                rightHand.transform.position = rigState.rightHandPosition;
                rightHand.transform.rotation = rigState.rightHandRotation;
                headset.transform.position = rigState.headsetPosition;
                headset.transform.rotation = rigState.headsetRotation;
            }
        }
    }
}
