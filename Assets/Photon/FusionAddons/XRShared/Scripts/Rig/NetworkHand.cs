using UnityEngine;

namespace Fusion.XR.Shared.Rig
{
    public interface IHandRepresentation
    {
        public void SetHandCommand(HandCommand command);
        public GameObject gameObject { get; }
        public void SetHandColor(Color color);
        public void SetHandMaterial(Material material);
        public void DisplayMesh(bool shouldDisplay);
        public bool IsMeshDisplayed { get; }
    }


    /**
     * 
     * Network VR user hand
     * 
     * Handle the synchronisation of the hand pose
     * Use the local HardwareRig rig hand pose when this rig is associated with the local user 
     * 
     * Position synchronization is handled in the NetworkRig
     * 
     **/

    [RequireComponent(typeof(NetworkTransform))]
    [DefaultExecutionOrder(NetworkHand.EXECUTION_ORDER)]
    public class NetworkHand : NetworkBehaviour
    {
        public const int EXECUTION_ORDER = NetworkRig.EXECUTION_ORDER + 10;
        [HideInInspector]
        public NetworkTransform networkTransform;
        [Networked]
        public HandCommand HandCommand { get; set; }
        public RigPart side;
        NetworkRig rig;
        IHandRepresentation handRepresentation;
        ChangeDetector changeDetector;

        public bool IsLocalNetworkRig => rig.IsLocalNetworkRig;

        public HardwareHand LocalHardwareHand => IsLocalNetworkRig ? (side == RigPart.LeftController ? rig.hardwareRig.leftHand : rig.hardwareRig.rightHand) : null;

        private void Awake()
        {
            rig = GetComponentInParent<NetworkRig>();
            networkTransform = GetComponent<NetworkTransform>();
            handRepresentation = GetComponentInChildren<IHandRepresentation>();
        }

        public override void Spawned()
        {
            base.Spawned();
            changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        }
      
        public override void Render()
        {
            base.Render();
            if (IsLocalNetworkRig)
            {
                // Extrapolate for local user : we want to have the visual at the good position as soon as possible, so we force the visuals to follow the most fresh hand pose
                UpdateRepresentationWithLocalHardwareState();
            }
            else
            {
                foreach (var changedNetworkedVarName in changeDetector.DetectChanges(this))
                {
                    if (changedNetworkedVarName == nameof(HandCommand))
                    {
                        // Will be called when the local user change the hand pose structure
                        // We trigger here the actual animation update
                        UpdateHandRepresentationWithNetworkState();
                    }
                }
            }
        }

        // Update the hand representation each time the network structure HandCommand is updated
        void UpdateHandRepresentationWithNetworkState()
        {
            if (handRepresentation != null) handRepresentation.SetHandCommand(HandCommand);
        }

        // Update the hand representation with local hardware HandCommand
        void UpdateRepresentationWithLocalHardwareState()
        {
            if (handRepresentation != null) handRepresentation.SetHandCommand(LocalHardwareHand.handCommand);
        }
    }
}

