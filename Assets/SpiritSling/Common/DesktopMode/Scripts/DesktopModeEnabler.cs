// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SpiritSling
{
    [MetaCodeSample("SpiritSling")]
    public class DesktopModeEnabler : MonoBehaviour
    {
        [SerializeField]
        private GameObject vrRig;

        [SerializeField]
        private GameObject desktopRig;

        private void Start()
        {
            SetDesktop(IsDesktopMode);
        }

        public void SetDesktop(bool yes)
        {
            // Set mouse event system
            var uiInput = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            uiInput.enabled = yes;
            var vrInput = EventSystem.current.GetComponent<PointableCanvasModule>();
            vrInput.enabled = !yes;

            desktopRig.gameObject.SetActive(yes);
            if (vrRig != null) vrRig.gameObject.SetActive(!yes);
        }

        public static bool IsDesktopMode
        {
#if UNITY_EDITOR
            get => UnityEditor.EditorPrefs.GetInt("DESKTOP_MODE", 0) > 0;
            set => UnityEditor.EditorPrefs.SetInt("DESKTOP_MODE", value ? 1 : 0);
#elif DESKTOP_MODE
            get => true;
            set {}
#else
            get => false;
            set {}
#endif
        }
    }
}
