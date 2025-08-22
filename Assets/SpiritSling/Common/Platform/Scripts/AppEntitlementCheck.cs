// Copyright (c) Meta Platforms, Inc. and affiliates.

#if (UNITY_STANDALONE_WIN || UNITY_ANDROID) && !BYPASS_OCULUS_CHECK
#define OCULUS_CHECK_ON
#endif
using Meta.XR.Samples;
using Oculus.Platform;
using UnityEngine;

namespace SpiritSling
{
	[MetaCodeSample("SpiritSling")]
    public sealed class AppEntitlementCheck : MonoBehaviour
    {
        public delegate void OnPlatformReadyHandler();

        public static OnPlatformReadyHandler OnPlatformReady;
        public static bool IsReady { get; private set; }

#if OCULUS_CHECK_ON
        private void Awake()
        {
            IsReady = false;
            try
            {
                Core.Initialize();
                Entitlements.IsUserEntitledToApplication().OnComplete(EntitlementCallback);
            }
            catch (UnityException e)
            {
                Log.Error(e);
                Log.Error("Platform failed to initialize due to exception.");
#if !UNITY_EDITOR
                // Immediately quit the application
                UnityEngine.Application.Quit();
#endif
            }
        }

        // Called when the Meta Quest Platform completes the async entitlement check request and a result is available.
        private void EntitlementCallback(Message msg)
        {
            if (msg.IsError) // User failed entitlement check
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Log.Warning("You are NOT entitled to use this app. Ignoring this for unity editor and dev build.");
                IsReady = true;
                OnPlatformReady?.Invoke();
#else
                // Implements a default behavior for an entitlement check failure -- log the failure and exit the app.
                Log.Error("You are NOT entitled to use this app.");
                UnityEngine.Application.Quit();
#endif
            }
            else // User passed entitlement check
            {
                // Log the succeeded entitlement check for debugging.
                Log.Info("You are entitled to use this app.");
                IsReady = true;
                OnPlatformReady?.Invoke();
            }
        }
#endif
    }
}
