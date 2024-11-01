// Copyright (c) Meta Platforms, Inc. and affiliates.

//https://en.wikipedia.org/wiki/Cubic_Hermite_spline
using UnityEngine;
using System.Collections;

namespace SpiritSling.TableTop
{
    [RequireComponent(typeof(LineRenderer))]
    public class HermiteSpline : MonoBehaviour
    {
         
        [SerializeField] private Transform _endTransform;
        [SerializeField] private int _numberOfPoints = 20;
        [SerializeField] private float _curvePower=0.3f;

        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private ParticleSystem _particleSystem;
        [SerializeField] private Vector2 _randomEmitDelayAndPosition = new Vector2(0.05f, 0.02f);

        [SerializeField] private bool _enabled = true; 
        [SerializeField] private bool loop = true;
        ParticleSystem.EmitParams _emitParams = new ParticleSystem.EmitParams();

        float _percent = 0;
        bool _psEmitRoutineRunning =false;
        private Vector3[] _controlPoints;
        int _propertyID = Shader.PropertyToID("_Vertical_Transition");

        private void Update()
        {
            UpdateLineRender();
        }

        public void SetEnable(bool enable)
        {
            _enabled = enable;
        }

        [ContextMenu("UpdateLineRender")]
        public void UpdateLineRender()
        {
            if (!_enabled || null == _lineRenderer || _endTransform == null || _particleSystem==null)
                return;

            _controlPoints = new Vector3[3];
            _controlPoints[0] = transform.position;
            _controlPoints[1] = (transform.position + _endTransform.position) / 2 + (transform.position - _endTransform.position).magnitude * Vector3.up * _curvePower;
            _controlPoints[2] = _endTransform.position;

            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = _numberOfPoints * (_controlPoints.Length - 1);

            // loop over segments of spline
            Vector3 p0, p1, m0, m1;

            //Hermite spline
            for (int j = 0; j < _controlPoints.Length - 1; j++)
            {
                // check control points
                if (_controlPoints[j] == null ||
                    _controlPoints[j + 1] == null ||
                    (j > 0 && _controlPoints[j - 1] == null) ||
                    (j < _controlPoints.Length - 2 && _controlPoints[j + 2] == null))
                {
                    return;
                }
                // determine control points of segment
                p0 = _controlPoints[j];
                p1 = _controlPoints[j + 1];

                if (j > 0)
                {
                    m0 =  (_controlPoints[j + 1]- _controlPoints[j - 1]);
                }
                else
                {
                    m0 = _controlPoints[j + 1]  - _controlPoints[j];
                }
                if (j < _controlPoints.Length - 2)
                {
                    m1 =  (_controlPoints[j + 2] - _controlPoints[j]);
                }
                else
                {
                    m1 = _controlPoints[j + 1] - _controlPoints[j];
                }

                //m0 *= 1.2f;
                //m1 *= 1.2f;

                // set points of Hermite curve
                Vector3 position;
                float t;
                float pointStep = 1.0f / _numberOfPoints;

                if (j == _controlPoints.Length - 2)
                {
                    pointStep = 1.0f / (_numberOfPoints - 1.0f);
                    // last point of last segment should reach p1
                }
                for (int i = 0; i < _numberOfPoints; i++)
                {
                    t = i * pointStep;
                    position = (2.0f * t * t * t - 3.0f * t * t + 1.0f) * p0
                        + (t * t * t - 2.0f * t * t + t) * m0
                        + (-2.0f * t * t * t + 3.0f * t * t) * p1
                        + (t * t * t - t * t) * m1;
                    _lineRenderer.SetPosition(i + j * _numberOfPoints, position);
                }
            }

            if (!_psEmitRoutineRunning)
                StartCoroutine(EmitParticlesAlongHermite());
        }

        public void SetEndPosition(Vector3 pos)
        {
            _endTransform.position = pos;
        }

        public IEnumerator EmitParticlesAlongHermite()
        {
            _psEmitRoutineRunning = true;

            for (int i = 0; i< _lineRenderer.positionCount;i++)
            {

                _percent = 1f * (i + 1) / _lineRenderer.positionCount ;
                _psEmitRoutineRunning = true;
                _emitParams.position = transform.InverseTransformPoint(_lineRenderer.GetPosition(i) + Vector3.up*Random.Range(-_randomEmitDelayAndPosition.y , _randomEmitDelayAndPosition.y));
                _particleSystem.Emit(_emitParams, Random.Range(1, 0));

                yield return new WaitForSeconds(_randomEmitDelayAndPosition.x);
                //also animate material along spline
                _lineRenderer.material.SetFloat(_propertyID, _percent);
            }

            _lineRenderer.material.SetFloat(_propertyID, 0);

            _psEmitRoutineRunning = false;

            if (!loop)
                _enabled = false;
        }

    }
}