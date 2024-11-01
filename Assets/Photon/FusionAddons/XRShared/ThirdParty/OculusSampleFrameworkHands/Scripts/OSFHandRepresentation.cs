using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.XR.Shared.Rig;

namespace Fusion.XR.Shared.SimpleHands
{
    /***
     * 
     * OSFHandRepresentation is in charge to display and animate the Oculus Sample Frame work hand according to the HandCommand received 
     * (for example by the HardwareHandRepresentationManager or NetworkHand classes using the SetHandCommand method)
     * It provides methods to change the hand's color or material
     * 
     ***/

    public class OSFHandRepresentation : MonoBehaviour, IHandRepresentation
    {

        public HandCommand currentCommand;

        public SkinnedMeshRenderer handMeshRenderer;
        public Animator handAnimator;

        [Header("Animation layers and configuration")]
        public string pinchAnimationParameter = "Pinch";
        public string flexAnimationParameter = "Flex";
        public string poseAnimationParameter = "Pose";
        public string pointAnimationLayer = "Point Layer";
        public string thumbAnimationLayer = "Thumb Layer";
        public float maxGripToPinch = 0.05f;

        private void Awake()
        {
            if (handMeshRenderer == null) handMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (handAnimator == null) handAnimator = GetComponentInChildren<Animator>();
            if (handAnimator == null) handAnimator = GetComponentInParent<Animator>();
        }

        public bool IsMeshDisplayed => handMeshRenderer.enabled;

        public void DisplayMesh(bool shouldDisplay)
        {
            if(handMeshRenderer) handMeshRenderer.enabled = shouldDisplay;
        }

        public virtual void SetHandColor(Color color)
        {
            handMeshRenderer.material.color = color;
        }
        public virtual void SetHandMaterial(Material material)
        {
            handMeshRenderer.sharedMaterial = material;
        }

        public void SetHandCommand(HandCommand command)
        {
            currentCommand = AnalyseCommand(command);
        }

        public virtual HandCommand AnalyseCommand(HandCommand command)
        {
            if (command.indexTouchedCommand == 1)
            {
                // Index is resting on the button: force a minimal value for trigger
                command.triggerCommand = 0.5f + command.triggerCommand/2;                
            }
            if (command.thumbTouchedCommand == 1 && command.triggerCommand >= 0.5f)
            {
                // Thumb and index are pressed: should we pinch ? => only if grip is not pressed
                if (command.gripCommand < maxGripToPinch)
                {
                    command.pinchCommand = 1f - command.triggerCommand;
                }
            }
            return command;
        }

        int pointAnimationLayerIndex = -1;
        int thumbAnimationLayerIndex = -1;
        bool layerIndexFound = false;
        public virtual void ApplyCommand(HandCommand command) {
            command = AnalyseCommand(command);

            if (!layerIndexFound)
            {
                layerIndexFound = true;
                pointAnimationLayerIndex = handAnimator.GetLayerIndex(pointAnimationLayer);
                thumbAnimationLayerIndex = handAnimator.GetLayerIndex(thumbAnimationLayer);
            }
            // Apply layers
            float pointRaisedLayerWeight = 1f - command.triggerCommand;
            handAnimator.SetLayerWeight(pointAnimationLayerIndex, pointRaisedLayerWeight);
            float thumbRaisedLayerWeight = 1f - command.thumbTouchedCommand;
            handAnimator.SetLayerWeight(thumbAnimationLayerIndex, thumbRaisedLayerWeight);

            // Apply parameters
            float flexParameterValue = command.gripCommand;
            handAnimator.SetFloat(flexAnimationParameter, flexParameterValue); 
            float pinchParameterValue = 1f - command.pinchCommand;
            handAnimator.SetFloat(pinchAnimationParameter, pinchParameterValue);
            int poseParameterValue = command.poseCommand;
            handAnimator.SetInteger(poseAnimationParameter, poseParameterValue);
        }

        private void Update()
        {
            if (!isVisible) return;
            ApplyCommand(currentCommand);
        }

        public bool isVisible { get; set;  } = false; 
        private void OnBecameVisible()
        {
            isVisible = true;
        }

        private void OnBecameInvisible()
        {
            isVisible = false;
        }

    }
}
