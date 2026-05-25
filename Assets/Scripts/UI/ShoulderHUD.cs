using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MediProfen.UI
{
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class ShoulderHUD : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Masukkan Main Camera dari XR Origin ke sini")]
        public Transform vrCamera;

        [Header("Settings")]
        [Tooltip("Posisi offset dari kamera. X negatif = kiri, Y negatif = bawah, Z positif = depan")]
        public Vector3 positionOffset = new Vector3(-0.5f, -0.2f, 0.2f);

        [Tooltip("Sudut rotasi offset (jika tablet perlu dimiringkan menghadap wajah)")]
        public Vector3 rotationOffset = new Vector3(0, 30f, 0);

        [Tooltip("Kecepatan tablet mengikuti putaran badan")]
        public float smoothSpeed = 8f;

        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
        private Rigidbody rb;
        private bool isGrabbed = false;

        private void Start()
        {
            if (vrCamera == null)
            {
                if (Camera.main != null)
                {
                    vrCamera = Camera.main.transform;
                }
                else
                {
                    Debug.LogWarning("[ShoulderHUD] vrCamera belum di-assign!");
                }
            }

            // Ambil komponen grab dan rigidbody
            grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();

            if (rb != null)
            {
                // Tablet melayang, jadi matikan gravitasi dan jadikan kinematic secara default
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            if (grabInteractable != null)
            {
                // Mendaftarkan event saat tablet dipegang dan dilepas
                grabInteractable.selectEntered.AddListener(OnGrabbed);
                grabInteractable.selectExited.AddListener(OnReleased);

                // Rekomendasi: Gunakan Kinematic agar pergerakannya mulus dan tidak bertabrakan aneh dengan tembok
                grabInteractable.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Kinematic;
            }
        }

        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.selectExited.RemoveListener(OnReleased);
            }
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            isGrabbed = true;
            // Saat dipegang, kita berhenti memaksa posisi ke bahu agar controller bebas membawanya
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            isGrabbed = false;
            // Hapus sisa lemparan / momentum
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void LateUpdate()
        {
            if (vrCamera == null) return;

            // Jika sedang dipegang oleh player, biarkan XRGrabInteractable yang memindahkan posisinya ke tangan!
            if (isGrabbed) return;

            // 1. Dapatkan rotasi Yaw (kiri-kanan) dari kamera. 
            Vector3 cameraEuler = vrCamera.eulerAngles;
            Quaternion bodyYawRotation = Quaternion.Euler(0, cameraEuler.y, 0);

            // 2. Tentukan target posisi di bahu kiri berdasarkan arah hadap badan
            Vector3 targetPosition = vrCamera.position + (bodyYawRotation * positionOffset);

            // 3. Tentukan target rotasi (mengarah searah badan + kemiringan tambahan agar menghadap tengah)
            Quaternion targetRotation = bodyYawRotation * Quaternion.Euler(rotationOffset);

            // 4. Terapkan pergerakan untuk kembali ke bahu / mengikuti bahu
            if (smoothSpeed <= 0f)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
            }
        }
    }
}
