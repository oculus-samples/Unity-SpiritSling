// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SpiritSling.TableTop
{
    public class GameVolumeGuide : MonoBehaviour
    {
        [SerializeField]
        private GameObject _guide;
        
        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private float _moveSpeed = 0.5f;
        
        
        [SerializeField]
        private AudioClip[] _spawnAudioClips;

        [SerializeField]
        private AudioClip[] _waitingAudioClips;

        [SerializeField]
        private AudioClip[] _despawnAudioClips;

        
        public UnityEvent ReachedDestination;

        private int _spawnTrigger = Animator.StringToHash("Spawn");
        private int _despawnTrigger = Animator.StringToHash("Despawn");
        private int _walkingTrigger = Animator.StringToHash("Walking");
        private int _stopTrigger = Animator.StringToHash("Stop");

        private Vector3 _currentPos;
        private Quaternion _currentRot;
        private Vector3 _destPos;
        private Quaternion _destRot;

        public void StartGuide(Vector3 startPos, Quaternion startRot, Vector3 destPos, Quaternion destRot)
        {
            _currentPos = startPos;
            _currentRot = startRot * Quaternion.Euler(0,180f,0);
            _destPos = destPos;
            _destRot = destRot * Quaternion.Euler(0,180f,0);
            StartCoroutine(Guide());
        }

        private bool IsGuideInView()
        {
            Transform centerEyeAnchor = DesktopModeEnabler.IsDesktopMode ? Camera.main.transform : OVRManager.instance.GetComponent<OVRCameraRig>().centerEyeAnchor;

            Vector3 directionPlayer = centerEyeAnchor.position - _guide.transform.position;
            float angle = Vector3.Angle(centerEyeAnchor.forward, -directionPlayer);

            // Check if the object is in front of the camera and within the FOV angle
            if (Vector3.Dot(centerEyeAnchor.forward, directionPlayer) < 0 && angle < 30)
                return true;

            return false;
        }

        private bool CanMoveTowardsDestination()
        {
            return true;
        }

        private bool IsAtDestination()
        {
            return Vector3.Distance(_currentPos, _destPos) < 0.02f;
        }

        private IEnumerator RotateTowards(Vector3 target)
        {
            //look at dest
            var direction = (target - _guide.transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(direction);
            Quaternion startRot = _guide.transform.rotation;                
            float elapsedTime = 0f;
            float rotationDuration = 0.3f;

            while (elapsedTime < 0.3f)
            {
                _guide.transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsedTime / rotationDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            _guide.transform.rotation = targetRot;            
        }
        
        private IEnumerator Guide()
        {
            //spawn guide
            _guide.transform.SetPositionAndRotation(_currentPos, _currentRot);
            _guide.SetActive(true);
            yield return null;
            _animator.SetTrigger(_spawnTrigger);
            InternalPlayAudio(_spawnAudioClips, AudioMixerGroups.SFX_KodamaVoices);
            yield return new WaitForSeconds(1.3f);

            var timeSinceLastSound = 0f;

            if (!IsAtDestination())
            {
                yield return RotateTowards(_destPos);

                //move towards dest
                _animator.SetTrigger(_walkingTrigger);

                yield return new WaitForSeconds(0.2f);
                
                while (!IsAtDestination())
                {
                    if (IsGuideInView())//only move if player sees guide
                    {
                        var dir = (_destPos - _currentPos).normalized;
                        _currentPos += dir * _moveSpeed * Time.deltaTime;
                        _guide.transform.position = _currentPos;
                    
                        // Check if we've overshot the destination
                        if (Vector3.Dot(_destPos - _currentPos, dir) < 0)
                        {
                            _guide.transform.position = _destPos; // Snap to destination
                            break;
                        }                        
                    }
                    else
                    {
                        timeSinceLastSound += Time.deltaTime;
                        if(timeSinceLastSound > 5f)
                        {
                            timeSinceLastSound = 0f;
                            InternalPlayAudio(_waitingAudioClips, AudioMixerGroups.SFX_KodamaVoices);
                        }
                    }
                    
                    yield return null;
                }
            }
            //rotate towards player
            Transform centerEyeAnchor = DesktopModeEnabler.IsDesktopMode ? Camera.main.transform : OVRManager.instance.GetComponent<OVRCameraRig>().centerEyeAnchor;
            yield return RotateTowards(centerEyeAnchor.position);
            
            //wait for player to lookat
            while (!IsGuideInView())
            {
                timeSinceLastSound += Time.deltaTime;
                if(timeSinceLastSound > 5f)
                {
                    timeSinceLastSound = 0f;
                    InternalPlayAudio(_waitingAudioClips, AudioMixerGroups.SFX_KodamaVoices);
                }

                yield return null;
            }
            
            //despawn guide
            _animator.SetTrigger(_despawnTrigger);
            InternalPlayAudio(_despawnAudioClips, AudioMixerGroups.SFX_KodamaVoices);
            yield return new WaitForSeconds(2.5f);
            _guide.SetActive(false);

            ReachedDestination?.Invoke();
        }
        
        protected void InternalPlayAudio(AudioClip[] clips, AudioMixerGroups audioGroup)
        {
            AudioManager.Instance.PlayRandom(clips, audioGroup, _guide.transform);
        }
    }
}
