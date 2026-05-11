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
            }
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.ObjectiveChanged -= HandleObjectiveChanged;
            }
        }

        private void HandleObjectiveChanged(ObjectiveData objective, int index, int total)
        {
            if (objective == null)
            {
                if (titleText != null) titleText.text = "All Objectives Complete!";
                if (descriptionText != null) descriptionText.text = "Skenario Selesai.";
                if (progressText != null) progressText.text = $"{total}/{total}";
                if (statusText != null) statusText.text = "<color=green>COMPLETED</color>";
                return;
            }

            if (titleText != null) titleText.text = objective.Title;
            if (descriptionText != null) descriptionText.text = objective.Description;
            if (progressText != null) progressText.text = $"Progress: {index + 1} / {total}";
            if (statusText != null) statusText.text = "<color=yellow>IN PROGRESS</color>";
        }
    }
}
