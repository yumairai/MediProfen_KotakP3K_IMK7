using UnityEngine;
using UnityEngine.Events;
using MediProfen.Objectives;
using TMPro; // Tambahkan namespace untuk UI

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

        // Mendeteksi jumlah tangan yang ada di dalam area
        private int handsInZone = 0;

        // Counter untuk melacak jumlah kompresi saat ini
        private int currentCompressions = 0;
        
        // Mencegah kompresi yang dihitung jika proses sudah selesai
        private bool isCprComplete = false;
        
        // Mencegah trigger berulang super cepat sebelum tangan diangkat
        private bool isCompressing = false;

        private void Start()
        {
            // Update teks awal, tetapi biarkan status terlihat/tidaknya diatur dari Inspector 
            // atau dijadikan child dari cprIndicator
            if (counterText != null)
            {
                UpdateCounterUI();
                // Opsional: Matikan text jika belum butuh (bisa dikontrol dari metode ActivateCPR)
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
                handsInZone++;

                // Hanya hitung CPR (tekanan ke bawah) jika KEDUA tangan (2) masuk ke zona
                if (handsInZone >= 2 && !isCompressing)
                {
                    PerformCompression();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Hand"))
            {
                handsInZone--;
                if (handsInZone < 0) handsInZone = 0; // Safety clamp

                // Jika salah satu atau kedua tangan diangkat (rilis kompresi)
                if (handsInZone == 0)
                {
                    // Memungkinkan kompresi berikutnya
                    isCompressing = false;
                }
            }
        }

        private void PerformCompression()
        {
            isCompressing = true;
            
            // Tambah counter
            currentCompressions++;
            Debug.Log($"[CPRManager] Compression count: {currentCompressions} / {requiredCompressions}");
            
            UpdateCounterUI();

            // Jalankan animasi dada kembang/kempis
            if (chestAnimator != null)
            {
                chestAnimator.SetTrigger(compressTriggerName);
            }

            // Jika sudah mencapai target kompresi
            if (currentCompressions >= requiredCompressions)
            {
                CompleteCPR();
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

            // Sembunyikan indikator
            if (cprIndicator != null)
            {
                cprIndicator.SetActive(false);
            }
            
            // Sembunyikan text jika perlu
            if (counterText != null)
            {
                counterText.gameObject.SetActive(false);
            }

            // Infokan ke ObjectiveRunner bahwa misi menekan dada sudah beres
            ObjectiveEvents.RaiseTargetCompleted(cprObjectiveTargetId, completionType);
        }
    }
}