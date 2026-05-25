using UnityEngine;
using UnityEngine.UI;
using MediProfen.Objectives;
using MediProfen.Core;
using MediProfen.Data;

namespace MediProfen.UI
{
    public class ScenarioCompletionMenu : MonoBehaviour
    {
        [Header("System References")]
        [Tooltip("Masukkan objek yang memiliki komponen ObjectiveRunner di scene ini")]
        public ObjectiveRunner runner;

        [Header("UI References")]
        [Tooltip("Panel Canvas utama yang berisi tulisan Selesai dan Tombol (akan disembunyikan di awal)")]
        public GameObject completionPanel;
        
        [Tooltip("Tombol untuk kembali ke Main Menu")]
        public Button backToMenuButton;

        private void OnEnable()
        {
            if (runner != null)
            {
                // Dengarkan sinyal ketika skenario benar-benar selesai
                runner.ScenarioCompleted += HandleScenarioCompleted;
            }

            if (completionPanel != null)
            {
                // Sembunyikan menu kemenangan saat game baru mulai
                completionPanel.SetActive(false); 
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            }
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.ScenarioCompleted -= HandleScenarioCompleted;
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveListener(OnBackToMenuClicked);
            }
        }

        private void HandleScenarioCompleted(ScenarioData scenario)
        {
            // Munculkan menu kemenangan di dunia VR
            if (completionPanel != null)
            {
                completionPanel.SetActive(true);
            }
            
            Debug.Log("[ScenarioCompletionMenu] Skenario selesai, menu kemenangan ditampilkan!");
        }

        private void OnBackToMenuClicked()
        {
            // Gunakan GameFlowManager untuk pindah kembali ke Main Menu
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.ReturnToMenu();
            }
            else
            {
                Debug.LogError("[ScenarioCompletionMenu] GameFlowManager tidak ditemukan di scene!");
            }
        }
    }
}
