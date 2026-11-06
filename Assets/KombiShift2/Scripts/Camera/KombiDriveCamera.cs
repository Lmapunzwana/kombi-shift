using UnityEngine;

namespace KombiShift2.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class KombiDriveCamera : MonoBehaviour
    {
        [SerializeField] private Transform vehicle;
        [SerializeField] private Vector3 chaseOffset = new(0f, 3.2f, -7.2f);
        // Matches the Camera_Cabin marker authored for this exact Hiace mesh.
        [SerializeField] private Vector3 cabOffset = new(-.262f, 1.18f, 1.193f);
        [SerializeField] private float positionDamping = 9f;
        private bool cabView;
        private float cabYaw;
        private float cabPitch;

        public void SetVehicle(Transform value) => vehicle = value;

        private void LateUpdate()
        {
            if (vehicle == null) return;
            if (Input.GetKeyDown(KeyCode.C)) cabView = !cabView;
            if (cabView)
            {
                cabYaw = Mathf.Clamp(cabYaw + Input.GetAxis("Mouse X") * 2.2f, -75f, 75f);
                cabPitch = Mathf.Clamp(cabPitch - Input.GetAxis("Mouse Y") * 1.7f, -28f, 32f);
                transform.SetPositionAndRotation(vehicle.TransformPoint(cabOffset), vehicle.rotation * Quaternion.Euler(cabPitch, cabYaw, 0f));
                Camera camera = GetComponent<Camera>(); camera.fieldOfView = 66f; camera.nearClipPlane = .035f;
                return;
            }
            Vector3 wanted = vehicle.TransformPoint(chaseOffset);
            Vector3 focus = vehicle.TransformPoint(new Vector3(0f, 1.1f, 2f));
            transform.position = Vector3.Lerp(transform.position, wanted, 1f - Mathf.Exp(-positionDamping * Time.deltaTime));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(focus - transform.position), 1f - Mathf.Exp(-positionDamping * Time.deltaTime));
            Camera chaseCamera = GetComponent<Camera>(); chaseCamera.fieldOfView = 62f; chaseCamera.nearClipPlane = .2f;
        }
    }
}
