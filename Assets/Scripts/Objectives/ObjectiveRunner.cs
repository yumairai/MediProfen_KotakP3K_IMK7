using System;
using UnityEngine;
using MediProfen.Data;

namespace MediProfen.Objectives
{
    public class ObjectiveRunner : MonoBehaviour
    {
        public event Action<ObjectiveData, int, int> ObjectiveChanged;
        public event Action<ScenarioData> ScenarioCompleted;

        private ScenarioData currentScenario;
        private int currentIndex = -1;

        public ScenarioData CurrentScenario => currentScenario;
        public int CurrentIndex => currentIndex;

        public ObjectiveData CurrentObjective
        {
            get
            {
                if (currentScenario == null || currentScenario.Objectives.Count == 0)
                {
                    return null;
                }

                if (currentIndex < 0 || currentIndex >= currentScenario.Objectives.Count)
                {
                    return null;
                }

                return currentScenario.Objectives[currentIndex];
            }
        }

        private void OnEnable()
        {
            ObjectiveEvents.TargetCompleted += HandleTargetCompleted;
        }

        private void OnDisable()
        {
            ObjectiveEvents.TargetCompleted -= HandleTargetCompleted;
        }

        public void BeginScenario(ScenarioData scenario)
        {
            currentScenario = scenario;
            currentIndex = -1;
            AdvanceObjective();
        }

        public void CompleteCurrentObjective()
        {
            AdvanceObjective();
        }

        private void HandleTargetCompleted(ObjectiveEventData eventData)
        {
            var objective = CurrentObjective;
            if (objective == null)
            {
                return;
            }

            if (!objective.Matches(eventData.TargetId, eventData.CompletionType))
            {
                return;
            }

            AdvanceObjective();
        }

        private void AdvanceObjective()
        {
            if (currentScenario == null)
            {
                return;
            }

            currentIndex++;
            if (currentIndex >= currentScenario.Objectives.Count)
            {
                ScenarioCompleted?.Invoke(currentScenario);
                return;
            }

            ObjectiveChanged?.Invoke(CurrentObjective, currentIndex + 1, currentScenario.Objectives.Count);
        }
    }
}
