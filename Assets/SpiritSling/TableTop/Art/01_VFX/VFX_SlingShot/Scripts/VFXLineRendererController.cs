// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VFXLineRendererController : MonoBehaviour
{
    [SerializeField]
    public Transform _lineEndTargetTransform;

    [SerializeField]
    Vector3 _lineEndOffset;

    LineRenderer _lineRenderer;

    //int _propretyID = Shader.PropertyToID("_Stretch_Distance");

    private void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (_lineRenderer != null && _lineEndOffset != null)
        {
            _lineRenderer.SetPosition(
                _lineRenderer.positionCount - 1, transform.InverseTransformPoint(_lineEndTargetTransform.position) + _lineEndOffset);

            //stretchDebug
            //_lineRenderer.material.SetFloat(_proprety, (_lineRenderer.GetPosition(0) - _lineRenderer.GetPosition(1)).sqrMagnitude);
        }
    }
}