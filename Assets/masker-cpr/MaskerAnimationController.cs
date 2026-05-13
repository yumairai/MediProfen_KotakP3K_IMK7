using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MaskerAnimationController : MonoBehaviour
{
    public Animator animator;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
        grabInteractable.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Cek jika yang memegang (interactor) adalah sebuah Socket (bukan Controller tangan)
        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor)
        {
            animator.SetBool("IsAttached", true);
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        // Matikan animasi saat dilepas dari apapun (Socket atau Tangan)
        animator.SetBool("IsAttached", false);
    }
}