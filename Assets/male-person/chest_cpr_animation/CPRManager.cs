using UnityEngine;
using UnityEngine.Events;
using MediProfen.Objectives;

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

        // Counter untuk melacak jumlah kompresi saat ini
        private int currentCompressions = 0;
        
        // Mencegah kompresi yang dihitung jika proses sudah selesai
        private bool isCprComplete = false;

        private void OnTriggerEnter(Collider other)
        {
            if (isCprComplete) return;

            // Jika tangan menyentuh (box collider isTrigger) area dada
            if (other.CompareTag("Hand"))
            {
                PerformCompression();
            }
        }

        private void PerformCompression()
        {
            // Tambah counter
            currentCompressions++;
            Debug.Log($"[CPRManager] Compression count: {currentCompressions} / {requiredCompressions}");

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

        private void CompleteCPR()
        {
            isCprComplete = true;
            Debug.Log("[CPRManager] CPR Completed!");

            // Sembunyikan indikator
            if (cprIndicator != null)
            {
                cprIndicator.SetActive(false);
            }

            // Infokan ke ObjectiveRunner bahwa misi menekan dada sudah beres
            ObjectiveEvents.RaiseTargetCompleted(cprObjectiveTargetId, completionType);
        }
    }
}