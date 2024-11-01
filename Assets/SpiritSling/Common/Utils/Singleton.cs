// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace SpiritSling
{
    public abstract class Singleton<T> : MonoBehaviour where T : Component
    {
        #region Fields

        /// <summary>
        /// The instance.
        /// </summary>
        protected static T instance;

        private static bool _shuttingDown;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.Assert(
                        !_shuttingDown, string.Format(
                            "Warning: you are trying to create {0} on application quit", typeof(T)));
                    instance = FindObjectOfType<T>();
                    if (instance == null)
                    {
                        var obj = new GameObject(typeof(T).Name);
                        instance = obj.AddComponent<T>();
                    }
                }

                return instance;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                return !_shuttingDown && instance != null;
            }
        }

        protected virtual bool IsPersistent
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Use this for initialization.
        /// </summary>
        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                if (IsPersistent)
                {
                    DontDestroyOnLoad(transform.root.gameObject);
                }
            }
            else if (instance != this as T)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _shuttingDown = true;
        }

        #endregion
    }
}