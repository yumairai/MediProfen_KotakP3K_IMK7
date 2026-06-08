using UnityEngine;
using UnityEngine.InputSystem;
using MediProfen.Core;

namespace MediProfen.UI
{
    public class PauseMenuManager : MonoBehaviour
    {
        [Header("Pengaturan Input")]
        [Tooltip("Pilih tombol dari Input Action (Misal: XRI Left/Menu)")]
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
        // Digunakan agar satu kali tekan tidak memicu toggle berkali-kali
        private bool wasButtonHeldLastFrame = false;

        private void OnEnable()
        {
            if (pauseButton != null && pauseButton.action != null)
            {
                // Aktifkan Action Map induk agar sinyal controller bisa mengalir
                pauseButton.action.actionMap?.Enable();
                pauseButton.action.Enable();
                Debug.Log($"[PauseMenuManager] Input terdaftar: '{pauseButton.action.name}' | Aktif: {pauseButton.action.enabled}");
            }
            else
            {
                Debug.LogWarning("[PauseMenuManager] Pause Button belum diisi di Inspector!");
            }
        }

        private void OnDisable()
        {
            // Tidak perlu menonaktifkan action map di sini agar controller lain tetap berjalan
        }

        private void Start()
        {
            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(false);
            }
        }

        private void Update()
        {
            if (pauseButton == null || pauseButton.action == null) return;

            // Cek apakah tombol sedang ditekan frame ini
            bool isButtonHeld = pauseButton.action.IsPressed();

            // Hanya toggle saat tombol baru ditekan (bukan terus-terusan saat ditahan)
            if (isButtonHeld && !wasButtonHeldLastFrame)
            {
                Debug.Log("[PauseMenuManager] Tombol Pause terdeteksi!");
                TogglePause();
            }

            wasButtonHeldLastFrame = isButtonHeld;
        }

        public void TogglePause()
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                OpenPauseMenu();
            }
        }

        private void OpenPauseMenu()
        {
            isPaused = true;
            Time.timeScale = 0f; // Hentikan waktu/fisika game

            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(true);

                // Munculkan menu tepat di depan wajah pemain
                if (headTransform != null)
                {
                    Vector3 forward = headTransform.forward;
                    forward.y = 0f;
                    forward.Normalize();

                    Vector3 spawnPos = headTransform.position + (forward * spawnDistance);
                    spawnPos.y = headTransform.position.y;

                    pauseMenuCanvas.transform.position = spawnPos;
                    pauseMenuCanvas.transform.LookAt(headTransform);
                    pauseMenuCanvas.transform.Rotate(0, 180, 0);
                }

                ShowMainPanel();
            }

            Debug.Log("[PauseMenuManager] Game di-pause.");
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f; // Kembalikan waktu normal

            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.SetActive(false);
            }

            Debug.Log("[PauseMenuManager] Game dilanjutkan.");
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
            // Wajib kembalikan timeScale sebelum pindah scene!
            Time.timeScale = 1f;

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
