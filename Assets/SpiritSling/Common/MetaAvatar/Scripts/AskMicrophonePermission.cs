// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace SpiritSling
{
    public class AskMicrophonePermission : MonoBehaviour
    {
        public static bool IsReady { get; private set; }

        private bool _hasRequested;
        
        private void Start()
        {
            RequestMicPermission();
        }
        
        
        private bool HasMicPermission()
        {
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
            return true;
#endif
        }

        private void RequestMicPermission()
        {
            if (HasMicPermission())
            {
                IsReady = true;
                return;
            }
#if UNITY_ANDROID
            Log.Debug("AskMicrophonePermission: Requesting permission...");
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
            callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;

            Permission.RequestUserPermission(Permission.Microphone, callbacks);
#endif
        }

        private void OnApplicationFocus(bool focus)
        {
            // Wait for the user to answer the spatial data permission request.
            // RequestUserPermission is ignored by the system if a permission request window is currently displayed.
            // Ask again on focus
            if (!IsReady && focus)
            {
                RequestMicPermission();
            }
        }
        
#if UNITY_ANDROID
        private void PermissionCallbacks_PermissionGranted(string permissionName)
        {
            Log.Debug($"AskMicrophonePermission: {permissionName} PermissionGranted");
            IsReady = true;
        }

        private void PermissionCallbacks_PermissionDenied(string permissionName)
        {
            Log.Debug($"AskMicrophonePermission: {permissionName} PermissionDenied");
            IsReady = true;
        }
#endif
    }
}