// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Introduction cutscene with Kodama spawn
    /// </summary>
    public class StartGameCutScene : MonoBehaviour
    {
        public PlayableDirector playableDirector;
        public double TimeLeft => playableDirector.playableAsset.duration - playableDirector.time;

        public void Play()
        {
            Log.Debug("Start cutscene: play");
            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            for (byte i = 0; i < 4; i++)
            {
                var kodama = GetKodama(i);
                if (kodama == null)
                {
                    SetTimelineBinding($"Kodama{i + 1}", null);
                    continue;
                }

                SetTimelineBinding($"Kodama{i + 1}", kodama.PawnAnimator.Animator);
                yield return null;
            }

            transform.SetParent(GameVolume.Instance.PlayerPivot, false);
            playableDirector.gameObject.SetActive(true);
            yield return null;

            for (byte i = 0; i < 4; i++)
            {
                var kodama = GetKodama(i);
                if (kodama != null)
                {
                    kodama.SetCutsceneMode(true, false);
                }
            }
            playableDirector.Play();
        }

        public void Stop()
        {
            Log.Debug("Start cutscene: stop");
            StartCoroutine(StopRoutine());
        }

        private IEnumerator StopRoutine()
        {
            // Ensure it is completed
            playableDirector.time = playableDirector.playableAsset.duration;
            yield return null;
            playableDirector.Stop();
            yield return null;
            playableDirector.gameObject.SetActive(false);
            yield return null;
            for (byte i = 0; i < 4; i++)
            {
                var kodama = GetKodama(i);
                if (kodama == null) continue;

                kodama.SetCutsceneMode(true, true);
                yield return null;
            }
        }

        private Kodama GetKodama(byte playerIndex)
        {
            var player = BaseTabletopPlayer.GetByPlayerIndex(playerIndex);
            if (player == null) return null;

            return player.Kodama;
        }

        private void SetTimelineBinding(string track, Object obj)
        {
            var outputs = playableDirector.playableAsset.outputs;
            var itm = outputs.First((itm => itm.streamName == track));

            var previous = playableDirector.GetGenericBinding(itm.sourceObject);
            if (previous != null && previous is GameObject g) g.SetActive(false);
            if (previous != null && previous is Component c) c.gameObject.SetActive(false);

            playableDirector.SetGenericBinding(itm.sourceObject, obj);
        }

        public void CreateStartTileDecal()
        {
            for (byte i = 0; i < 4; i++)
            {
                var kodama = GetKodama(i);
                if (kodama == null) continue;

                kodama.PlaySpawnVFX();
            }
        }
    }
}