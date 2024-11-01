// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpiritSling
{
    public class SceneLoadTrigger : MonoBehaviour
    {
        [SerializeField]
        private SceneReference[] _scenesToLoad;

        [SerializeField]
        private LoadSceneMode _loadSceneMode = LoadSceneMode.Single;

        [SerializeField]
        private bool _loadOnStart;

        public void LoadScenes()
        {
            SceneLoader.Instance.LoadScenes(_scenesToLoad, _loadSceneMode);
        }

        private IEnumerator Start()
        {
            if (_loadOnStart)
            {
                yield return null;

                yield return SceneLoader.Instance.LoadScenesAsync(_scenesToLoad, _loadSceneMode);
            }
        }
    }
}