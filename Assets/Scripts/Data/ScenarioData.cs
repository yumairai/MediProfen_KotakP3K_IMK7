using System.Collections.Generic;
using UnityEngine;
using MediProfen.Objectives;

namespace MediProfen.Data
{
    [CreateAssetMenu(menuName = "MediProfen/Scenario")]
    public class ScenarioData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string scenarioId;
        [SerializeField] private string displayName;

        [Header("Content")]
        [TextArea(2, 6)]
        [SerializeField] private string description;
        [SerializeField] private string sceneName;
        [SerializeField] private List<ObjectiveData> objectives = new List<ObjectiveData>();

        public string ScenarioId => scenarioId;
        public string DisplayName => displayName;
        public string Description => description;
        public string SceneName => sceneName;
        public IReadOnlyList<ObjectiveData> Objectives => objectives;
    }
}
