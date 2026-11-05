using UnityEngine;

namespace KombiShift2.CameraSystem
{
    public sealed class PlayerBusCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Range(20f, 90f)] private float baseFieldOfView = 58f;
        [SerializeField, Range(20f, 90f)] private float highSpeedFieldOfView = 66f;
        [SerializeField, Min(0f)] private float orbitSensitivity = 2.2f;
        [SerializeField, Min(0.01f)] private float collisionRadius = .28f;
        [SerializeField] private LayerMask collisionMask = ~0;
        private readonly Vector3[] offsets = { new(0f, 4.8f, -11f), new(-1.2f, 3.1f, -7.2f), new(-.55f, 2.15f, 1.65f) };
        private int view;
        private float orbitYaw;
        private float orbitPitch;
        private Camera cameraComponent;

        private void Awake() => cameraComponent = GetComponent<Camera>();

        public void SetTarget(Transform value) => target = value;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                view = (view + 1) % offsets.Length;
                orbitYaw = orbitPitch = 0f;
            }
            if (view < 2 && Input.GetMouseButton(1))
            {
                orbitYaw += Input.GetAxis("Mouse X") * orbitSensitivity;
                orbitPitch = Mathf.Clamp(orbitPitch - Input.GetAxis("Mouse Y") * orbitSensitivity, -18f, 42f);
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Quaternion orbit = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            Vector3 localOffset = view < 2 ? orbit * offsets[view] : offsets[view];
            Vector3 desired = target.TransformPoint(localOffset);
            Vector3 pivot = target.position + Vector3.up * 1.45f;
            Vector3 ray = desired - pivot;
            if (view < 2 && Physics.SphereCast(pivot, collisionRadius, ray.normalized, out RaycastHit hit, ray.magnitude, collisionMask, QueryTriggerInteraction.Ignore))
                desired = pivot + ray.normalized * Mathf.Max(.5f, hit.distance - collisionRadius);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-5.5f * Time.deltaTime));
            Vector3 look = target.position + target.forward * (view == 2 ? 12f : 4f) + Vector3.up * 1.5f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look - transform.position), 1f - Mathf.Exp(-7f * Time.deltaTime));

            if (cameraComponent != null)
            {
                KombiShift2.Vehicle.PlayerBusController vehicle = target.GetComponent<KombiShift2.Vehicle.PlayerBusController>();
                float speed = vehicle == null ? 0f : Mathf.Clamp01(Mathf.Abs(vehicle.SpeedKph) / 75f);
                cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, Mathf.Lerp(baseFieldOfView, highSpeedFieldOfView, speed), 1f - Mathf.Exp(-3f * Time.deltaTime));
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(20, 18, 850, 28), "W/S DRIVE   A/D STEER   SPACE HANDBRAKE   C CAMERA   RIGHT-DRAG LOOK   R RESET   ESC MENU");
        }
    }
}
