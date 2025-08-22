// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Photon.Voice.Unity;
using UnityEngine;

namespace SpiritSling
{
    /// <summary>
    /// Utility script to modify microphone processing settings with the controllers. To use only in internal test builds.
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class MicrophoneSettingsModifier : MonoBehaviour
    {
        private WebRtcAudioDsp webRtcAudioDsp;

        private bool aec = true;
        private bool agc = true;
        private bool ns = true;
        private bool vad = true;

        private int agcCompressionGain = 25;
        private int agcTargetLevel;

        private void Start()
        {
            webRtcAudioDsp = GetComponent<WebRtcAudioDsp>();
        }

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.RawButton.A))
            {
                aec = !aec;
                Debug.Log("MicrophoneSettingsModifier Button A, aec = " + aec);
                webRtcAudioDsp.AEC = aec;
            }

            if (OVRInput.GetDown(OVRInput.RawButton.B))
            {
                agc = !agc;
                Debug.Log("MicrophoneSettingsModifier Button B, agc = " + agc);
                webRtcAudioDsp.AGC = agc;
            }

            if (OVRInput.GetDown(OVRInput.RawButton.X))
            {
                ns = !ns;
                Debug.Log("MicrophoneSettingsModifier Button X, ns = " + ns);
                webRtcAudioDsp.NoiseSuppression = ns;
            }

            if (OVRInput.GetDown(OVRInput.RawButton.Y))
            {
                vad = !vad;
                Debug.Log("MicrophoneSettingsModifier Button Y, vad = " + vad);
                webRtcAudioDsp.VAD = vad;
            }

            if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickRight))
            {
                agcCompressionGain++;
                Debug.Log("MicrophoneSettingsModifier joystick R right, agcCompressionGain = " + agcCompressionGain);
                webRtcAudioDsp.AgcCompressionGain = agcCompressionGain;
            }

            if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickLeft))
            {
                agcCompressionGain--;
                Debug.Log("MicrophoneSettingsModifier joystick R left, agcCompressionGain = " + agcCompressionGain);
                webRtcAudioDsp.AgcCompressionGain = agcCompressionGain;
            }

            if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickUp))
            {
                agcTargetLevel++;
                Debug.Log("MicrophoneSettingsModifier joystick R up, agcTargetLevel = " + agcTargetLevel);
                webRtcAudioDsp.AgcTargetLevel = agcTargetLevel;
            }

            if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickDown))
            {
                agcTargetLevel--;
                Debug.Log("MicrophoneSettingsModifier joystick R down, agcTargetLevel = " + agcTargetLevel);
                webRtcAudioDsp.AgcTargetLevel = agcTargetLevel;
            }
        }
    }
}
