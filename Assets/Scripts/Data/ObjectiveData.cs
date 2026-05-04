using System;
using UnityEngine;
using MediProfen.Objectives;

namespace MediProfen.Data
{
    [CreateAssetMenu(menuName = "MediProfen/Objective")]
    public class ObjectiveData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string objectiveId;
    
        [Header("Display")]
        [SerializeField] private string title;
        [TextArea(2, 6)]
        [SerializeField] private string description;

        [Header("Completion")]
        [SerializeField] private ObjectiveCompletionType completionType = ObjectiveCompletionType.Trigger;
        [SerializeField] private string targetId;

        public string ObjectiveId => objectiveId;
        public string Title => title;
        public string Description => description;
        public ObjectiveCompletionType CompletionType => completionType;
        public string TargetId => targetId;

        public bool Matches(string target, ObjectiveCompletionType type)
        {
            if (completionType != ObjectiveCompletionType.Any && completionType != type)
            {
                return false;
            }

            return string.Equals(targetId, target, StringComparison.OrdinalIgnoreCase);
        }
    }
}
