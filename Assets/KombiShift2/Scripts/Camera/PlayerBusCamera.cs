using UnityEngine;

namespace KombiShift2.CameraSystem
{
    public sealed class PlayerBusCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        private readonly Vector3[] offsets = { new(0f, 4.8f, -11f), new(-1.2f, 3.1f, -7.2f), new(-.55f, 2.15f, 1.65f) };
        private int view;

        public void SetTarget(Transform value) => target = value;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C)) view = (view + 1) % offsets.Length;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = target.TransformPoint(offsets[view]);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-5.5f * Time.deltaTime));
            Vector3 look = target.position + target.forward * (view == 2 ? 12f : 4f) + Vector3.up * 1.5f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look - transform.position), 1f - Mathf.Exp(-7f * Time.deltaTime));
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(20, 18, 650, 28), "W/S DRIVE   A/D STEER   SPACE HANDBRAKE   C CAMERA   R RESET   ESC MENU");
        }
    }
}
