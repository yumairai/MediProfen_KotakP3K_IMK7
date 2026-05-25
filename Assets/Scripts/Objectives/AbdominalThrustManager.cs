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

        // State Tarikan
        private bool isGrabbing = false;
        private float grabStartZ = 0f;
        private bool hasThrustThisCycle = false;

        private class TrackedHand
        {
            public GameObject handObj;
            public AnimateHandOnInput animateHand;
            public GameObject visualObj;

            public bool isInsideStomach;
            public bool isGrabbing; // Melacak apakah tangan ini sedang mengunci perut

            // Variabel untuk mengunci visual model tangan
            public Transform originalParent;
            public Vector3 originalLocalPos;
            public Quaternion originalLocalRot;
            public Vector3 lockLocalPos;
            public Quaternion lockLocalRot;
        }

        private List<TrackedHand> trackedHands = new List<TrackedHand>();

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
                var hand = GetOrAddHand(other.gameObject);
                if (stomachCollider != null && stomachCollider.bounds.Intersects(other.bounds))
                {
                    hand.isInsideStomach = true;
                }
            }
        }

        public void RelayTriggerExit(Collider other)
        {
            if (other.CompareTag("Hand"))
            {
                var hand = trackedHands.Find(h => h.handObj == other.gameObject);
                if (hand != null && stomachCollider != null)
                {
                    if (!stomachCollider.bounds.Intersects(other.bounds))
                    {
                        hand.isInsideStomach = false;
                    }
                }
            }
        }

        private void Update()
        {
            if (isCompleted) return;

            for (int i = trackedHands.Count - 1; i >= 0; i--)
            {
                var hand = trackedHands[i];
                if (hand.handObj != null && stomachCollider != null)
                {
                    bool physicallyInside = stomachCollider.bounds.Intersects(hand.handObj.GetComponent<Collider>().bounds);
                    if (!hand.isGrabbing) hand.isInsideStomach = physicallyInside; // Update jika belum digenggam
                }

                float grip = GetGripValue(hand);
                bool isGripActive = grip >= gripThreshold;

                if (!hand.isInsideStomach && !isGripActive)
                {
                    if (hand.isGrabbing) RestoreHandVisual(hand);
                    trackedHands.RemoveAt(i);
                    continue;
                }

                if (hand.isInsideStomach || hand.isGrabbing)
                {
                    if (isGripActive && !hand.isGrabbing && hand.isInsideStomach)
                    {
                        hand.isGrabbing = true;
                        LockHandVisual(hand);
                    }
                    else if (!isGripActive && hand.isGrabbing)
                    {
                        hand.isGrabbing = false;
                        RestoreHandVisual(hand);
                    }

                    if (hand.isGrabbing)
                    {
                        UpdateLockedHandPosition(hand);
                    }
                }
            }

            int grippingHandsCount = 0;
            float sumZ = 0f;

            foreach (var hand in trackedHands)
            {
                if (hand.isGrabbing)
                {
                    grippingHandsCount++;
                    sumZ += GetLocalZ(hand.handObj.transform.position);
                }
            }

            // Membutuhkan 2 tangan untuk melakukan tarikan
            bool shouldBeGrabbing = grippingHandsCount >= 2;

            if (shouldBeGrabbing)
            {
                float currentAvgZ = sumZ / grippingHandsCount;

                if (!isGrabbing)
                {
                    // Mulai menggenggam dengan dua tangan
                    isGrabbing = true;
                    grabStartZ = currentAvgZ;
                    hasThrustThisCycle = false;
                    Debug.Log($"[AbdominalThrustManager] Perut digenggam dengan 2 tangan. Start Z: {grabStartZ}");
                }
                else
                {
                    // Sedang menggenggam, hitung tarikan
                    // Kita asumsikan Transform Z korban mengarah ke depan (keluar dari dada). 
                    // Jadi kalau ditarik ke arah player (punggung), Z lokal akan berkurang (menjadi lebih negatif).
                    // Displacement = Start Z - Current Z
                    float pullDistance = grabStartZ - currentAvgZ;

                    if (!hasThrustThisCycle && pullDistance >= pullDistanceThreshold)
                    {
                        if (Time.time - lastThrustTime >= cooldownTime)
                        {
                            hasThrustThisCycle = true;
                            currentThrusts++;
                            lastThrustTime = Time.time;
                            UpdateCounterUI();

                            // Berikan haptic ke semua tangan yang menggenggam
                            foreach(var hand in trackedHands)
                            {
                                if (hand.isInsideStomach) TriggerHaptic(hand, 0.9f, 0.2f);
                            }

                            Debug.Log($"[AbdominalThrustManager] Thrust Sukses! {currentThrusts}/{requiredThrusts}");

                            if (currentThrusts >= requiredThrusts)
                            {
                                CompleteThrust();
                            }
                        }
                    }
                    else if (hasThrustThisCycle && pullDistance < pullDistanceThreshold * 0.3f)
                    {
                        // Reset siklus jika tangan dikembalikan posisinya (kurang dari 30% threshold)
                        hasThrustThisCycle = false;
                        grabStartZ = currentAvgZ; // Reset titik awal tarikan
                    }
                }
            }
            else
            {
                if (isGrabbing)
                {
                    // Lepas genggaman
                    isGrabbing = false;
                    hasThrustThisCycle = false;
                    Debug.Log("[AbdominalThrustManager] Genggaman dilepas.");
                }
            }
        }

        private float GetLocalZ(Vector3 worldPos)
        {
            // Menghitung posisi Z di ruang lokal objek ini (korban)
            return transform.InverseTransformPoint(worldPos).z;
        }

        private TrackedHand GetOrAddHand(GameObject obj)
        {
            var hand = trackedHands.Find(h => h.handObj == obj);
            if (hand == null)
            {
                var animateHand = obj.GetComponent<AnimateHandOnInput>();
                if (animateHand == null) animateHand = obj.GetComponentInParent<AnimateHandOnInput>();
                if (animateHand == null && obj.transform.parent != null)
                    animateHand = obj.transform.parent.GetComponentInChildren<AnimateHandOnInput>();

                GameObject visualObject = (animateHand != null) ? animateHand.gameObject : obj;

                hand = new TrackedHand { 
                    handObj = obj, 
                    animateHand = animateHand,
                    visualObj = visualObject
                };
                trackedHands.Add(hand);
            }
            return hand;
        }

        private float GetGripValue(TrackedHand hand)
        {
            if (hand.animateHand != null && hand.animateHand.gripValue.action != null)
            {
                return hand.animateHand.gripValue.action.ReadValue<float>();
            }
            return 0f;
        }

        private void TriggerHaptic(TrackedHand hand, float amplitude, float duration)
        {
            if (hand.handObj != null)
            {
                var controller = hand.handObj.GetComponentInParent<ActionBasedController>();
                if (controller != null)
                {
                    controller.SendHapticImpulse(amplitude, duration);
                }
            }
        }

        private void UpdateCounterUI()
        {
            if (counterText != null)
                counterText.text = $"{currentThrusts} / {requiredThrusts}";
        }

        private void LockHandVisual(TrackedHand hand)
        {
            if (hand.visualObj != null && hand.visualObj != hand.handObj)
            {
                hand.originalParent = hand.visualObj.transform.parent;
                hand.originalLocalPos = hand.visualObj.transform.localPosition;
                hand.originalLocalRot = hand.visualObj.transform.localRotation;

                Transform referenceTransform = stomachCollider != null ? stomachCollider.transform : this.transform;
                
                hand.lockLocalPos = referenceTransform.InverseTransformPoint(hand.visualObj.transform.position);
                hand.lockLocalRot = Quaternion.Inverse(referenceTransform.rotation) * hand.visualObj.transform.rotation;

                Transform safeParent = hand.originalParent != null ? hand.originalParent.parent : null;
                hand.visualObj.transform.SetParent(safeParent, true);
            }
        }

        private void UpdateLockedHandPosition(TrackedHand hand)
        {
            if (hand.visualObj != null && hand.visualObj != hand.handObj && hand.originalParent != null)
            {
                Transform referenceTransform = stomachCollider != null ? stomachCollider.transform : this.transform;
                
                Vector3 worldLockedPos = referenceTransform.TransformPoint(hand.lockLocalPos);
                hand.visualObj.transform.position = worldLockedPos;
                hand.visualObj.transform.rotation = referenceTransform.rotation * hand.lockLocalRot;
            }
        }

        private void RestoreHandVisual(TrackedHand hand)
        {
            if (hand.visualObj != null && hand.visualObj != hand.handObj && hand.originalParent != null)
            {
                if (hand.visualObj.transform.parent != hand.originalParent)
                {
                    hand.visualObj.transform.SetParent(hand.originalParent, true);
                    hand.visualObj.transform.localPosition = hand.originalLocalPos;
                    hand.visualObj.transform.localRotation = hand.originalLocalRot;
                }
            }
        }

        private void CompleteThrust()
        {
            isCompleted = true;
            Debug.Log("[AbdominalThrustManager] Objektif Abdominal Thrust Selesai!");

            foreach(var hand in trackedHands)
            {
                if (hand.isGrabbing) RestoreHandVisual(hand);
            }

            ObjectiveEvents.RaiseTargetCompleted(objectiveTargetId, completionType);
        }
    }
}
