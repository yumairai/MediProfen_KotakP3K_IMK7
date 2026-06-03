using UnityEngine;
using UnityEngine.InputSystem;
using MediProfen.Core;

namespace MediProfen.UI
{
    public class PauseMenuManager : MonoBehaviour
    {
        [Header("Pengaturan Input")]
        [Tooltip("Pilih tombol dari Input Action (Misal: XRI LeftHand/Menu atau Primary Button)")]
        [SerializeField] private InputActionReference pauseButton;

        [Header("UI Canvas & Panels")]
        [Tooltip("Canvas utama menu pause keseluruhan")]
        [SerializeField] private GameObject pauseMenuCanvas;
        [Tooltip("Panel yang berisi tombol Lanjut, Kontrol, Keluar")]
        [SerializeField] private GameObject mainPausePanel;
        [Tooltip("Panel yang berisi gambar/panduan kontrol")]
        [SerializeField] private GameObject controlPanel;

        [Header("Posisi Muncul (Opsional)")]
        [Tooltip("Masukkan Main Camera (VR) agar menu selalu muncul di depan wajah")]
        [SerializeField] private Transform headTransform;
        [Tooltip("Jarak menu dari wajah pemain saat muncul")]
        [SerializeField] private float spawnDistance = 1.5f;

        private bool isPaused = false;

        private void OnEnable()
        {
            // Mengaktifkan pendeteksi tombol
            if (pauseButton != null && pauseButton.action != null)
            {
                pauseButton.action.Enable();
                pauseButton.action.performed += OnPauseButtonPressed;
            }
        }

        private void OnDisable()
        {
            // Mematikan pendeteksi tombol saat script mati
            if (pauseButton != null && pauseButton.action != null)
            {
                pauseButton.action.performed -= OnPauseButtonPressed;
            }
        }

        private void Start()
        {
            // Sembunyikan menu saat game baru mulai
            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(false);
            }
        }

        private void OnPauseButtonPressed(InputAction.CallbackContext context)
        {
            TogglePause();
        }

        public void TogglePause()
        {
            isPaused = !isPaused;

            if (isPaused)
            {
                OpenPauseMenu();
            }
            else
            {
                ResumeGame();
            }
        }

        private void OpenPauseMenu()
        {
            isPaused = true;
            Time.timeScale = 0f; // Menghentikan waktu/fisika di dalam game

            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(true);
                
                // Menempatkan menu tepat di depan posisi wajah pemain saat ini
                if (headTransform != null)
                {
                    Vector3 spawnPos = headTransform.position + (headTransform.forward * spawnDistance);
                    spawnPos.y = headTransform.position.y; // Buat tingginya sejajar mata
                    
                    pauseMenuCanvas.transform.position = spawnPos;
                    
                    // Memutar menu agar menghadap ke pemain
                    pauseMenuCanvas.transform.LookAt(headTransform);
                    pauseMenuCanvas.transform.Rotate(0, 180, 0); // Dibalik agar teksnya tidak mirror
                }

                ShowMainPanel();
            }
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f; // Kembalikan waktu normal agar game berjalan lagi

            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(false);
            }
        }

        public void ShowMainPanel()
        {
            if (mainPausePanel != null) mainPausePanel.SetActive(true);
            if (controlPanel != null) controlPanel.SetActive(false);
        }

        public void ShowControlPanel()
        {
            if (mainPausePanel != null) mainPausePanel.SetActive(false);
            if (controlPanel != null) controlPanel.SetActive(true);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f; // Sangat penting! Waktu wajib dikembalikan normal sebelum pindah scene
            
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.ReturnToMenu();
            }
            else
            {
                Debug.LogWarning("[PauseMenuManager] GameFlowManager tidak ditemukan!");
            }
        }
    }
}
