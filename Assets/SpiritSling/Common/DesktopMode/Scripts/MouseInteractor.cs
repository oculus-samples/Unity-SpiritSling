// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseInteractor : MonoBehaviour
{
    // Raycast variables
    private Camera mainCamera;
    public float raycastDistance = 100f;
    public float handHeight = 1f;
    public LayerMask interactableLayer;
    public static Vector3 mouse3DPosition;

    // Interaction variables
    private PointableElement currentPointable;
    private bool isInteracting;
    private Vector3 hitPointOnClick;
    private Vector3 offsetToPivot;
    public static Pose pose;
    private int identifier = 1111;

    void Start()
    {
        mainCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        CalculateMousePosition();

        if (isInteracting && Mouse.current.leftButton.isPressed)
        {
            Move();
        }

        if (isInteracting && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Unselect();
        }

        // Perform raycast from mouse position
        var ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance, interactableLayer))
        {
            var hitPointable = hit.collider.GetComponentInChildren<PointableElement>();

            //Debug.Log("Hit! collider:" + hit.collider.name + " pointable:" + hitPointable);

            if (hitPointable != null)
            {
                // Check for interaction on mouse click
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    identifier = Random.Range(1111, 9999);
                    hitPointOnClick = hit.point;

                    //offsetToPivot = hit.collider.transform.InverseTransformPoint(hitPointOnClick);
                    offsetToPivot = hit.collider.transform.position - (hitPointOnClick);

                    //Debug.Log("offsetToPivot:" + offsetToPivot);
                    Select(hitPointable);
                }
                else
                {
                    Hover(hitPointable);
                }
            }
        }
    }

    private void Hover(PointableElement pointable)
    {
        var pe = new PointerEvent(identifier, PointerEventType.Hover, pose);
        pointable.ProcessPointerEvent(pe);
    }

    void Select(PointableElement pointable)
    {
        currentPointable = pointable;
        isInteracting = true;

        pose.position = mouse3DPosition + offsetToPivot;
        ITransformer tr = currentPointable.GetComponent<GrabFreeTransformer>();
        if (tr != null)
        {
            ITransformer tr2 = currentPointable.GetComponent<GrabFreeTransformerV2>();
            if (tr2 == null)
                tr2 = currentPointable.gameObject.AddComponent<GrabFreeTransformerV2>();
            tr2.Initialize(currentPointable as Grabbable);
            ((Grabbable)currentPointable).InjectOptionalOneGrabTransformer(tr2);
        }

        var pe = new PointerEvent(identifier, PointerEventType.Select, pose);
        currentPointable.ProcessPointerEvent(pe);

        //Debug.Log("Mouse Select mouse3DPosition:" + mouse3DPosition + " currentPointable:" + currentPointable.transform.position);
    }

    void Move()
    {
        if (currentPointable != null)
        {
            pose.position = mouse3DPosition + offsetToPivot;

            var pe = new PointerEvent(identifier, PointerEventType.Move, pose);
            currentPointable.ProcessPointerEvent(pe);

            //Debug.Log("Mouse Move mouse3DPosition:" + mouse3DPosition + " currentPointable:" + currentPointable.transform.position);
        }
    }

    void Unselect()
    {
        if (currentPointable != null)
        {
            pose.position = mouse3DPosition + offsetToPivot;

            var pe = new PointerEvent(identifier, PointerEventType.Unselect, pose);
            currentPointable.ProcessPointerEvent(pe);

            //Debug.Log("Mouse Unselect mouse3DPosition:" + mouse3DPosition + " currentPointable:" + currentPointable.transform.position);
        }

        isInteracting = false;
        currentPointable = null;
    }

    protected void CalculateMousePosition()
    {
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        var floorPlane = new Plane(Vector3.up, hitPointOnClick);

        floorPlane.Raycast(ray, out var distance);
        mouse3DPosition = ray.GetPoint(distance - handHeight);
    }
}