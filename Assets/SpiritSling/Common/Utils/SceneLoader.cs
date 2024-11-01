// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SpiritSling
{
    /// <summary>
    ///     Manager to handle loading and unloading of scenes
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField, Tooltip("First Scenes to load on Start")]
        private SceneReference[] _firstScenes;

        [SerializeField, Tooltip("Loading screen scene that is added during the loading of other scenes")]
        private SceneReference _loadingScene;

        [SerializeField]
        private float _minLoadTimeSec = 1.0f;

        //events
        public UnityEvent<SceneReference> SceneLoaded = new UnityEvent<SceneReference>();

        private static readonly Stack<string> _scenesToUnload = new();

        private static SceneLoader _instance;
        public static SceneLoader Instance => _instance;

        /// <summary>
        ///     Start loading next scene(s) and unloading previous ones
        /// </summary>
        public void LoadScenes(SceneReference scenesToLoad, LoadSceneMode loadSceneMode)
        {
            StartCoroutine(InternalLoadScenes(new[] { scenesToLoad }, loadSceneMode));
        }

        /// <summary>
        ///     Start loading next scene(s) and unloading previous ones
        /// </summary>
        public void LoadScenes(SceneReference[] scenesToLoad, LoadSceneMode loadSceneMode)
        {
            StartCoroutine(InternalLoadScenes(scenesToLoad, loadSceneMode));
        }

        public IEnumerator LoadScenesAsync(SceneReference[] scenesToLoad, LoadSceneMode loadSceneMode)
        {
            yield return InternalLoadScenes(scenesToLoad, loadSceneMode);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"{name} is destroyed because it's duplicated!");
                Destroy(gameObject);
                return;
            }

            if (_instance == null)
            {
                _instance = this;
            }
        }

        private IEnumerator Start()
        {
            if (_firstScenes.Length > 0)
            {
                yield return LoadScenesAsync(_firstScenes, LoadSceneMode.Single);
            }
        }

        private IEnumerator InternalLoadScenes(SceneReference[] scenesToLoad, LoadSceneMode loadSceneMode)
        {
            Application.backgroundLoadingPriority = ThreadPriority.Low;

            //load loading scene
            var loadingScenePath = _loadingScene?.ScenePath;

            if (string.IsNullOrEmpty(loadingScenePath))
            {
                Debug.LogWarning("No Loading Scene defined, consider adding one.");
            }
            else
            {
                var loadLoadingOp = SceneManager.LoadSceneAsync(loadingScenePath, LoadSceneMode.Additive);
                yield return new WaitUntil(() => loadLoadingOp == null || loadLoadingOp.isDone);
            }

            //start loading timer
            var startLoadingTime = Time.realtimeSinceStartup;

            if (loadSceneMode == LoadSceneMode.Single)
            {
                while (_scenesToUnload.Count > 0)
                {
                    var scene = _scenesToUnload.Pop();
                    var unloadSceneOp = SceneManager.UnloadSceneAsync(scene);
                    yield return new WaitUntil(() => unloadSceneOp == null || unloadSceneOp.isDone);
                }

                _scenesToUnload.Clear();
            }

            //extra unload
            var asyncUnload = Resources.UnloadUnusedAssets();
            while (!asyncUnload.isDone)
            {
                yield return null;
            }

            //force some GC
            GC.Collect();
            for (var i = 0; i < 10; ++i)
            {
                yield return null;
            }

            //then load new ones
            foreach (var sceneRef in scenesToLoad)
            {
                var scenePath = sceneRef.ScenePath;
                {
                    var loadSceneOp = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                    if (loadSceneOp != null)
                    {
                        loadSceneOp.allowSceneActivation = false; // Deactivate the load of gameobjects on scene load

                        while (!loadSceneOp.isDone)
                        {
                            if (!loadSceneOp.allowSceneActivation)
                            {
                                if (loadSceneOp.progress < 0.9f)
                                {
                                    yield return null; //initial load of gameobjects
                                }
                                else
                                {
                                    // Once everything is loaded, reactive this variable (could wait for user input here)
                                    loadSceneOp.allowSceneActivation = true;
                                }
                            }
                            else
                            {
                                yield return null; // finish the remaining scene load
                            }
                        }
                    }
                }
                _scenesToUnload.Push(scenePath);
                SceneLoaded?.Invoke(sceneRef);
            }

            //keep first scene as active scene
            if (scenesToLoad.Length > 0)
                SceneManager.SetActiveScene(SceneManager.GetSceneByPath(scenesToLoad[0].ScenePath));

            //wait the minimum time
            yield return new WaitUntil(() => _minLoadTimeSec <= Time.realtimeSinceStartup - startLoadingTime);

            //finally unload loading scene
            if (!string.IsNullOrEmpty(loadingScenePath))
            {
                var unloadLoadingOp = SceneManager.UnloadSceneAsync(loadingScenePath);
                yield return new WaitUntil(() => unloadLoadingOp == null || unloadLoadingOp.isDone);
            }

            Application.backgroundLoadingPriority = ThreadPriority.Normal;
        }
    }
}