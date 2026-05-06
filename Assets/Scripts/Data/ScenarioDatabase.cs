using System.Collections.Generic;
using UnityEngine;

namespace MediProfen.Data
{
    [CreateAssetMenu(menuName = "MediProfen/Scenario Database")]
    public class ScenarioDatabase : ScriptableObject
    {
        [SerializeField] private List<ScenarioData> scenarios = new List<ScenarioData>();

        public IReadOnlyList<ScenarioData> Scenarios => scenarios;

        public ScenarioData GetById(string scenarioId)
        {
            if (string.IsNullOrEmpty(scenarioId))
            {
                return null;
            }

            for (int i = 0; i < scenarios.Count; i++)
            {
                var scenario = scenarios[i];
                if (scenario != null && scenario.ScenarioId == scenarioId)
                {
                    return scenario;
                }
            }

            return null;
        }
    }
}
