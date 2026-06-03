using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using MediProfen.Objectives;
using MediProfen.Data;
using TMPro;

namespace MediProfen.Interactions
{
    public class AbdominalThrustManager : MonoBehaviour
    {
        [Header("Objective Target")]
        public string objectiveTargetId = "AbdominalThrust";
        public ObjectiveCompletionType completionType = ObjectiveCompletionType.Trigger;

        [Header("Objective Runner (Optional)")]
        [Tooltip("Runner objective pada scene. Jika kosong, akan dicari otomatis.")]
        public ObjectiveRunner objectiveRunner;

        [Header("Collider Settings")]
        [Tooltip("Collider trigger untuk area perut korban")]
        public Collider stomachCollider;

        [Header("Markers")]
        [Tooltip("Marker hentak perut yang muncul saat objective Abdominal Thrust aktif")]
        public GameObject stomachThrustMarker;

        [Header("Thrust Settings")]
        public int requiredThrusts = 5;
        public float gripThreshold = 0.5f;
        [Tooltip("Jarak tarikan ke arah player (dalam meter)")]
        public float pullDistanceThreshold = 0.10f; 
        [Tooltip("Waktu jeda antar tarikan (detik)")]
        public float cooldownTime = 0.5f;

        [Header("UI Feedback (Optional)")]
        public TextMeshProUGUI counterText;

        [Header("Animation")]
        [Tooltip("Animator pada model korban")]
        public Animator victimAnimator;
        [Tooltip("Nama state animasi saat perut mulai digenggam (opsional)")]
        public string grabStateName = "";
        [Tooltip("Nama state animasi saat perut dilepas (opsional)")]
        public string releaseStateName = "Idle";
        [Tooltip("Nama state animasi saat ditarik/dihentak sukses (contoh: thrust_hit)")]
        public string thrustStateName = "thrust_hit";
        [Tooltip("Nama state animasi saat objektif selesai dan memuntahkan objek")]
        public string vomitStateName = "vomit_pose";
        [Tooltip("Nama state animasi idle setelah animasi objek keluar selesai")]
        public string postVomitIdleStateName = "";
        [Tooltip("Jeda sebelum kembali ke idle setelah animasi objek keluar dimulai")]
        public float postVomitIdleDelay = 2.0f;
        [Tooltip("Matikan Animator setelah animasi objek keluar selesai agar tidak kembali ke animasi tersedak")]
        public bool disableAnimatorAfterVomit = true;

        [Header("Choking Object Spawner (Selesai Objective)")]
        [Tooltip("Prefab objek yang membuat tersedak (harus memiliki Rigidbody)")]
        public GameObject chokingObjectPrefab;
        [Tooltip("Posisi mulut korban tempat objek akan dimuntahkan")]
        public Transform mouthTransform;
        [Tooltip("Kekuatan dorongan objek saat dimuntahkan")]
        public float spitForce = 2.5f;

        [Header("Audio (Opsional)")]
        [Tooltip("Komponen AudioSource untuk memutar efek suara")]
        public AudioSource sfxSource;
        [Tooltip("Efek suara saat hentakan (thrust) berhasil")]
        public AudioClip thrustSound;
        [Tooltip("Efek suara saat objektif ini selesai dan objek muntah")]
        public AudioClip successSound;

        private int currentThrusts = 0;
        private bool isCompleted = false;
        private float lastThrustTime = 0f;

        private class TrackedHand
        {
            public GameObject colliderObject;
            public AnimateHandOnInput animateHand;
            public GameObject visualObject;
            
            public bool isInsideTrigger;
            public bool isGrabbing;
            
            public Transform originalParent;
            public Vector3 originalLocalPos;
            public Quaternion originalLocalRot;

            public Vector3 lockLocalPos;
            public Quaternion lockLocalRot;
        }

        private List<TrackedHand> trackedHands = new List<TrackedHand>();

        // State Tarikan
        private bool isGrabbingStomach = false;
        private Vector3 grabStartAveragePos;
        private bool hasThrustThisCycle = false;

        private void Awake()
        {
            ResolveObjectiveRunner();
        }

        private void Start()
        {
            UpdateCounterUI();

            if (stomachCollider != null)
            {
                var relay = stomachCollider.gameObject.AddComponent<SimpleTriggerRelay>();
                relay.onEnter = this.RelayTriggerEnter;
                relay.onExit = this.RelayTriggerExit;
            }

            UpdateMarkersForCurrentObjective();
        }

        private void SetCollidersVisibility(bool visible)
        {
            if (stomachCollider != null)
            {
                var renderer = stomachCollider.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.enabled = visible;
            }
        }

        private void OnEnable()
        {
            ResolveObjectiveRunner();
            if (objectiveRunner != null)
            {
                objectiveRunner.ObjectiveChanged += HandleObjectiveChanged;
                objectiveRunner.ScenarioCompleted += HandleScenarioCompleted;
            }

            if (!isCompleted) SetCollidersVisibility(true);
            UpdateMarkersForCurrentObjective();
        }

        private void OnDisable()
        {
            if (objectiveRunner != null)
            {
                objectiveRunner.ObjectiveChanged -= HandleObjectiveChanged;
                objectiveRunner.ScenarioCompleted -= HandleScenarioCompleted;
            }

            SetCollidersVisibility(false);
            SetMarkersVisibility(false);
        }

        private void ResolveObjectiveRunner()
        {
            if (objectiveRunner == null)
            {
                objectiveRunner = FindAnyObjectByType<ObjectiveRunner>();
            }
        }

        private void HandleObjectiveChanged(ObjectiveData objective, int index, int total)
        {
            SetMarkersVisibility(!isCompleted && IsMatchingObjective(objective));
        }

        private void HandleScenarioCompleted(ScenarioData scenario)
        {
            SetMarkersVisibility(false);
        }

        private void UpdateMarkersForCurrentObjective()
        {
            ObjectiveData objective = objectiveRunner != null ? objectiveRunner.CurrentObjective : null;
            SetMarkersVisibility(!isCompleted && IsMatchingObjective(objective));
        }

        private bool IsMatchingObjective(ObjectiveData objective)
        {
            return objective != null && objective.Matches(objectiveTargetId, completionType);
        }

        private void SetMarkersVisibility(bool visible)
        {
            if (stomachThrustMarker != null) stomachThrustMarker.SetActive(visible);
        }

        public void RelayTriggerEnter(Collider other)
        {
            if (isCompleted) return;

            if (other.CompareTag("Hand"))
            {
                var tracked = trackedHands.Find(h => h.colliderObject == other.gameObject);
                if (tracked != null)
                {
                    tracked.isInsideTrigger = true;
                    return;
                }

                var animateHand = other.GetComponent<AnimateHandOnInput>();
                if (animateHand == null && other.transform.parent != null)
                {
                    animateHand = other.transform.parent.GetComponentInChildren<AnimateHandOnInput>();
                }
                if (animateHand == null)
                {
                    animateHand = other.GetComponentInParent<AnimateHandOnInput>();
                }

                GameObject visualObject = (animateHand != null) ? animateHand.gameObject : other.gameObject;

                var newHand = new TrackedHand
                {
                    colliderObject = other.gameObject,
                    animateHand = animateHand,
                    visualObject = visualObject,
                    isInsideTrigger = true,
                    isGrabbing = false
                };
                trackedHands.Add(newHand);
            }
        }

        public void RelayTriggerExit(Collider other)
        {
            if (other.CompareTag("Hand"))
            {
                var hand = trackedHands.Find(h => h.colliderObject == other.gameObject);
                if (hand != null)
                {
                    hand.isInsideTrigger = false;
                }
            }
        }

        private void Update()
        {
            if (isCompleted) return;

            // 1. Bersihkan tangan yang sudah keluar dari trigger DAN tidak sedang menahan grip
            for (int i = trackedHands.Count - 1; i >= 0; i--)
            {
                var hand = trackedHands[i];
                float grip = GetGripValue(hand);
                bool isGripActive = grip >= gripThreshold;

                if (!hand.isInsideTrigger && !isGripActive)
                {
                    if (hand.isGrabbing)
                    {
                        RestoreHandVisual(hand);
                    }
                    trackedHands.RemoveAt(i);
                }
            }
            

            // 2. Evaluasi state tangan
            int grabbingHandsCount = 0;
            int readyToGrabCount = 0;

            foreach (var hand in trackedHands)
            {
                float grip = GetGripValue(hand);
                bool isGripActive = grip >= gripThreshold;
                hand.isGrabbing = isGripActive;

                if (isGripActive)
                {
                    grabbingHandsCount++;
                }

                if (hand.isInsideTrigger && isGripActive)
                {
                    readyToGrabCount++;
                }
            }

            bool shouldBeInGrabState = false;

            if (isGrabbingStomach)
            {
                // Jika sedang grab, izinkan keluar asalkan grip dipertahankan
                if (grabbingHandsCount >= 2)
                {
                    shouldBeInGrabState = true;
                }
            }
            else
            {
                // Mulai grab harus kedua tangan di dalam trigger
                if (readyToGrabCount >= 2)
                {
                    shouldBeInGrabState = true;
                }
            }

            if (shouldBeInGrabState)
            {
                if (!isGrabbingStomach)
                {
                    EnterGrabState();
                }
                UpdateGrabAndPull();
            }
            else
            {
                if (isGrabbingStomach)
                {
                    ExitGrabState();
                }
            }
        }

        private void EnterGrabState()
        {
            isGrabbingStomach = true;
            grabStartAveragePos = GetAveragePos();
            hasThrustThisCycle = false;
            
            Debug.Log("[AbdominalThrustManager] Enter grab state - Hands locked to stomach!");

            // Trigger animasi saat bersiap melakukan thrust (opsional)
            if (victimAnimator != null && !string.IsNullOrEmpty(grabStateName))
            {
                victimAnimator.CrossFade(grabStateName, 0.2f);
            }

            foreach (var hand in trackedHands)
            {
                if (hand.visualObject != null && hand.visualObject != hand.colliderObject)
                {
                    hand.originalParent = hand.visualObject.transform.parent;
                    hand.originalLocalPos = hand.visualObject.transform.localPosition;
                    hand.originalLocalRot = hand.visualObject.transform.localRotation;

                    Transform referenceTransform = stomachCollider != null ? stomachCollider.transform : this.transform;

                    hand.lockLocalPos = referenceTransform.InverseTransformPoint(hand.visualObject.transform.position);
                    hand.lockLocalRot = Quaternion.Inverse(referenceTransform.rotation) * hand.visualObject.transform.rotation;

                    Transform safeParent = hand.originalParent != null ? hand.originalParent.parent : null;
                    hand.visualObject.transform.SetParent(safeParent, true);
                }
            }
        }

        private void ExitGrabState()
        {
            isGrabbingStomach = false;
            Debug.Log("[AbdominalThrustManager] Exit grab state - Hands released!");

            // Kembali ke animasi semula (opsional)
            if (victimAnimator != null && !string.IsNullOrEmpty(releaseStateName))
            {
                victimAnimator.CrossFade(releaseStateName, 0.2f);
            }

            foreach (var hand in trackedHands)
            {
                RestoreHandVisual(hand);
            }
            hasThrustThisCycle = false;
        }

        private void UpdateGrabAndPull()
        {
            Vector3 currentAveragePos = GetAveragePos();
            Transform referenceTransform = stomachCollider != null ? stomachCollider.transform : this.transform;
            
            Vector3 pullAxis = -referenceTransform.forward;
            Vector3 rawDisplacement = currentAveragePos - grabStartAveragePos;
            float pullAmount = Vector3.Dot(rawDisplacement, pullAxis);

            foreach (var hand in trackedHands)
            {
                if (hand.visualObject != null && hand.visualObject != hand.colliderObject && hand.originalParent != null)
                {
                    if (hand.visualObject.transform.parent != hand.originalParent)
                    {
                        Vector3 worldLockedPos = referenceTransform.TransformPoint(hand.lockLocalPos);
                        hand.visualObject.transform.position = worldLockedPos + (pullAxis * pullAmount);
                        hand.visualObject.transform.rotation = referenceTransform.rotation * hand.lockLocalRot;
                    }
                }
            }

            if (pullAmount >= pullDistanceThreshold * 0.8f)
            {
                if (!hasThrustThisCycle)
                {
                    if (Time.time - lastThrustTime >= cooldownTime)
                    {
                        hasThrustThisCycle = true;
                        currentThrusts++;
                        lastThrustTime = Time.time;
                        UpdateCounterUI();
                        TriggerHapticFeedback(0.8f, 0.2f);
                        Debug.Log($"[AbdominalThrustManager] Thrust Sukses! {currentThrusts}/{requiredThrusts}");

                        // Mainkan animasi hentakan (hit) perut
                        if (victimAnimator != null && !string.IsNullOrEmpty(thrustStateName))
                        {
                            victimAnimator.Play(thrustStateName, 0, 0f);
                        }

                        // Putar suara hentakan
                        if (sfxSource != null && thrustSound != null)
                        {
                            sfxSource.PlayOneShot(thrustSound);
                        }

                        if (currentThrusts >= requiredThrusts)
                        {
                            CompleteThrust();
                        }
                    }
                }
            }
            else if (pullAmount <= pullDistanceThreshold * 0.2f)
            {
                hasThrustThisCycle = false;
            }
        }

        private void RestoreHandVisual(TrackedHand hand)
        {
            if (hand.visualObject != null && hand.visualObject != hand.colliderObject && hand.originalParent != null)
            {
                if (hand.visualObject.transform.parent != hand.originalParent)
                {
                    hand.visualObject.transform.SetParent(hand.originalParent, true);
                    hand.visualObject.transform.localPosition = hand.originalLocalPos;
                    hand.visualObject.transform.localRotation = hand.originalLocalRot;
                }
            }
        }

        private float GetGripValue(TrackedHand hand)
        {
            if (hand.animateHand != null && hand.animateHand.gripValue.action != null)
            {
                return hand.animateHand.gripValue.action.ReadValue<float>();
            }
            return 0f;
        }

        private Vector3 GetAveragePos()
        {
            if (trackedHands.Count < 2) return Vector3.zero;
            return (trackedHands[0].colliderObject.transform.position + trackedHands[1].colliderObject.transform.position) / 2.0f;
        }

        private void TriggerHapticFeedback(float amplitude, float duration)
        {
            foreach (var hand in trackedHands)
            {
                if (hand.colliderObject != null)
                {
                    var controller = hand.colliderObject.GetComponentInParent<ActionBasedController>();
                    if (controller != null)
                    {
                        controller.SendHapticImpulse(amplitude, duration);
                    }
                }
            }
        }

        private void UpdateCounterUI()
        {
            if (counterText != null)
                counterText.text = $"{currentThrusts} / {requiredThrusts}";
        }

        private void CompleteThrust()
        {
            isCompleted = true;
            Debug.Log("[AbdominalThrustManager] Objektif Abdominal Thrust Selesai!");

            SetCollidersVisibility(false); // Matikan warna penanda
            SetMarkersVisibility(false);

            // Suara sukses
            if (sfxSource != null && successSound != null)
            {
                sfxSource.PlayOneShot(successSound);
            }

            foreach(var hand in trackedHands)
            {
                RestoreHandVisual(hand);
            }

            // Memutar animasi terakhir (muntah)
            if (victimAnimator != null && !string.IsNullOrEmpty(vomitStateName))
            {
                victimAnimator.CrossFade(vomitStateName, 0.2f);

                if (disableAnimatorAfterVomit || !string.IsNullOrEmpty(postVomitIdleStateName))
                {
                    StartCoroutine(ReturnToIdleAfterVomit());
                }
            }

            // Memunculkan objek yang membuat tersedak dari mulut
            if (chokingObjectPrefab != null && mouthTransform != null)
            {
                GameObject chokedObj = Instantiate(chokingObjectPrefab, mouthTransform.position, mouthTransform.rotation);
                Rigidbody rb = chokedObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Tembakkan ke arah depan relatif terhadap wajah/mulut korban
                    rb.AddForce(mouthTransform.forward * spitForce, ForceMode.Impulse);
                }
            }

            ObjectiveEvents.RaiseTargetCompleted(objectiveTargetId, completionType);
        }

        private IEnumerator ReturnToIdleAfterVomit()
        {
            yield return new WaitForSeconds(postVomitIdleDelay);

            if (victimAnimator != null && !string.IsNullOrEmpty(postVomitIdleStateName))
            {
                victimAnimator.CrossFade(postVomitIdleStateName, 0.2f);
            }

            if (victimAnimator != null && disableAnimatorAfterVomit)
            {
                victimAnimator.enabled = false;
            }
        }
    }
}
