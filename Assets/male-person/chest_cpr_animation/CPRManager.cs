using UnityEngine;
using UnityEngine.Events;
using MediProfen.Objectives;
using TMPro; // Tambahkan namespace untuk UI
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

namespace MediProfen.Interactions
{
    [RequireComponent(typeof(Collider))]
    public class CPRManager : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Animator pada model dada pria")]
        public Animator chestAnimator;
        [Tooltip("Nama Trigger parameter di Animator")]
        public string compressTriggerName = "Compress";

        [Header("CPR Requirements")]
        [Tooltip("Jumlah tekanan/kompresi yang dibutuhkan untuk menyelesaikan objective")]
        public int requiredCompressions = 30;
        
        [Header("Objective Events (Auto trigger finish)")]
        [Tooltip("ID target objective untuk CPR (harus sama dengan yang ada di ObjectiveData)")]
        public string cprObjectiveTargetId = "FinishCPR";
        public ObjectiveCompletionType completionType = ObjectiveCompletionType.Trigger;

        [Header("Visual Feedback (Optional)")]
        [Tooltip("Indikator hologram yang akan dimatikan otomatis ketika CPR selesai")]
        public GameObject cprIndicator;
        [Tooltip("Text UI untuk menampilkan counter CPR secara realtime (Mesh/Canvas)")]
        public TextMeshProUGUI counterText;

        [Header("Realistic CPR Settings")]
        [Tooltip("Offset lokal maksimum untuk gerakan tangan ke bawah (meter)")]
        public float maxHandsOffset = 0.10f;
        [Tooltip("Kedalaman maksimum kompresi fisik kontroller (meter)")]
        public float maxDepth = 0.20f;
        [Tooltip("Nilai ambang batas grip untuk mengaktifkan grab (0.0 sampai 1.0)")]
        public float gripThreshold = 0.5f;

        private class TrackedHand
        {
            public GameObject colliderObject;
            public AnimateHandOnInput animateHand;
            public GameObject visualObject;
            
            // Tracking state
            public bool isInsideTrigger;
            public bool isGrabbing;
            public float grabStartY;
            
            // Reparenting state
            public Transform originalParent;
            public Vector3 originalLocalPos;
            public Quaternion originalLocalRot;

            public Vector3 lockLocalPos;
            public Quaternion lockLocalRot;
        }

        // List untuk melacak tangan di area atau yang sedang menahan grab
        private List<TrackedHand> trackedHands = new List<TrackedHand>();

        // State CPR
        private int currentCompressions = 0;
        private bool isCprComplete = false;
        private bool isGrabbingChest = false;
        private float grabStartAverageY = 0f;
        private bool hasCompressedThisCycle = false;

        private void Start()
        {
            // Update teks awal, tetapi biarkan status terlihat/tidaknya diatur dari Inspector 
            // atau dijadikan child dari cprIndicator
            if (counterText != null)
            {
                UpdateCounterUI();
                counterText.gameObject.SetActive(false);
            }
        }

        // --- METHOD BARU --- 
        // Panggil method ini dari XR Socket Interactor (Masker) > Select Entered
        public void ActivateCPRPhase()
        {
            Debug.Log("[CPRManager] ActivateCPRPhase terpanggil! Menyalakan indikator visual dan teks...");

            if (cprIndicator != null) cprIndicator.SetActive(true);
            if (counterText != null) counterText.gameObject.SetActive(true);
            
            UpdateCounterUI();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCprComplete) return;

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

                // Tentukan GameObject visual (Hand Model) untuk dilock posisinya
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
                Debug.Log($"[CPRManager] Hand entered: {other.gameObject.name}. AnimateHand found: {animateHand != null}. Tracked: {trackedHands.Count}");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Hand"))
            {
                var hand = trackedHands.Find(h => h.colliderObject == other.gameObject);
                if (hand != null)
                {
                    hand.isInsideTrigger = false;
                    Debug.Log($"[CPRManager] Hand exited trigger: {other.gameObject.name}. Tracking will remain if still grabbing.");
                }
            }
        }

        private void Update()
        {
            if (isCprComplete) return;

            // 1. Bersihkan tangan yang sudah keluar dari trigger DAN tidak sedang menahan grip
            for (int i = trackedHands.Count - 1; i >= 0; i--)
            {
                var hand = trackedHands[i];
                float grip = GetGripValue(hand);
                bool isGripActive = grip >= gripThreshold;

                if (!hand.isInsideTrigger && !isGripActive)
                {
                    // Tangan sudah keluar secara fisik dan grip dilepas
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

            if (isGrabbingChest)
            {
                // Jika sedang dalam sesi kompresi, izinkan tangan keluar trigger asalkan grip masih ditahan
                if (grabbingHandsCount >= 2)
                {
                    shouldBeInGrabState = true;
                }
            }
            else
            {
                // Untuk mulai grab baru, kedua tangan harus ada di dalam trigger
                if (readyToGrabCount >= 2)
                {
                    shouldBeInGrabState = true;
                }
            }

            if (shouldBeInGrabState)
            {
                if (!isGrabbingChest)
                {
                    EnterGrabState();
                }
                UpdateGrabAndPump();
            }
            else
            {
                if (isGrabbingChest)
                {
                    ExitGrabState();
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

        private void EnterGrabState()
        {
            isGrabbingChest = true;
            Debug.Log("[CPRManager] Enter grab state - Hands locked to chest!");

            // Catat posisi Y awal rata-rata
            grabStartAverageY = GetAverageY();

            foreach (var hand in trackedHands)
            {
                hand.grabStartY = hand.colliderObject.transform.position.y;

                if (hand.visualObject != null && hand.visualObject != hand.colliderObject)
                {
                    // Simpan state asli
                    hand.originalParent = hand.visualObject.transform.parent;
                    hand.originalLocalPos = hand.visualObject.transform.localPosition;
                    hand.originalLocalRot = hand.visualObject.transform.localRotation;

                    // Hitung posisi dan rotasi relatif terhadap dada saat tangan mulai grab
                    hand.lockLocalPos = this.transform.InverseTransformPoint(hand.visualObject.transform.position);
                    hand.lockLocalRot = Quaternion.Inverse(this.transform.rotation) * hand.visualObject.transform.rotation;

                    // Cegah stretching: jangan parent ke dada jika dada punya non-uniform scale.
                    // Parent ke Camera Offset (parent dari controller) atau null agar scale tetap normal 1,1,1.
                    Transform safeParent = hand.originalParent != null ? hand.originalParent.parent : null;
                    hand.visualObject.transform.SetParent(safeParent, true);
                }
            }

            // Matikan kecepatan animator agar kita bisa scrub secara manual
            if (chestAnimator != null)
            {
                chestAnimator.speed = 0;
            }
        }

        private void ExitGrabState()
        {
            isGrabbingChest = false;
            Debug.Log("[CPRManager] Exit grab state - Hands released!");

            foreach (var hand in trackedHands)
            {
                RestoreHandVisual(hand);
            }

            // Kembalikan animator ke keadaan normal
            if (chestAnimator != null)
            {
                chestAnimator.speed = 1f;
                chestAnimator.Play("Idle", 0, 0f);
            }

            hasCompressedThisCycle = false;
        }

        private void UpdateGrabAndPump()
        {
            float currentAverageY = GetAverageY();
            // displacement bernilai positif jika tangan ditekan ke bawah (Y mengecil)
            float displacement = grabStartAverageY - currentAverageY;

            float compressionFraction = Mathf.Clamp01(displacement / maxDepth);

            // Gerakkan tangan visual ke bawah sesuai penekanan
            foreach (var hand in trackedHands)
            {
                if (hand.visualObject != null && hand.visualObject != hand.colliderObject && hand.originalParent != null)
                {
                    if (hand.visualObject.transform.parent != hand.originalParent)
                    {
                        // Posisi terkunci (dalam world space)
                        Vector3 worldLockedPos = this.transform.TransformPoint(hand.lockLocalPos);
                        
                        // Turunkan searah sumbu Y negatif global murni (Vector3.down)
                        // Ini mencegah pergerakan menyamping jika rotasi dada miring
                        hand.visualObject.transform.position = worldLockedPos + Vector3.down * (compressionFraction * maxHandsOffset);
                        hand.visualObject.transform.rotation = this.transform.rotation * hand.lockLocalRot;
                    }
                }
            }

            // Scrub animasi dada korban berdasarkan penekanan
            if (chestAnimator != null)
            {
                chestAnimator.Play("rig|rig|rigAction", 0, compressionFraction);
            }

            // Deteksi siklus penekanan (80% ke bawah untuk hitung ditekan, 20% ke atas untuk rilis sukses)
            if (compressionFraction >= 0.8f)
            {
                if (!hasCompressedThisCycle)
                {
                    hasCompressedThisCycle = true;
                    TriggerHapticFeedback(0.3f, 0.05f);
                }
            }
            else if (compressionFraction <= 0.2f)
            {
                if (hasCompressedThisCycle)
                {
                    currentCompressions++;
                    UpdateCounterUI();
                    TriggerHapticFeedback(0.7f, 0.15f);

                    hasCompressedThisCycle = false;

                    if (currentCompressions >= requiredCompressions)
                    {
                        CompleteCPR();
                    }
                }
            }
        }

        private void RestoreHandVisual(TrackedHand hand)
        {
            // Pastikan kita mengembalikan object yang benar
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

        private float GetAverageY()
        {
            if (trackedHands.Count < 2) return 0f;
            return (trackedHands[0].colliderObject.transform.position.y + trackedHands[1].colliderObject.transform.position.y) / 2.0f;
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
            {
                counterText.text = $"{currentCompressions} / {requiredCompressions}";
            }
        }

        private void CompleteCPR()
        {
            isCprComplete = true;
            Debug.Log("[CPRManager] CPR Completed!");

            foreach (var hand in trackedHands)
            {
                RestoreHandVisual(hand);
            }

            if (cprIndicator != null)
            {
                cprIndicator.SetActive(false);
            }
            
            if (counterText != null)
            {
                counterText.gameObject.SetActive(false);
            }

            ObjectiveEvents.RaiseTargetCompleted(cprObjectiveTargetId, completionType);
        }
    }
}