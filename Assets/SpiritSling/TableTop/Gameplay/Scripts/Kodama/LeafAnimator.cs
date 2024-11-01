// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Should be placed on the main kodama rig to get the events from the kodama animations.
    /// </summary>
    public class LeafAnimator : MonoBehaviour
    {
        private static readonly int DEPLOYING_IN_AIR_ID = Animator.StringToHash("DeployingInAir");
        private static readonly int IMMUNE_ID = Animator.StringToHash("Shielded?");
        private static readonly int HIDE_LEAF_ID = Animator.StringToHash("HideLeaf?");
        private static readonly int PUSHED_SHIELDED_ID = Animator.StringToHash("PushedShielded?");

        private static readonly int SHIELD_ACTIVATE_ID = Animator.StringToHash("Activate");
        private static readonly int SHIELD_DEACTIVATE_ID = Animator.StringToHash("Deactivate");
        private static readonly int SHIELD_IMPACT_ID = Animator.StringToHash("Impact");


        [SerializeField]
        private Animator leafAnimator;

        [SerializeField]
        private Animator shieldAnimator;

        [SerializeField]
        private AnimationClip leafShieldEnd;

        [SerializeField]
        private AnimationClip pushedShielded;

        private WaitForSeconds waitToDisableLeaf;
        private WaitForSeconds waitForPushedShielded;

        private void Awake()
        {
            waitToDisableLeaf = new WaitForSeconds(leafShieldEnd.length);
            waitForPushedShielded = new WaitForSeconds(pushedShielded.length);
        }

        public void StartDeployingInAir()
        {
            leafAnimator.gameObject.SetActive(true);
            leafAnimator.SetTrigger(DEPLOYING_IN_AIR_ID);

            shieldAnimator.SetTrigger(SHIELD_DEACTIVATE_ID);
            leafAnimator.SetBool(IMMUNE_ID, false);
        }

        public void StartShield()
        {
            shieldAnimator.ResetTrigger(SHIELD_IMPACT_ID);
            leafAnimator.gameObject.SetActive(true);
            leafAnimator.SetBool(IMMUNE_ID, true);

            shieldAnimator.gameObject.SetActive(true);
            shieldAnimator.SetTrigger(SHIELD_ACTIVATE_ID);
        }

        public void EndShield()
        {
            shieldAnimator.ResetTrigger(SHIELD_IMPACT_ID);
            leafAnimator.SetBool(IMMUNE_ID, false);
            shieldAnimator.SetTrigger(SHIELD_DEACTIVATE_ID);
            StartCoroutine(DisableLeaf());
        }

        public void ImpactShield()
        {
            shieldAnimator.SetTrigger(SHIELD_IMPACT_ID);
        }

        public void PushedShieldedStart()
        {
            StartCoroutine(PushedShieldedCoroutine());
        }

        private IEnumerator PushedShieldedCoroutine()
        {
            leafAnimator.SetBool(PUSHED_SHIELDED_ID, true);
            yield return waitForPushedShielded;
            leafAnimator.SetBool(PUSHED_SHIELDED_ID, false);
        }

        public void HideLeaf()
        {
            shieldAnimator.ResetTrigger(SHIELD_IMPACT_ID);
            leafAnimator.SetBool(HIDE_LEAF_ID, true);
            StartCoroutine(DisableLeaf());
        }

        private IEnumerator DisableLeaf()
        {
            yield return waitToDisableLeaf;
            shieldAnimator.ResetTrigger(SHIELD_IMPACT_ID);
            leafAnimator.gameObject.SetActive(false);
            shieldAnimator.gameObject.SetActive(false);
            leafAnimator.SetBool(HIDE_LEAF_ID, false);
        }
    }
}