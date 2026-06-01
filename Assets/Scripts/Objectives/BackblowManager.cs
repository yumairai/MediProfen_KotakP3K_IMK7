using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using MediProfen.Objectives;
using TMPro;

namespace MediProfen.Interactions
{
    public class SimpleTriggerRelay : MonoBehaviour
    {
        public System.Action<Collider> onEnter;
        public System.Action<Collider> onExit;

        private void OnTriggerEnter(Collider other)
        {
            onEnter?.Invoke(other);
        }
        private void OnTriggerExit(Collider other)
        {
            onExit?.Invoke(other);
        }
    }

    public class BackblowManager : MonoBehaviour
    {
        [Header("Objective Target")]
        public string objectiveTargetId = "HeimlichManuver";
        public ObjectiveCompletionType completionType = ObjectiveCompletionType.Trigger;

        [Header("Colliders")]
        [Tooltip("Collider trigger untuk area dada yang harus di-grab")]
        public Collider chestCollider;
        [Tooltip("Collider trigger untuk area punggung yang akan dipukul")]
        public Collider backCollider;

        [Header("Settings")]
        public int requiredBlows = 5;
        [Tooltip("Waktu jeda antar pukulan (detik)")]
        public float cooldownTime = 0.5f;
        public float gripThreshold = 0.5f;

        [Header("UI Feedback (Optional)")]
        public TextMeshProUGUI counterText;

        [Header("Animation")]
        [Tooltip("Animator pada model korban")]
        public Animator victimAnimator;
        [Tooltip("Nama state animasi di Animator saat dada digrab (contoh: benddown_pose)")]
        public string bendDownStateName = "benddown_pose";
        [Tooltip("Nama state animasi di Animator saat dada dilepas (contoh: Idle)")]
        public string releaseStateName = "Idle";
        [Tooltip("Nama state animasi di Animator saat punggung dipukul (contoh: hit_pose)")]
        public string hitStateName = "hit_pose";

        private int currentBlows = 0;
        private bool isCompleted = false;
        private float lastStrikeTime = 0f;

        private class TrackedHand
        {
            public GameObject handObj;
            public AnimateHandOnInput animateHand;
            public GameObject visualObj;

            public bool isInsideChest;
            public bool isInsideBack;
            public bool isGrabbing;

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

            // Memasang pendeteksi trigger otomatis ke anak objek (collider)
            if (chestCollider != null)
            {
                var relay = chestCollider.gameObject.AddComponent<SimpleTriggerRelay>();
                relay.onEnter = this.RelayTriggerEnter;
                relay.onExit = this.RelayTriggerExit;
            }
            if (backCollider != null)
            {
                var relay = backCollider.gameObject.AddComponent<SimpleTriggerRelay>();
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
                
                // Cek masuk ke dada
                if (chestCollider != null && chestCollider.bounds.Intersects(other.bounds))
                {
                    hand.isInsideChest = true;
                }

                // Cek masuk ke punggung
                if (backCollider != null && backCollider.bounds.Intersects(other.bounds))
                {
                    hand.isInsideBack = true;
                    ProcessBackStrike(hand);
                }
            }
        }

        public void RelayTriggerExit(Collider other)
        {
            if (other.CompareTag("Hand"))
            {
                var hand = trackedHands.Find(h => h.handObj == other.gameObject);
                if (hand != null)
                {
                    if (chestCollider != null && !chestCollider.bounds.Intersects(other.bounds))
                        hand.isInsideChest = false;

                    if (backCollider != null && !backCollider.bounds.Intersects(other.bounds))
                        hand.isInsideBack = false;
                }
            }
        }

        private void Update()
        {
            if (isCompleted) return;

            for (int i = trackedHands.Count - 1; i >= 0; i--)
            {
                var hand = trackedHands[i];
                float grip = GetGripValue(hand);
                bool isGripActive = grip >= gripThreshold;

                // Cleanup tangan jika sudah keluar dari semua area dan tidak sedang menggenggam
                if (!hand.isInsideChest && !hand.isInsideBack && !isGripActive)
                {
                    if (hand.isGrabbing)
                    {
                        RestoreHandVisual(hand);
                    }
                    trackedHands.RemoveAt(i);
                    continue;
                }

                // Logika mengunci tangan di dada
                if (hand.isInsideChest || hand.isGrabbing)
                {
                    if (isGripActive && !hand.isGrabbing && hand.isInsideChest)
                    {
                        // Mulai menggenggam
                        hand.isGrabbing = true;
                        LockHandVisual(hand);

                        // Trigger animasi membungkuk
                        if (victimAnimator != null && !string.IsNullOrEmpty(bendDownStateName))
                        {
                            victimAnimator.CrossFade(bendDownStateName, 0.2f);
                        }
                    }
                    else if (!isGripActive && hand.isGrabbing)
                    {
                        // Lepas genggaman
                        hand.isGrabbing = false;
                        RestoreHandVisual(hand);

                        // Jika tidak ada tangan lain yang sedang menggenggam dada, kembalikan animasi
                        if (victimAnimator != null && !string.IsNullOrEmpty(releaseStateName))
                        {
                            if (!IsChestGrabbed())
                            {
                                victimAnimator.CrossFade(releaseStateName, 0.2f);
                            }
                        }
                    }

                    // Paksa posisi model tangan (visual) agar diam di tempat (menempel dada)
                    if (hand.isGrabbing)
                    {
                        UpdateLockedHandPosition(hand);
                    }
                }
            }
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

        private bool IsChestGrabbed()
        {
            foreach (var hand in trackedHands)
            {
                if (hand.isGrabbing)
                {
                    return true; 
                }
            }
            return false;
        }

        private void ProcessBackStrike(TrackedHand strikingHand)
        {
            if (Time.time - lastStrikeTime < cooldownTime) return; // Cooldown

            // Pastikan ada tangan yang mengunci di dada (isGrabbing == true)
            if (IsChestGrabbed())
            {
                // Hitung pukulan
                currentBlows++;
                lastStrikeTime = Time.time;
                UpdateCounterUI();
                TriggerHaptic(strikingHand, 0.8f, 0.15f);

                // Mainkan animasi hentakan (hit)
                if (victimAnimator != null && !string.IsNullOrEmpty(hitStateName))
                {
                    // Memaksa animasi dimainkan dari awal (frame 0) setiap kali dipukul
                    victimAnimator.Play(hitStateName, 0, 0f);
                }

                Debug.Log($"[BackblowManager] Backblow sukses! {currentBlows}/{requiredBlows}");

                if (currentBlows >= requiredBlows)
                {
                    CompleteBackblow();
                }
            }
            else
            {
                Debug.Log("[BackblowManager] Backblow ditolak: Dada belum digenggam.");
            }
        }

        private void LockHandVisual(TrackedHand hand)
        {
            if (hand.visualObj != null && hand.visualObj != hand.handObj)
            {
                hand.originalParent = hand.visualObj.transform.parent;
                hand.originalLocalPos = hand.visualObj.transform.localPosition;
                hand.originalLocalRot = hand.visualObj.transform.localRotation;

                Transform referenceTransform = chestCollider != null ? chestCollider.transform : this.transform;
                
                // Hitung offset relatif terhadap dada
                hand.lockLocalPos = referenceTransform.InverseTransformPoint(hand.visualObj.transform.position);
                hand.lockLocalRot = Quaternion.Inverse(referenceTransform.rotation) * hand.visualObj.transform.rotation;

                // Pindahkan parent ke tempat aman (agar tidak kena scale/stretching dari collider parent)
                Transform safeParent = hand.originalParent != null ? hand.originalParent.parent : null;
                hand.visualObj.transform.SetParent(safeParent, true);
            }
            Debug.Log("[BackblowManager] Tangan terkunci di dada!");
        }

        private void UpdateLockedHandPosition(TrackedHand hand)
        {
            if (hand.visualObj != null && hand.visualObj != hand.handObj && hand.originalParent != null)
            {
                Transform referenceTransform = chestCollider != null ? chestCollider.transform : this.transform;
                
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
            Debug.Log("[BackblowManager] Tangan dilepas dari dada!");
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
                counterText.text = $"{currentBlows} / {requiredBlows}";
        }

        private void CompleteBackblow()
        {
            isCompleted = true;
            Debug.Log("[BackblowManager] Objektif Backblow Selesai!");
            
            // Kembalikan semua tangan visual yang mungkin masih nyangkut
            foreach(var hand in trackedHands)
            {
                if (hand.isGrabbing) RestoreHandVisual(hand);
            }

            ObjectiveEvents.RaiseTargetCompleted(objectiveTargetId, completionType);
        }
    }
}
