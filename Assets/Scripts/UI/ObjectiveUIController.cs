using System.Text;
using UnityEngine;
using TMPro;
using MediProfen.Objectives;
using MediProfen.Data;

namespace MediProfen.UI
{
    public class ObjectiveUIController : MonoBehaviour
    {
        [SerializeField] private ObjectiveRunner runner;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI statusText;

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

        private void HandleObjectiveChanged(ObjectiveData objective, int index, int total)
        {
            RefreshHUD(index, total, false);
        }

        private void HandleScenarioCompleted(ScenarioData scenario)
        {
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
            RefreshHUD(currentObjectiveIndex, total, false);
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
                progressText.text = $"Progress: {Mathf.Clamp(currentObjectiveIndex, 0, totalObjectives)} / {totalObjectives}";
            }

            if (statusText != null)
            {
                statusText.text = scenarioCompleted ? "<color=green>SELESAI</color>" : "<color=yellow>SEDANG BERJALAN</color>";
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
                    builder.AppendLine($"<color=#00C853>\u2713 {objective.Title}</color>");
                }
                else if (hasActiveObjective && i == activeIndex)
                {
                    builder.AppendLine($"<color=#FFD54F>• {objective.Title}</color>");
                }
                else
                {
                    builder.AppendLine($"<color=#B0BEC5>• {objective.Title}</color>");
                }
            }

            return builder.ToString().TrimEnd();
        }

        private void ClearHUD()
        {
            if (titleText != null)
            {
                titleText.text = string.Empty;
            }

            if (descriptionText != null)
            {
                descriptionText.text = string.Empty;
            }

            if (progressText != null)
            {
                progressText.text = string.Empty;
            }

            if (statusText != null)
            {
                statusText.text = string.Empty;
            }
        }
    }
}
