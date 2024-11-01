// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpiritSling
{
    [ExecuteInEditMode]
    public class PoolManager : Singleton<PoolManager>
    {
        private List<Pool> _pools = new();

        private List<Pool> Pools
        {
            get
            {
                Clean();
                return _pools;
            }
        }

        [SerializeField]
        private bool _createInEditMode = true;

        public bool CreateInEditMode => _createInEditMode;

        private bool _isDirty = true;

        protected override bool IsPersistent => false;

        private bool _isReady;
        public bool IsReady => _isReady;

#if UNITY_EDITOR || (DEVELOPMENT_BUILD && TRACK_POOL)
        private readonly Dictionary<string, (int nbUsed, int amount)> _poolUsageStats = new();
#endif
        protected override void Awake()
        {
            if (!Application.isEditor || Application.isPlaying)
            {
                base.Awake();
            }

            if (!Application.isPlaying) return;

            if (CreateInEditMode)
            {
                CreatePoolObjects();
            }
            else
            {
                var existingPools = GetComponentsInChildren<Pool>();
                _pools.AddRange(existingPools);
            }
        }

        protected IEnumerator Start()
        {
            if (!CreateInEditMode && Application.isPlaying)
            {
                yield return CreatePoolObjectsAsync();
            }

            _isReady = true;
        }

        protected override void OnApplicationQuit()
        {
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && TRACK_POOL)
            LogUsage();
#endif
            DestroyAllPools();
            base.OnApplicationQuit();
        }

        protected void OnDestroy()
        {
            DestroyAllPools();
        }

#if UNITY_EDITOR

        //only called when something change in the scene
        private void Update()
        {
            if (CreateInEditMode && !Application.isPlaying)
            {
                _isDirty = true;
            }
        }

        public void CreateEmptyPool()
        {
            //create new pool
            var newPoolGo = new GameObject("NewPool");
            newPoolGo.transform.SetParent(transform);

            if (newPoolGo.GetComponent<Pool>() == null)
                newPoolGo.AddComponent<Pool>();

            _isDirty = true;
        }
#endif

        public void CreatePool(GameObject prefabToPool, int amountToPool, bool expandable = false)
        {
            var poolId = prefabToPool.name;

            Debug.Assert(!PoolExists(poolId), $"[Pool] Can't create a pool with identifier {poolId}, one already exists.");

            //create new pool
            var newPoolGo = new GameObject(poolId);
            newPoolGo.transform.SetParent(transform);

            var newPool = newPoolGo.AddComponent<Pool>();
            newPool.Prefab = prefabToPool;
            newPool.Amount = amountToPool;
            newPool.Expandable = expandable;

            newPool.CreatePoolObjects();

            Pools.Add(newPool);
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && TRACK_POOL)
            TrackUsage(newPool);
#endif
        }

        public void CreatePoolObjects()
        {
            //create objects for already existing pools (set from inspector)
            foreach (var p in Pools)
            {
                p.CreatePoolObjects();

#if UNITY_EDITOR || (DEVELOPMENT_BUILD && TRACK_POOL)
                if (Application.isPlaying)
                {
                    TrackUsage(p);
                }
#endif
            }

            Debug.Log($"[Pool] {Pools.Count} Pools initialized.");
        }

        public IEnumerator CreatePoolObjectsAsync()
        {
            //create objects for already existing pools (set from inspector)
            foreach (var p in Pools)
            {
                var savedAmount = p.Amount;
                yield return CreateOrExpandPoolAsync(p.Prefab, p.Amount);

                //(hacky) override amount that has been falsly changed during initial expand
                p.Amount = savedAmount;
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && TRACK_POOL)
                if (Application.isPlaying)
                {
                    TrackUsage(p);
                }
#endif
            }

            Debug.Log($"[Pool] {Pools.Count} Pools initialized.");
        }

        public void ExpandPool(GameObject prefabToExpand, int amount)
        {
            Debug.Assert(
                PoolExists(prefabToExpand), $"[Pool] Can't expand a pool {prefabToExpand.name} that does not exists, use CreatePool() first.");

            GetPool(prefabToExpand).Expand(amount);

#if UNITY_EDITOR || (DEVELOPMENT_BUILD && TRACK_POOL)
            TrackUsage(GetPool(prefabToExpand));
#endif
        }

        public IEnumerator ExpandPoolAsync(GameObject prefabToExpand, int amount)
        {
            Debug.Assert(
                PoolExists(prefabToExpand)
                , $"[Pool] Can't expand a pool {prefabToExpand.name} that does not exists, use CreatePool() first.");

            yield return GetPool(prefabToExpand).ExpandAsync(amount);

#if UNITY_EDITOR || (DEVELOPMENT_BUILD && TRACK_POOL)
            TrackUsage(GetPool(prefabToExpand));
#endif
        }

        public void CreateOrExpandPool(GameObject prefabToPool, int amount)
        {
            if (PoolExists(prefabToPool))
            {
                ExpandPool(prefabToPool, amount);
            }
            else
            {
                CreatePool(prefabToPool, amount, true);
            }
        }

        public IEnumerator CreateOrExpandPoolAsync(GameObject prefabToPool, int amount)
        {
            int amountToAdd = amount;
            if (!PoolExists(prefabToPool))
            {
                CreatePool(prefabToPool, 1, true);
                amountToAdd -= 1;
            }

            yield return ExpandPoolAsync(prefabToPool, amountToAdd);
        }

        public void ShrinkPool(GameObject prefab, int amount)
        {
            Debug.Assert(PoolExists(prefab), $"[Pool] Can't shrink pool, no pools with prefab {prefab.name} exists.");
            var pool = GetPool(prefab);

            if (pool.Shrink(amount))
            {
                Pools.Remove(pool);
                _isDirty = true;
            }
        }

        public void DestroyPool(GameObject prefab)
        {
            Debug.Assert(PoolExists(prefab), $"[Pool] Can't destroy pool, no pools with prefab {prefab.name} exists.");
            var pool = GetPool(prefab);

            pool.DestroyPoolObjects();
            Pools.Remove(pool);
            _isDirty = true;
        }

        public void DestroyPool(string identifier)
        {
            Debug.Assert(PoolExists(identifier), $"[Pool] Can't destroy pool no pools with identifier {identifier} exists.");
            var pool = GetPool(identifier);

            pool.DestroyPoolObjects();
            Pools.Remove(pool);
            _isDirty = true;
        }

        /// <summary>
        /// returns an object from pool
        /// </summary>
        /// <returns>an object from the pool (with active = false)</returns>
        public GameObject GetPoolObject(GameObject prefab)
        {
            return GetPoolObject(GetPool(prefab));
        }

        public GameObject GetPoolObject(string identifier)
        {
            return GetPoolObject(GetPool(identifier));
        }

        private GameObject GetPoolObject(Pool pool)
        {
            var go = pool.GetOne();

#if UNITY_EDITOR || (DEVELOPMENT_BUILD && TRACK_POOL)
            TrackUsage(pool);
#endif
            return go;
        }

        public int GetNbUsedPoolObjects(GameObject prefab)
        {
            return GetPool(prefab).NbUsed;
        }

        public bool PoolExists(GameObject prefab)
        {
            return Pools.Any(p => p.Prefab == prefab);
        }

        public bool PoolExists(string identifier)
        {
            return Pools.Any(p => p.PoolId == identifier);
        }

        public void DestroyAllPools()
        {
            foreach (var p in Pools)
            {
                p.DestroyPoolObjects();
            }

            Pools.Clear();
            _isDirty = true;
        }

#if UNITY_EDITOR || (DEVELOPMENT_BUILD && TRACK_POOL)
        private void TrackUsage(Pool pool)
        {
            var poolId = pool.PoolId;
            var amount = pool.Amount;
            var nbUsed = pool.NbUsed;

            if (_poolUsageStats.ContainsKey(poolId))
            {
                if (_poolUsageStats[poolId].nbUsed < nbUsed || _poolUsageStats[poolId].amount < amount)
                {
                    _poolUsageStats[poolId] = (nbUsed, amount);
                }
            }
            else
            {
                _poolUsageStats.Add(poolId, (nbUsed, amount));
            }
        }

        private void LogUsage()
        {
            var log = "[Pool] Stats of max pool objects used :\n";
            var nbLines = 1;
            foreach (var kvp in _poolUsageStats)
            {
                log += $"{kvp.Key}: {kvp.Value.nbUsed}/{kvp.Value.amount}\n";

                // log every 10 lines
                if (++nbLines >= 10)
                {
                    Debug.Log(log);
                    log = "";
                    nbLines = 0;
                }
            }

            if (nbLines > 0)
            {
                Debug.Log(log);
            }
        }
#endif
        private Pool GetPool(GameObject prefab)
        {
            Debug.Assert(PoolExists(prefab), $"[Pool] Can't get pool, no pools with prefab '{prefab.name}' exists.");
            return Pools.FirstOrDefault(p => p.Prefab == prefab);
        }

        private Pool GetPool(string identifier)
        {
            Debug.Assert(PoolExists(identifier), $"[Pool] Can't get pool, no pools with identifier '{identifier}' exists.");
            return Pools.FirstOrDefault(p => p.PoolId == identifier);
        }

        private void Clean()
        {
            if (_isDirty)
            {
                transform.GetComponentsInChildren(true, _pools);
                _isDirty = false;
            }
        }
    }
}