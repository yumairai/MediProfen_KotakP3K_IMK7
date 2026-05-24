using UnityEngine;
using Unity.XR.CoreUtils;

namespace MediProfen.Core
{
    [RequireComponent(typeof(XROrigin))]
    public class AlignCameraOnStart : MonoBehaviour
    {
        [Tooltip("Jalankan penyelarasan rotasi saat scene dimulai?")]
        public bool alignRotation = true;

        [Tooltip("Jalankan penyelarasan posisi (agar kepala tepat di tengah XR Origin)?")]
        public bool alignPosition = false;

        private void Start()
        {
            XROrigin xrOrigin = GetComponent<XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                if (alignRotation)
                {
                    // 1. Simpan arah rotasi yang Anda atur di Inspector (Target)
                    float targetYaw = transform.eulerAngles.y;

                    // 2. Baca arah putaran fisik kepala player saat ini di dunia nyata
                    float physicalHeadYaw = xrOrigin.Camera.transform.localEulerAngles.y;

                    // 3. Putar XR Origin untuk mengimbangi (Offset) arah kepala player.
                    // Hasilnya: Saat player menatap ke depan secara fisik, di game dia akan menghadap targetYaw.
                    transform.rotation = Quaternion.Euler(0, targetYaw - physicalHeadYaw, 0);
                }

                if (alignPosition)
                {
                    // Paskan posisi X/Z agar mata player tepat berada di titik pusat XR Origin
                    Vector3 headLocalPos = xrOrigin.Camera.transform.localPosition;
                    headLocalPos.y = 0; // Biarkan tinggi Y alami apa adanya
                    transform.position = transform.position - transform.TransformDirection(headLocalPos);
                }
                
                Debug.Log("[AlignCameraOnStart] Camera aligned to XR Origin spawn point.");
            }
        }
    }
}
