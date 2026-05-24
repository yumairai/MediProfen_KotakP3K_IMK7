using UnityEngine;

namespace MediProfen.UI
{
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

        [Tooltip("Kecepatan tablet mengikuti putaran badan (0 = instan, nilai kecil = lebih lambat/halus)")]
        public float smoothSpeed = 8f;

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
        }

        private void LateUpdate()
        {
            if (vrCamera == null) return;

            // 1. Dapatkan rotasi Yaw (kiri-kanan) dari kamera. 
            // Kita abaikan rotasi Pitch (atas-bawah) dan Roll (miring) agar orientasi tablet selalu tegak.
            Vector3 cameraEuler = vrCamera.eulerAngles;
            Quaternion bodyYawRotation = Quaternion.Euler(0, cameraEuler.y, 0);

            // 2. Tentukan target posisi di bahu kiri berdasarkan arah hadap badan
            Vector3 targetPosition = vrCamera.position + (bodyYawRotation * positionOffset);

            // 3. Tentukan target rotasi (mengarah searah badan + kemiringan tambahan agar menghadap tengah)
            Quaternion targetRotation = bodyYawRotation * Quaternion.Euler(rotationOffset);

            // 4. Terapkan pergerakan
            if (smoothSpeed <= 0f)
            {
                // Bergerak secara instan
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
            else
            {
                // Bergerak dengan efek 'Lazy Follow' yang mulus
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
            }
        }
    }
}
