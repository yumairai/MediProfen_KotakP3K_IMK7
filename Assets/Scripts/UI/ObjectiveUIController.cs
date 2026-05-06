using UnityEngine;
using UnityEngine.UI;
using MediProfen.Objectives;
using MediProfen.Data;

namespace MediProfen.UI
{
    public class ObjectiveUIController : MonoBehaviour
    {
        [SerializeField] private ObjectiveRunner runner;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text progressText;

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
            if (titleText != null)
            {
                titleText.text = objective != null ? objective.Title : "";
            }

            if (descriptionText != null)
            {
                descriptionText.text = objective != null ? objective.Description : "";
            }

            if (progressText != null)
            {
                progressText.text = $"{index}/{total}";
            }
        }
    }
}
