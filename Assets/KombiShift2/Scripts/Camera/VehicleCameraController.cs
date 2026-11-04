using UnityEngine;

namespace KombiShift2.CameraSystem
{
    public sealed class VehicleCameraController : MonoBehaviour
    {
        [System.Serializable]
        private struct View
        {
            public string name;
            public Transform anchor;
            public bool allowOrbit;
        }

        [SerializeField] private View[] views;
        [SerializeField] private KeyCode nextCameraKey = KeyCode.C;
        [SerializeField, Min(0.1f)] private float positionSharpness = 12f;
        [SerializeField, Min(0.1f)] private float rotationSharpness = 12f;
        [SerializeField, Range(10f, 180f)] private float orbitSensitivity = 75f;
        [SerializeField, Range(10f, 85f)] private float orbitPitchLimit = 70f;

        private int currentView;
        private float yaw;
        private float pitch;

        private void Update()
        {
            if (views.Length == 0) return;
            if (Input.GetKeyDown(nextCameraKey))
            {
                currentView = (currentView + 1) % views.Length;
                yaw = 0f;
                pitch = 0f;
            }

            if (views[currentView].allowOrbit && Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * orbitSensitivity * Time.unscaledDeltaTime;
                pitch -= Input.GetAxis("Mouse Y") * orbitSensitivity * Time.unscaledDeltaTime;
                pitch = Mathf.Clamp(pitch, -orbitPitchLimit, orbitPitchLimit);
            }
        }

        private void LateUpdate()
        {
            if (views.Length == 0 || views[currentView].anchor == null) return;
            Transform anchor = views[currentView].anchor;
            float positionT = 1f - Mathf.Exp(-positionSharpness * Time.unscaledDeltaTime);
            float rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.unscaledDeltaTime);
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = Vector3.Lerp(transform.position, anchor.position, positionT);
            transform.rotation = Quaternion.Slerp(transform.rotation, anchor.rotation * orbit, rotationT);
        }
    }
}
