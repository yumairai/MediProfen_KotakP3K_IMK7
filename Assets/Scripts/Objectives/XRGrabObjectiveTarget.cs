using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MediProfen.Objectives
{
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class XRGrabObjectiveTarget : MonoBehaviour
    {
        [SerializeField] private string targetId;

        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable interactable;

        private void Awake()
        {
            interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }

        private void OnEnable()
        {
            interactable.selectEntered.AddListener(HandleSelectEntered);
        }

        private void OnDisable()
        {
            interactable.selectEntered.RemoveListener(HandleSelectEntered);
        }

        private void HandleSelectEntered(SelectEnterEventArgs args)
        {
            ObjectiveEvents.RaiseTargetCompleted(targetId, ObjectiveCompletionType.Grab);
        }
    }
}
