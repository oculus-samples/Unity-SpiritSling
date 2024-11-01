using Fusion.XR.Shared.Rig;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;

namespace Fusion.XR.Shared
{
    /**
     * Helpers methods to facilitate the settings of InputActionProperty for XR controllers, to give them default action IF none were set in the inspector
     */
    public static class InputSystemExtensions
    {
        /**
         * Add default binding if none are already set (in reference or manually set in action), and add XR prefix for left and right bindings
         **/
        public static void EnableWithDefaultXRBindings(this InputActionProperty property, List<string> bindings = null, List<string> leftBindings = null, List<string> rightBindings = null, bool mergeWithPreExistingBinding = false)
        {
            if (property.reference == null && (property.action.bindings.Count == 0 || mergeWithPreExistingBinding))
            {
                const string xrPrefix = "<XRController>";
                if (bindings == null) bindings = new List<string>();
                if (leftBindings != null) foreach (var binding in leftBindings) bindings.Add(xrPrefix + "{LeftHand}" + "/" + binding.TrimStart('/'));
                if (rightBindings != null) foreach (var binding in rightBindings) bindings.Add(xrPrefix + "{RightHand}" + "/" + binding.TrimStart('/'));

                foreach (var binding in bindings)
                {
                    if (binding.StartsWith("composite", System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        string[] parameters = binding.Split("||");
                        if (parameters.Length >= 2)
                        {
                            string bindingType = parameters[1];
                            var compositeBindingState = property.action.AddCompositeBinding(bindingType);

                            string bindingName = default;
                            string bindingValue = default;

                            for (int i = 2; i < parameters.Length; i++)
                            {
                                if (i % 2 == 0)
                                {
                                    bindingName = parameters[i];
                                }
                                else
                                {
                                    bindingValue = parameters[i];
                                    compositeBindingState = compositeBindingState.With(bindingName, bindingValue);
                                }
                            }
                        }
                    }
                    else
                    {
                        property.action.AddBinding(binding);
                    }

                }
            }

            if (property.action != null)
            {
                property.action.Enable();
            }
        }

        public static void EnableWithDefaultXRBindings(this InputActionProperty property, RigPart side, List<string> bindings)
        {
            if (side == RigPart.LeftController) property.EnableWithDefaultXRBindings(leftBindings: bindings);
            if (side == RigPart.RightController) property.EnableWithDefaultXRBindings(rightBindings: bindings);
        }
    }
}

#endif

