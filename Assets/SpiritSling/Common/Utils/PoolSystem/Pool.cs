// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpiritSling
{
    [DisallowMultipleComponent]
    public class Pool : MonoBehaviour
    {
        [SerializeField, Tooltip("object to instantiate")]
        private GameObject _prefab;

        [SerializeField, Tooltip("number of objects to precreate")]
        private int _amount;

        [SerializeField, Tooltip("allow to create more objects than amount")]
        private bool _expandable = true;

        public string PoolId => _prefab.name;

        internal GameObject Prefab { get => _prefab; set => _prefab = value; }
        internal int Amount { get => _amount; set => _amount = value; }
        internal bool Expandable { get => _expandable; set => _expandable = value; }

        private List<GameObject> _pooledObjects = new List<GameObject>();

        internal int NbUsed => _pooledObjects.Count(p => !p.GetComponent<PoolObject>().Available);

        internal GameObject GetOne()
        {
            GameObject theOne = null;
            for (var i = 0; i < _pooledObjects.Count; i++)
            {
                if (!_pooledObjects[i].activeSelf && _pooledObjects[i].GetComponent<PoolObject>().Available)
                {
                    theOne = _pooledObjects[i];
                    break;
                }
            }

            if (theOne == null && _expandable)
            {
                theOne = Expand();
            }

            if (theOne == null)
            {
                Log.Error($"[Pool] {PoolId} is empty and non expandable, can't extract an object.");
            }
            else
            {
                theOne.GetComponent<PoolObject>().SetParentPool(this);
                theOne.GetComponent<PoolObject>().Available = false;
            }

            return theOne;
        }

        private static List<IPoolable> _cacheList = new();

        internal void BackToPool(GameObject poolObject)
        {
            //check that it's an object from this pool
            Debug.Assert(
                poolObject != null && poolObject.GetComponent<PoolObject>() != null && _pooledObjects.Contains(poolObject)
                , $"[Pool] trying to release object {poolObject?.name} that is not part of pool {PoolId}");

            //reparent it to this pool gameobject
            poolObject.transform.SetParent(transform, false);

            //reset state for each Ipoolable script on the object
            poolObject.GetComponentsInChildren(_cacheList);
            foreach (var poolable in _cacheList)
            {
                poolable.ResetPoolObject();
            }

            _cacheList.Clear();

            poolObject.SetActive(false);
            poolObject.GetComponent<PoolObject>().Available = true;
        }

        internal void CreatePoolObjects()
        {
            var currentPoolObjects = gameObject.GetComponentsInChildren<PoolObject>(true).ToList();

            //remove extra objects in case amount has changed
            while (currentPoolObjects.Count() > _amount)
            {
                DestroyImmediate(currentPoolObjects.First().gameObject);
                currentPoolObjects.RemoveAt(0);
            }

            _pooledObjects = currentPoolObjects.Select(p => p.gameObject).ToList();

            //create objects up to amount
            if (currentPoolObjects.Count() < _amount)
            {
                var nbToCreate = _amount - currentPoolObjects.Count();
                _amount = currentPoolObjects.Count();
                Expand(nbToCreate);
            }
        }

        internal void Expand(int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                Expand();
            }
        }

        internal IEnumerator ExpandAsync(int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                Expand();
                yield return null;
            }
        }

        // instantiate and add one object in the pool
        private GameObject Expand()
        {
            GameObject obj = null;
#if UNITY_EDITOR

            //in editor, prefer to instantiate a prefab to keep changes in that prefab to the pooled objects
            if (!Application.isPlaying)
            {
                obj = PrefabUtility.InstantiatePrefab(_prefab, transform) as GameObject;
            }
            else
#endif
            {
                obj = Instantiate(_prefab, transform);
            }

            obj.SetActive(false); // disable by default

            if (!obj.TryGetComponent(out PoolObject pool))
                obj.AddComponent<PoolObject>();

            _pooledObjects.Add(obj);
            _amount += 1;

            return obj;
        }

        // remove one object from the pool
        internal bool Shrink(int amount)
        {
            Debug.Assert(
                amount <= _pooledObjects.Count,
                $"[Pool] Trying to remove more pooled objects ({amount}) than the pool contains ({_pooledObjects.Count}).");

            var freeObjects = _pooledObjects.Where(p => p.GetComponent<PoolObject>().Available).ToList();
            Debug.Assert(amount <= freeObjects.Count, $"[Pool] Trying to remove {amount} pooled objects when only {freeObjects.Count} are unused.).");

            for (var i = 0; i < amount && i < freeObjects.Count; i++)
            {
                var go = freeObjects[i];
                _pooledObjects.Remove(go);
                go.GetComponent<PoolObject>().DestroyedByPool = true;
                Destroy(go);
                _amount -= 1;
            }

            Debug.Assert(_amount == _pooledObjects.Count);

            return _pooledObjects.Count == 0;
        }

        internal void RemovePoolObject(GameObject go)
        {
            Debug.Assert(_pooledObjects.Contains(go));
            _pooledObjects.Remove(go);
            _amount -= 1;
        }

        internal void DestroyPoolObjects()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                foreach (var po in gameObject.GetComponentsInChildren<PoolObject>(true))
                {
                    DestroyImmediate(po.gameObject);
                }
            }
            else
            {
                foreach (var go in _pooledObjects)
                {
                    go.GetComponent<PoolObject>().Available = false; //ensure not available because Destroy is not immediate
                    go.GetComponent<PoolObject>().DestroyedByPool = true;

                    Destroy(go);
                }

                _pooledObjects.Clear();
            }
        }
    }
}