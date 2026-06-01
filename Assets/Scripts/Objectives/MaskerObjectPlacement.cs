using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MaskerObjectPlacement : MonoBehaviour
{
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socketInteractor;

    private void OnEnable()
    {
        if (socketInteractor == null)
            socketInteractor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();

        socketInteractor.selectEntered.AddListener(OnMaskerPlaced);
    }

    private void OnDisable()
    {
        if (socketInteractor != null)
            socketInteractor.selectEntered.RemoveListener(OnMaskerPlaced);
    }

    private void OnMaskerPlaced(SelectEnterEventArgs args)
    {
        // Set parent masker ke socket agar selalu ikut posisi socket
        args.interactableObject.transform.SetParent(this.transform);

        // Matikan XR Grab Interactable pada masker
        var grab = args.interactableObject.transform.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
        {
            grab.enabled = false;
            grab.interactionLayers = 0; // Tidak bisa diinteraksi oleh interactor manapun
        }

        // Set Rigidbody menjadi kinematic agar tidak jatuh
        var rb = args.interactableObject.transform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }
}