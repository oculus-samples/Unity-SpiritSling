// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ParticleSystem))]
public class WaterParticlePool : MonoBehaviour
{
    [SerializeField]
    LayerMask _layer;

    [SerializeField]
    BoxCollider _waterBoxCollider;

    [SerializeField]
    int _maxCollisionDetection = 15;

    //collider array
    Collider[] _collidersResults; //max collision in pool

    //Particles property
    ParticleSystem _particleSystem;
    ParticleSystem.EmissionModule _emissionModule;
    ParticleSystem.EmitParams emitParams;
    float particleRate = 1.0f;
    float particleRateDistance;

    //dictionaty particle->emitrate and particle->RateOverDistance;
    Dictionary<GameObject, float> particleElapsedTime = new Dictionary<GameObject, float>(); //particle emission per collision by time 
    Dictionary<GameObject, Vector3> particleElapsedDistance = new Dictionary<GameObject, Vector3>(); //particle emission per collision by distance 

    private bool alive;
    private void Awake()
    {
        _collidersResults = new Collider[_maxCollisionDetection];
    }

    private void Start()
    { 
        if (_waterBoxCollider == null)
            _waterBoxCollider = transform.parent.GetComponent<BoxCollider>();

        _particleSystem = GetComponent<ParticleSystem>();
        _emissionModule = _particleSystem.emission;
        _emissionModule.enabled = false;

        particleRate = 1.0f / _emissionModule.rateOverTime.constant;

        if (_emissionModule.rateOverDistance.constant != 0)
        {
            particleRateDistance = 0.05f / _emissionModule.rateOverDistance.constant;
            particleRateDistance *= particleRateDistance; //for sqrMagnitude
        }

        alive = true;
        StartCoroutine(UpdateSplash());
    }

    private void OnDestroy()
    {
        alive = false;
    }

    private IEnumerator UpdateSplash()
    {
        while (alive)
        {
            var num = Physics.OverlapBoxNonAlloc(
                transform.position + _waterBoxCollider.center, _waterBoxCollider.size,
                _collidersResults, transform.rotation, _layer);
            if (num == 0)
            {
                yield return new WaitForFixedUpdate();
            }
            else
            {
                for (var i = 0; i < num; i++)
                {
                    var c = _collidersResults[i];
                    if (c != null && c != _waterBoxCollider)
                    {
                        //Debug.Log("COLLIDER : " + collider.gameObject.name);
                        if (particleRate > 0)
                            EmitRateOverTimeParticleSystem(c);
                        if (particleRateDistance > 0)
                            EmitRateOverDistanceParticleSystem(c);
                    }

                    if (i%10==0)
                    {
                        yield return null;
                    }
                }                   
            }
        }
    }

    private void EmitRateOverTimeParticleSystem(Collider collider)
    {
        if (!particleElapsedTime.ContainsKey(collider.gameObject))
        {
            particleElapsedTime.Add(collider.gameObject, Time.time - 0.5f + Random.Range(0, _particleSystem.main.startDelayMultiplier));
        }

        if (Time.time - particleElapsedTime[collider.gameObject] > particleRate + Random.Range(0, _particleSystem.main.startDelayMultiplier))
        {
            emitParams.position = collider.ClosestPointOnBounds(collider.transform.position);
            _particleSystem.Emit(emitParams, 1);

            particleElapsedTime[collider.gameObject] = Time.time + Random.Range(0, _particleSystem.main.startDelayMultiplier); //store last emit time
        }
    }

    private void EmitRateOverDistanceParticleSystem(Collider collider)
    {
        if (!particleElapsedDistance.ContainsKey(collider.gameObject))
        {
            particleElapsedDistance.Add(collider.gameObject, collider.transform.position);
        }

        var dir = (particleElapsedDistance[collider.gameObject] - collider.transform.position);

        if (dir.sqrMagnitude > particleRateDistance)
        {
            emitParams.applyShapeToPosition = true;
            emitParams.position = collider.ClosestPointOnBounds(collider.transform.position);
            emitParams.velocity = -dir;
            _particleSystem.Emit(emitParams, 3);

            particleElapsedDistance[collider.gameObject] = collider.transform.position; //store last emit position
        }
    }
}