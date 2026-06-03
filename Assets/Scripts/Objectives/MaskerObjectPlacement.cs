using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MaskerObjectPlacement : MonoBehaviour
{
    [Header("Socket")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socketInteractor;

    [Header("Audio (Opsional)")]
    [Tooltip("Komponen AudioSource untuk memutar efek suara")]
    public AudioSource sfxSource;
    [Tooltip("Efek suara saat masker berhasil dipasang")]
    public AudioClip completionSound;

    private bool isPlaced = false;

    private void OnEnable()
    {
        if (socketInteractor == null)
            socketInteractor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();

        if (socketInteractor != null)
            socketInteractor.selectEntered.AddListener(OnMaskerPlaced);
    }

    private void OnDisable()
    {
        if (socketInteractor != null)
            socketInteractor.selectEntered.RemoveListener(OnMaskerPlaced);
    }

    private void OnMaskerPlaced(SelectEnterEventArgs args)
    {
        if (isPlaced) return;
        isPlaced = true;

        Transform maskerTransform = args.interactableObject.transform;

        // Jangan disable XRGrabInteractable saat sedang dipegang socket.
        // XRI bisa melepas object jika interactable/layer-nya dibuat tidak valid.
        var grab = maskerTransform.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null && socketInteractor != null)
        {
            grab.interactionLayers = socketInteractor.interactionLayers;
        }

        if (sfxSource != null && completionSound != null)
        {
            sfxSource.PlayOneShot(completionSound);
        }
    }
}
