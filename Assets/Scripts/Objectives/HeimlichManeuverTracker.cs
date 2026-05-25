using UnityEngine;
using UnityEngine.Events;
using MediProfen.Objectives;

namespace MediProfen.Objectives
{
    /// <summary>
    /// Melacak jumlah interaksi Heimlich Maneuver.
    /// Script ini dapat dipasangkan pada root objek korban atau di sebuah manager.
    /// </summary>
    public class HeimlichManeuverTracker : MonoBehaviour
    {
        [Header("Objective Settings")]
        [Tooltip("ID target objective (sama dengan TargetId di ScriptableObject ObjectiveData)")]
        [SerializeField] private string targetId = "Heimlich_Finished";
        [SerializeField] private ObjectiveCompletionType completionType = ObjectiveCompletionType.Custom;

        [Header("Heimlich Settings")]
        [Tooltip("Berapa kali Heimlich Maneuver harus dilakukan sampai objective selesai.")]
        public int requiredThrusts = 5;
        
        [Header("Events (Optional)")]
        public UnityEvent OnThrustPerformed;
        public UnityEvent OnHeimlichCompleted;

        private int currentThrusts = 0;
        private bool isCompleted = false;

        // Status apakah kedua area sedang di-grab
        private bool isBackGrabbed = false;
        private bool isStomachGrabbed = false;

        /// <summary>
        /// Panggil dari event Select Entered di Plane Punggung (centang checkbox 'true' di inspector)
        /// dan Select Exited (uncentang / 'false' di inspector)
        /// </summary>
        public void SetBackGrabbed(bool state)
        {
            isBackGrabbed = state;
        }

        /// <summary>
        /// Panggil dari event Select Entered di Plane Perut (centang checkbox 'true' di inspector)
        /// dan Select Exited (uncentang / 'false' di inspector)
        /// </summary>
        public void SetStomachGrabbed(bool state)
        {
            isStomachGrabbed = state;
        }

        /// <summary>
        /// Panggil fungsi ini ketika "ditekan ke dalam" 
        /// (misal dari event Trigger Enter di collider yang lebih dalam, atau saat threshold posisi tercapai).
        /// Thrust HANYA akan terhitung jika kedua plane sedang di-grab.
        /// </summary>
        public void TryPerformThrust()
        {
            if (isCompleted) return;

            // Pastikan punggung dan perut sedang dipegang secara bersamaan
            if (isBackGrabbed && isStomachGrabbed)
            {
                currentThrusts++;
                Debug.Log($"[HeimlichManeuverTracker] Thrust berhasil dilakukan! ({currentThrusts}/{requiredThrusts})");
                
                OnThrustPerformed?.Invoke();

                if (currentThrusts >= requiredThrusts)
                {
                    CompleteHeimlich();
                }
            }
            else
            {
                Debug.Log("[HeimlichManeuverTracker] Thrust gagal: Pastikan punggung dan perut di-grab bersamaan.");
            }
        }

        private void CompleteHeimlich()
        {
            isCompleted = true;
            Debug.Log($"[HeimlichManeuverTracker] Heimlich Maneuver selesai! Mengirim event objective untuk '{targetId}'");
            
            // Mengirim sinyal ke sistem ObjectiveRunner bahwa target ini selesai
            ObjectiveEvents.RaiseTargetCompleted(targetId, completionType);
            
            OnHeimlichCompleted?.Invoke();
        }
        
        // Fungsi pembantu jika ingin me-reset simulasi tanpa reload scene
        public void ResetTracker()
        {
            currentThrusts = 0;
            isCompleted = false;
            isBackGrabbed = false;
            isStomachGrabbed = false;
        }
    }
}
