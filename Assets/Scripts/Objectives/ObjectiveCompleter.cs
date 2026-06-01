using UnityEngine;
using MediProfen.Objectives;

public class ObjectiveCompleter : MonoBehaviour
{
    [Tooltip("ID target objective (sama dengan TargetId di ScriptableObject ObjectiveData)")]
    public string targetId;
    public ObjectiveCompletionType completionType = ObjectiveCompletionType.Trigger;

    public void CompleteTarget()
    {
        ObjectiveEvents.RaiseTargetCompleted(targetId, completionType);
        Debug.Log($"[ObjectiveCompleter] Sinyal penyelesaian '{targetId}' terkirim.");
    }
}