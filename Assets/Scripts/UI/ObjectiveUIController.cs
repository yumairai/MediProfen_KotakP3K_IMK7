using System.Text;
using UnityEngine;
using TMPro;
using MediProfen.Objectives;
using MediProfen.Data;

namespace MediProfen.UI
{
    public enum VitalsSimulationMode
    {
        Default,
        CardiacArrest_CPR,
        Choking_Heimlich
    }

    public class ObjectiveUIController : MonoBehaviour
    {
        [SerializeField] private ObjectiveRunner runner;
        [Header("Teks Utama")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Vitals (HR & RR)")]
        [SerializeField] private TextMeshProUGUI hrText;
        [SerializeField] private TextMeshProUGUI rrText;
        [Tooltip("Pilih tipe simulasi penyakit untuk layar vital sign")]
        [SerializeField] private VitalsSimulationMode vitalsMode = VitalsSimulationMode.Default;
        
        [Header("Vitals Blink Settings")]
        [SerializeField] private Color normalColor = Color.black;
        [SerializeField] private Color emergencyColor = Color.red;
        [SerializeField] private float blinkSpeed = 10f; // Kecepatan kedap-kedip

        private float nextVitalsUpdateTime = 0f;
        private int currentHR = 0;
        private int currentRR = 0;
        private bool isScenarioCompleted = false;

        private void OnEnable()
        {
            if (runner != null)
            {
                runner.ObjectiveChanged += HandleObjectiveChanged;
                runner.ScenarioCompleted += HandleScenarioCompleted;
                RefreshHUD();
            }
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.ObjectiveChanged -= HandleObjectiveChanged;
                runner.ScenarioCompleted -= HandleScenarioCompleted;
            }
        }

        private void Update()
        {
            if (runner == null || runner.CurrentScenario == null) return;

            // Update Vitals setiap 1 detik
            if (Time.time >= nextVitalsUpdateTime)
            {
                nextVitalsUpdateTime = Time.time + 1.0f;
                UpdateVitals();
            }

            UpdateBlinkEffect();
        }

        private void UpdateBlinkEffect()
        {
            // Kondisi darurat jika detak jantung atau napas di luar batas normal
            bool isEmergencyHR = (currentHR < 60 || currentHR > 100);
            bool isEmergencyRR = (currentRR < 12 || currentRR > 20);

            // Menggunakan Sinus untuk pergantian warna cepat (0 atau 1)
            Color blinkCol = (Mathf.Sin(Time.time * blinkSpeed) > 0f) ? emergencyColor : normalColor;

            if (hrText != null)
            {
                hrText.color = isEmergencyHR ? blinkCol : normalColor;
            }
            if (rrText != null)
            {
                rrText.color = isEmergencyRR ? blinkCol : normalColor;
            }
        }

        private void UpdateVitals()
        {
            if (vitalsMode == VitalsSimulationMode.CardiacArrest_CPR)
            {
                // Skenario CPR
                // HR 0 selama belum selesai semua
                currentHR = isScenarioCompleted ? Random.Range(60, 80) : 0;

                // RR normal setelah masker napas dipakaikan
                bool maskApplied = isScenarioCompleted;
                if (!isScenarioCompleted)
                {
                    // Cek apakah objektif masker/napas sudah terlewati
                    for (int i = 0; i < runner.CurrentIndex; i++)
                    {
                        string objTitle = runner.CurrentScenario.Objectives[i].Title.ToLower();
                        if (objTitle.Contains("napas") || objTitle.Contains("masker") || objTitle.Contains("ventilasi"))
                        {
                            maskApplied = true;
                            break;
                        }
                    }
                }
                currentRR = maskApplied ? Random.Range(12, 20) : 0;
            }
            else if (vitalsMode == VitalsSimulationMode.Choking_Heimlich)
            {
                // Skenario Tersedak
                // HR fluktuatif tinggi selama belum selesai
                currentHR = isScenarioCompleted ? Random.Range(60, 80) : Random.Range(110, 145);
                // RR 0 selama belum selesai
                currentRR = isScenarioCompleted ? Random.Range(12, 20) : 0;
            }
            else
            {
                // Default
                currentHR = isScenarioCompleted ? Random.Range(60, 80) : Random.Range(80, 100);
                currentRR = isScenarioCompleted ? Random.Range(12, 20) : Random.Range(20, 25);
            }

            if (hrText != null) hrText.text = currentHR.ToString();
            if (rrText != null) rrText.text = currentRR.ToString();
        }

        private void HandleObjectiveChanged(ObjectiveData objective, int index, int total)
        {
            isScenarioCompleted = false;
            RefreshHUD(index, total, false);
        }

        private void HandleScenarioCompleted(ScenarioData scenario)
        {
            isScenarioCompleted = true;
            var total = scenario != null ? scenario.Objectives.Count : 0;
            RefreshHUD(total, total, true);
        }

        private void RefreshHUD()
        {
            if (runner == null || runner.CurrentScenario == null)
            {
                ClearHUD();
                return;
            }

            var total = runner.CurrentScenario.Objectives.Count;
            var currentObjectiveIndex = runner.CurrentIndex < 0 ? 0 : runner.CurrentIndex + 1;
            isScenarioCompleted = (runner.CurrentIndex >= total);
            RefreshHUD(currentObjectiveIndex, total, isScenarioCompleted);
        }

        private void RefreshHUD(int currentObjectiveIndex, int totalObjectives, bool scenarioCompleted)
        {
            var scenario = runner != null ? runner.CurrentScenario : null;
            if (scenario == null)
            {
                ClearHUD();
                return;
            }

            if (titleText != null)
            {
                titleText.text = scenario.DisplayName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = BuildObjectiveList(scenario, currentObjectiveIndex, scenarioCompleted);
            }

            if (progressText != null)
            {
                progressText.text = $"{Mathf.Clamp(currentObjectiveIndex, 0, totalObjectives)} / {totalObjectives}";
            }

            if (statusText != null)
            {
                // GAWAT merah, STABIL hijau
                statusText.text = scenarioCompleted ? "<color=#00C853>STABIL</color>" : "<color=#D50000>GAWAT</color>";
            }
        }

        private string BuildObjectiveList(ScenarioData scenario, int currentObjectiveIndex, bool scenarioCompleted)
        {
            if (scenario == null || scenario.Objectives == null || scenario.Objectives.Count == 0)
            {
                return "No objectives available.";
            }

            var activeIndex = Mathf.Max(0, currentObjectiveIndex - 1);
            var hasActiveObjective = !scenarioCompleted && currentObjectiveIndex > 0;
            var builder = new StringBuilder();

            for (var i = 0; i < scenario.Objectives.Count; i++)
            {
                var objective = scenario.Objectives[i];

                if (scenarioCompleted || i < activeIndex)
                {
                    // Berhasil: Hijau
                    builder.AppendLine($"<color=#02FF00>\u2713 {objective.Title}</color>");
                }
                else if (hasActiveObjective && i == activeIndex)
                {
                    // Sedang berjalan: Orange
                    builder.AppendLine($"<color=#FF9800>• {objective.Title}</color>");
                }
                else
                {
                    // Akan datang: Hitam
                    builder.AppendLine($"<color=#000000>• {objective.Title}</color>");
                }
            }

            return builder.ToString().TrimEnd();
        }

        private void ClearHUD()
        {
            if (titleText != null) titleText.text = string.Empty;
            if (descriptionText != null) descriptionText.text = string.Empty;
            if (progressText != null) progressText.text = string.Empty;
            if (statusText != null) statusText.text = string.Empty;
            if (hrText != null) hrText.text = "0";
            if (rrText != null) rrText.text = "0";
        }
    }
}
