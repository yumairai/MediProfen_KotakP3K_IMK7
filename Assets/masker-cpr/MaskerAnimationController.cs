using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MaskerAnimationController : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;

    [Header("Audio (Opsional)")]
    [Tooltip("Komponen AudioSource untuk memutar efek suara masker")]
    public AudioSource sfxSource;
    [Tooltip("Efek suara saat masker di-grab oleh tangan/controller")]
    public AudioClip grabSound;

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
            if (animator != null)
            {
                animator.SetBool("IsAttached", true);
            }
            return;
        }

        if (sfxSource != null && grabSound != null)
        {
            sfxSource.PlayOneShot(grabSound);
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        // Matikan animasi saat dilepas dari apapun (Socket atau Tangan)
        if (animator != null)
        {
            animator.SetBool("IsAttached", false);
        }
    }
}
