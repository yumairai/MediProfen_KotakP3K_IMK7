using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using MediProfen.Objectives;
using TMPro;

namespace MediProfen.Interactions
{
    public class AbdominalThrustManager : MonoBehaviour
    {
        [Header("Objective Target")]
        public string objectiveTargetId = "AbdominalThrust";
        public ObjectiveCompletionType completionType = ObjectiveCompletionType.Trigger;

        [Header("Collider Settings")]
        [Tooltip("Collider trigger untuk area perut korban")]
        public Collider stomachCollider;

        [Header("Thrust Settings")]
        public int requiredThrusts = 5;
        public float gripThreshold = 0.5f;
        [Tooltip("Jarak tarikan ke arah player (dalam meter)")]
        public float pullDistanceThreshold = 0.10f; 
        [Tooltip("Waktu jeda antar tarikan (detik)")]
        public float cooldownTime = 0.5f;

        [Header("UI Feedback (Optional)")]
        public TextMeshProUGUI counterText;

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

        private void Start()
        {
            UpdateCounterUI();

            if (stomachCollider != null)
            {
                var relay = stomachCollider.gameObject.AddComponent<SimpleTriggerRelay>();
                relay.onEnter = this.RelayTriggerEnter;
                relay.onExit = this.RelayTriggerExit;
            }
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
            
            // Sumbu tarikan Abdominal Thrust adalah ditarik mundur ke arah perut/dada player.
            // Asumsi transform.forward adalah menghadap ke depan, maka tarikan adalah -forward.
            Vector3 pullAxis = -referenceTransform.forward;
            
            // Proyeksikan pergerakan controller fisik ke sumbu tarikan
            Vector3 rawDisplacement = currentAveragePos - grabStartAveragePos;
            float pullAmount = Vector3.Dot(rawDisplacement, pullAxis);

            // Gerakkan tangan visual HANYA pada satu sumbu (pullAxis) agar persis seperti CPR
            // Tangan tidak akan bergerak liar ke atas/bawah/samping.
            foreach (var hand in trackedHands)
            {
                if (hand.visualObject != null && hand.visualObject != hand.colliderObject && hand.originalParent != null)
                {
                    if (hand.visualObject.transform.parent != hand.originalParent)
                    {
                        Vector3 worldLockedPos = referenceTransform.TransformPoint(hand.lockLocalPos);
                        
                        // Posisi terkunci + pergerakan searah sumbu tarikan sesuai jarak fisik
                        hand.visualObject.transform.position = worldLockedPos + (pullAxis * pullAmount);
                        hand.visualObject.transform.rotation = referenceTransform.rotation * hand.lockLocalRot;
                    }
                }
            }

            // Abdominal Thrust dihitung SAAT DITARIK, bukan saat dilepas (beda dengan CPR yang menekan)
            // Jika ditarik sejauh 80% dari target threshold
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

                        if (currentThrusts >= requiredThrusts)
                        {
                            CompleteThrust();
                        }
                    }
                }
            }
            else if (pullAmount <= pullDistanceThreshold * 0.2f)
            {
                // Reset siklus jika tangan dikembalikan posisinya (maju lagi)
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

            foreach(var hand in trackedHands)
            {
                RestoreHandVisual(hand);
            }

            ObjectiveEvents.RaiseTargetCompleted(objectiveTargetId, completionType);
        }
    }
}
