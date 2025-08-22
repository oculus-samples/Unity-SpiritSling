// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

[MetaCodeSample("SpiritSling")]
[RequireComponent(typeof(Animator))]
public class VerticalHighlightController : MonoBehaviour
{
    private static int EnableAnimation = Animator.StringToHash("Activate");
    private static int DisableAnimation = Animator.StringToHash("Deactivate");

    [SerializeField]
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void EnableHighlight()
    {
        if (gameObject.activeSelf == false)
        {
            gameObject.SetActive(true);
        }

        animator.SetTrigger(EnableAnimation);
    }

    public void DisableHighlight()
    {
        if (gameObject.activeSelf == false)
        {
            return;
        }

        animator.SetTrigger(DisableAnimation);
    }
}
