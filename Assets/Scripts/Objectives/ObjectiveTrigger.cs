using UnityEngine;

namespace MediProfen.Objectives
{
    [RequireComponent(typeof(Collider))]
    public class ObjectiveTrigger : MonoBehaviour
    {
        [SerializeField] private string targetId;
        [SerializeField] private string requiredTag = "Player";

        private void Reset()
        {
            var collider = GetComponent<Collider>();
            collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            {
                return;
            }

            ObjectiveEvents.RaiseTargetCompleted(targetId, ObjectiveCompletionType.Trigger);
        }
    }
}
