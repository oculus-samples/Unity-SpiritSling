using UnityEngine;

namespace Fusion.XR.Shared.Rig
{
    /**
     * 
     * Network VR user headset
     * Position synchronization is handled in the NetworkRig
     * 
     **/

    [RequireComponent(typeof(NetworkTransform))]
    [DefaultExecutionOrder(NetworkHeadset.EXECUTION_ORDER)]
    public class NetworkHeadset : NetworkBehaviour
    {
        public const int EXECUTION_ORDER = NetworkRig.EXECUTION_ORDER + 10;

        [HideInInspector]
        public NetworkTransform networkTransform;

        private void Awake()
        {
            if (networkTransform == null) networkTransform = GetComponent<NetworkTransform>();
        }
    }
}

