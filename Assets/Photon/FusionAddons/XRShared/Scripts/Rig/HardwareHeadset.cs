using UnityEngine;

namespace Fusion.XR.Shared.Rig
{
    /**
     * 
     * Head class for the hardware rig
     * 
     * The gameobject should susually contain a Camera and a Fader
     * 
     **/

    public class HardwareHeadset : MonoBehaviour
    {
        public NetworkTransform networkTransform;

        private void Awake()
        {
            if (networkTransform == null) networkTransform = GetComponent<NetworkTransform>();
        }
    }
}
