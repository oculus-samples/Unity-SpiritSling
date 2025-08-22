// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Meta.XR.Samples;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Handles the in-game tutorials for different phases of the tabletop game.
    /// This class manages the display and hiding of tutorial phases based on the game state.
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class InGameTutorials : NetworkBehaviour
    {
        /// <summary>
        /// Prefab for the first phase of the tutorial.
        /// </summary>
        [SerializeField]
        private GameObject tutorialPhase1Prefab;

        /// <summary>
        /// Prefab for the second phase of the tutorial.
        /// </summary>
        [SerializeField]
        private GameObject tutorialPhase2Prefab;

        /// <summary>
        /// Prefab for the third phase of the tutorial.
        /// </summary>
        [SerializeField]
        private GameObject tutorialPhase3Prefab;

        /// <summary>
        /// Offset for the first phase tutorial instance position.
        /// </summary>
        [SerializeField]
        private Vector3 tutorialPhase1Offset;

        /// <summary>
        /// Offset for the second phase tutorial instance position.
        /// </summary>
        [SerializeField]
        private Vector3 tutorialPhase2Offset;

        /// <summary>
        /// Offset for the third phase tutorial instance position.
        /// </summary>
        [SerializeField]
        private Vector3 tutorialPhase3Offset;

        /// <summary>
        /// Indicates whether the first phase tutorial is completed.
        /// </summary>
        private static bool m_phase1TutorialDone;

        /// <summary>
        /// Indicates whether the second phase tutorial is completed.
        /// </summary>
        private static bool m_phase2TutorialDone;

        /// <summary>
        /// Indicates whether the third phase tutorial is completed.
        /// </summary>
        private static bool m_phase3TutorialDone;

        /// <summary>
        /// Instance of the first phase tutorial.
        /// </summary>
        private GameObject m_tutorialPhase1Instance;

        /// <summary>
        /// Instance of the second phase tutorial.
        /// </summary>
        private GameObject m_tutorialPhase2Instance;

        /// <summary>
        /// Instance of the third phase tutorial.
        /// </summary>
        private GameObject m_tutorialPhase3Instance;

        private GameObject m_lastTutorialDisplayed;

        /// <summary>
        /// Unsubscribes from game events when the object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            TabletopGameEvents.OnGamePhaseChanged -= OnGamePhaseChanged;
            TabletopGameEvents.OnPawnDragEnd -= OnPawnDragEnd;
            TabletopGameEvents.OnPawnDragStart -= OnPawnDragStart;
            TabletopGameEvents.OnPawnDragCanceled -= OnPawnDragCanceled;
            SlingBall.OnShotSequenceDone -= OnSlingBallShotDone;
        }

        /// <summary>
        /// Subscribes to game events when the object is initialized.
        /// </summary>
        private void Awake()
        {
            TabletopGameEvents.OnGamePhaseChanged += OnGamePhaseChanged;
            TabletopGameEvents.OnPawnDragStart += OnPawnDragStart;
            TabletopGameEvents.OnPawnDragEnd += OnPawnDragEnd;
            TabletopGameEvents.OnPawnDragCanceled += OnPawnDragCanceled;
            SlingBall.OnShotSequenceDone += OnSlingBallShotDone;
        }

        /// <summary>
        /// Handles the logic for displaying or hiding tutorial phases based on the game phase.
        /// </summary>
        /// <param name="player">The player whose game phase has changed.</param>
        /// <param name="phase">The current phase of the game.</param>
        private void OnGamePhaseChanged(BaseTabletopPlayer player, TableTopPhase phase)
        {
            if (player.Index != BaseTabletopPlayer.LocalPlayer.Index)
                return;

            m_lastTutorialDisplayed = null;

            switch (phase)
            {
                case TableTopPhase.Move:
                    DisplayTutorialPhase1();
                    break;

                case TableTopPhase.Summon:
                    HideTutorialPhase1();
                    DisplayTutorialPhase2();
                    break;

                case TableTopPhase.Shoot:
                    HideTutorialPhase2();
                    DisplayTutorialPhase3();
                    break;

                case TableTopPhase.EndTurn:
                    HideTutorialPhase3();
                    break;

                case TableTopPhase.Victory:
                    HideTutorialPhase1();
                    HideTutorialPhase2();
                    HideTutorialPhase3();
                    break;
            }
        }

        /// <summary>
        /// Hides the third phase tutorial after a shot sequence is completed.
        /// </summary>
        /// <param name="slingshot">The slingshot that was used in the shot.</param>
        /// <param name="targetShotCell">The target cell for the shot.</param>
        /// <param name="hitCliff">Indicates if the shot hit a cliff.</param>
        private void OnSlingBallShotDone(Slingshot slingshot, HexCell targetShotCell, bool hitCliff)
        {
            // Prevents bots running locally to complete the tutorial
            if (slingshot.Owner == null || !slingshot.Owner.IsHuman)
            {
                return;
            }

            if (!m_phase3TutorialDone)
            {
                m_phase3TutorialDone = true;
                m_tutorialPhase3Instance.SetActive(false);
            }
        }

        /// <summary>
        /// Hides the appropriate tutorial phase after a pawn drag action ends.
        /// </summary>
        /// <param name="comp">The pawn component that was dragged.</param>
        private void OnPawnDragEnd(Pawn comp)
        {
            // Prevents bots running locally to complete the tutorial
            if (comp == null || comp.Owner == null || !comp.Owner.IsHuman)
            {
                return;
            }

            if (comp is Kodama && !m_phase1TutorialDone)
            {
                m_phase1TutorialDone = true;
                m_tutorialPhase1Instance.SetActive(false);
            }
            else if (comp is Slingshot && !m_phase2TutorialDone)
            {
                m_phase2TutorialDone = true;
                m_tutorialPhase2Instance.SetActive(false);
            }
        }

        /// <summary>
        /// Display the last tutoriel if drag is cancelled
        /// </summary>
        private void OnPawnDragCanceled()
        {
            if (m_lastTutorialDisplayed != null)
                m_lastTutorialDisplayed.SetActive(true);
        }

        /// <summary>
        /// Hides the current tutorial when start dragging
        /// </summary>
        private void OnPawnDragStart()
        {
            if (m_tutorialPhase1Instance != null && m_tutorialPhase1Instance.activeSelf)
            {
                HideTutorialPhase1();
                m_lastTutorialDisplayed = m_tutorialPhase1Instance;
            }
            else if (m_tutorialPhase2Instance != null && m_tutorialPhase2Instance.activeSelf)
            {
                HideTutorialPhase2();
                m_lastTutorialDisplayed = m_tutorialPhase2Instance;
            }
            else if (m_tutorialPhase3Instance != null && m_tutorialPhase3Instance.activeSelf)
            {
                HideTutorialPhase3();
                m_lastTutorialDisplayed = m_tutorialPhase3Instance;
            }
        }

        /// <summary>
        /// Displays the third phase tutorial if it has not been completed.
        /// </summary>
        private void DisplayTutorialPhase3()
        {
            if (m_phase3TutorialDone)
                return;

            if (m_tutorialPhase3Instance == null)
            {
                var localPlayerSlingshots = BaseTabletopPlayer.LocalPlayer.Slingshots;
                var container = localPlayerSlingshots[0].transform;

                for (var i = 0; i < localPlayerSlingshots.Count; i++)
                {
                    if (localPlayerSlingshots[i].IsOnGrid)
                        container = localPlayerSlingshots[i].transform;
                }

                m_tutorialPhase3Instance = Instantiate(tutorialPhase3Prefab, container);
                m_tutorialPhase3Instance.transform.localPosition = tutorialPhase3Offset;
            }
            else
            {
                m_tutorialPhase3Instance.SetActive(true);
            }
        }

        /// <summary>
        /// Displays the second phase tutorial if it has not been completed.
        /// </summary>
        private void DisplayTutorialPhase2()
        {
            if (m_phase2TutorialDone)
                return;

            if (m_tutorialPhase2Instance == null)
            {
                var container = BaseTabletopPlayer.LocalPlayer.Slingshots[0].transform;
                m_tutorialPhase2Instance = Instantiate(tutorialPhase2Prefab, container);
                m_tutorialPhase2Instance.transform.localPosition = tutorialPhase2Offset;
            }
            else
            {
                m_tutorialPhase2Instance.SetActive(true);
            }
        }

        /// <summary>
        /// Displays the first phase tutorial if it has not been completed.
        /// </summary>
        private void DisplayTutorialPhase1()
        {
            if (m_phase1TutorialDone)
                return;

            if (m_tutorialPhase1Instance == null)
            {
                var container = BaseTabletopPlayer.LocalPlayer.Kodama.transform;
                m_tutorialPhase1Instance = Instantiate(tutorialPhase1Prefab, container);
                m_tutorialPhase1Instance.transform.localPosition = tutorialPhase1Offset;
            }
            else
            {
                m_tutorialPhase1Instance.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the first phase tutorial instance if it exists.
        /// </summary>
        private void HideTutorialPhase1()
        {
            if (m_tutorialPhase1Instance != null)
            {
                m_tutorialPhase1Instance.SetActive(false);
            }
        }

        /// <summary>
        /// Hides the second phase tutorial instance if it exists.
        /// </summary>
        private void HideTutorialPhase2()
        {
            if (m_tutorialPhase2Instance != null)
            {
                m_tutorialPhase2Instance.SetActive(false);
            }
        }

        /// <summary>
        /// Hides the third phase tutorial instance if it exists.
        /// </summary>
        private void HideTutorialPhase3()
        {
            if (m_tutorialPhase3Instance != null)
            {
                m_tutorialPhase3Instance.SetActive(false);
            }
        }

        private void Update()
        {
            var fwd = Vector3.one;

            if (m_tutorialPhase1Instance != null && m_tutorialPhase1Instance.activeSelf)
            {
                fwd = (m_tutorialPhase1Instance.transform.position - Camera.main.transform.position).SetY(0).normalized;
                m_tutorialPhase1Instance.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
            else if (m_tutorialPhase2Instance != null && m_tutorialPhase2Instance.activeSelf)
            {
                fwd = (m_tutorialPhase2Instance.transform.position - Camera.main.transform.position).SetY(0).normalized;
                m_tutorialPhase2Instance.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
            else if (m_tutorialPhase3Instance != null && m_tutorialPhase3Instance.activeSelf)
            {
                fwd = (m_tutorialPhase3Instance.transform.position - Camera.main.transform.position).SetY(0).normalized;
                m_tutorialPhase3Instance.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
        }
    }
}
