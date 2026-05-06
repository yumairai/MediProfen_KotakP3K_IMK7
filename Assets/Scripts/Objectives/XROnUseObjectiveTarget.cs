using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MediProfen.Objectives
{
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable))]
    public class XROnUseObjectiveTarget : MonoBehaviour
    {
        [SerializeField] private string targetId;

        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;

        private void Awake()
        {
            interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        }

        private void OnEnable()
        {
            interactable.activated.AddListener(HandleActivated);
        }

        private void OnDisable()
        {
            interactable.activated.RemoveListener(HandleActivated);
        }

        private void HandleActivated(ActivateEventArgs args)
        {
            ObjectiveEvents.RaiseTargetCompleted(targetId, ObjectiveCompletionType.Use);
        }
    }
}
