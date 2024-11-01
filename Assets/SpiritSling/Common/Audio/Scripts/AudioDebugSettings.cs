// Copyright (c) Meta Platforms, Inc. and affiliates.

using Photon.Voice;
using Photon.Voice.Unity;
using SpiritSling.TableTop;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritSling
{
    public class AudioDebugSettings : MonoBehaviour
    {
        [SerializeField]
        private Button applyButton;

        [SerializeField]
        private AudioClip applySound;

        [Header("Recorder")]
        [SerializeField]
        private SettingsToggleWidget echoMode;

        [SerializeField]
        private SettingWidget frameDuration;

        [SerializeField]
        private SettingWidget bitRate;

        [SerializeField]
        private SettingsToggleWidget microphoneType;

        [Header("Web RTC Audio DSP")]
        [SerializeField]
        private SettingsToggleWidget enableDsp;

        [SerializeField]
        private SettingsToggleWidget aec;

        [SerializeField]
        private SettingWidget aecReverseStreamDelay;

        [SerializeField]
        private SettingsToggleWidget aecHighPass;

        [SerializeField]
        private SettingsToggleWidget agc;

        [SerializeField]
        private SettingWidget agcCompressionGain;

        [SerializeField]
        private SettingWidget agcTargetLevel;

        [SerializeField]
        private SettingsToggleWidget highPass;

        [SerializeField]
        private SettingsToggleWidget noiseSuppression;

        [SerializeField]
        private SettingsToggleWidget autoVoiceDetection;

        private void Awake()
        {
            applyButton.onClick.AddListener(ApplySettings);
        }

        private void ApplySettings()
        {
            var recorder = BaseTabletopPlayer.LocalPlayer.GetComponentInChildren<Recorder>();
            var webRtcAudioDsp = recorder.GetComponent<WebRtcAudioDsp>();

            AudioManager.Instance.Play(applySound, AudioMixerGroups.UI);

            recorder.DebugEchoMode = echoMode.Value;
            recorder.FrameDuration = frameDuration.Value switch
            {
                10 => OpusCodec.FrameDuration.Frame10ms,
                40 => OpusCodec.FrameDuration.Frame40ms,
                60 => OpusCodec.FrameDuration.Frame60ms,
                _ => OpusCodec.FrameDuration.Frame20ms,
            };
            recorder.Bitrate = bitRate.Value;
            recorder.MicrophoneType = microphoneType.Value ? Recorder.MicType.Unity : Recorder.MicType.Photon;

            webRtcAudioDsp.enabled = enableDsp.Value;

            webRtcAudioDsp.AEC = aec.Value;
            webRtcAudioDsp.ReverseStreamDelayMs = aecReverseStreamDelay.Value;
            webRtcAudioDsp.AecHighPass = aecHighPass.Value;

            webRtcAudioDsp.AGC = agc.Value;
            webRtcAudioDsp.AgcCompressionGain = agcCompressionGain.Value;
            webRtcAudioDsp.AgcTargetLevel = agcTargetLevel.Value;

            webRtcAudioDsp.HighPass = highPass.Value;
            webRtcAudioDsp.NoiseSuppression = noiseSuppression.Value;
            webRtcAudioDsp.VAD = autoVoiceDetection.Value;
        }
    }
}