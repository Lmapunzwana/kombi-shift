using UnityEngine;

namespace KombiShift2.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class KombiVehicleController : MonoBehaviour
    {
        [System.Serializable]
        public struct Axle
        {
            public WheelCollider leftCollider;
            public WheelCollider rightCollider;
            public Transform leftVisual;
            public Transform rightVisual;
            public bool steering;
            public bool powered;
            public bool braking;
        }

        [Header("Wheels")]
        [SerializeField] private Axle[] axles;

        [Header("Handling")]
        [SerializeField, Min(1f)] private float motorTorque = 4200f;
        [SerializeField, Min(1f)] private float serviceBrakeTorque = 8500f;
        [SerializeField, Min(1f)] private float handbrakeTorque = 14000f;
        [SerializeField, Range(5f, 50f)] private float maximumSteeringAngle = 28f;
        [SerializeField, Min(1f)] private float maximumForwardSpeedKph = 100f;
        [SerializeField, Min(1f)] private float maximumReverseSpeedKph = 25f;
        [SerializeField] private Vector3 centreOfMassOffset = new(0f, -0.75f, 0.1f);
        [SerializeField, Min(0f)] private float aerodynamicDownforce = 35f;

        [Header("Input")]
        [SerializeField] private string steeringAxis = "Horizontal";
        [SerializeField] private string throttleAxis = "Vertical";
        [SerializeField] private KeyCode handbrakeKey = KeyCode.Space;
        [SerializeField] private KeyCode resetKey = KeyCode.R;

        private Rigidbody body;
        private Vector3 resetPosition;
        private Quaternion resetRotation;

        public float SpeedKph => body == null ? 0f : Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.centerOfMass += centreOfMassOffset;
            resetPosition = transform.position;
            resetRotation = transform.rotation;
        }

        private void Update()
        {
            UpdateWheelVisuals();
            if (Input.GetKeyDown(resetKey)) ResetVehicle();
        }

        private void FixedUpdate()
        {
            float steering = Mathf.Clamp(Input.GetAxisRaw(steeringAxis), -1f, 1f);
            float throttle = Mathf.Clamp(Input.GetAxisRaw(throttleAxis), -1f, 1f);
            bool handbrake = Input.GetKey(handbrakeKey);

            float signedSpeed = SpeedKph;
            bool askingOppositeDirection = Mathf.Abs(signedSpeed) > 2f && Mathf.Sign(throttle) != Mathf.Sign(signedSpeed);
            float requestedLimit = throttle < 0f ? maximumReverseSpeedKph : maximumForwardSpeedKph;
            float driveTorque = Mathf.Abs(signedSpeed) < requestedLimit && !askingOppositeDirection
                ? throttle * motorTorque
                : 0f;
            float brakeTorque = askingOppositeDirection ? Mathf.Abs(throttle) * serviceBrakeTorque : 0f;

            foreach (Axle axle in axles)
            {
                if (axle.steering)
                {
                    float steerAngle = steering * maximumSteeringAngle;
                    axle.leftCollider.steerAngle = steerAngle;
                    axle.rightCollider.steerAngle = steerAngle;
                }

                axle.leftCollider.motorTorque = axle.powered ? driveTorque : 0f;
                axle.rightCollider.motorTorque = axle.powered ? driveTorque : 0f;
                float axleBrake = handbrake && axle.braking ? handbrakeTorque : brakeTorque;
                axle.leftCollider.brakeTorque = axleBrake;
                axle.rightCollider.brakeTorque = axleBrake;
            }

            body.AddForce(-transform.up * (body.linearVelocity.magnitude * aerodynamicDownforce));
        }

        public void ResetVehicle()
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = resetPosition + Vector3.up;
            body.rotation = Quaternion.Euler(0f, resetRotation.eulerAngles.y, 0f);
        }

        public void SetResetPoint(Transform point)
        {
            resetPosition = point.position;
            resetRotation = point.rotation;
        }

        private void UpdateWheelVisuals()
        {
            foreach (Axle axle in axles)
            {
                UpdateWheel(axle.leftCollider, axle.leftVisual);
                UpdateWheel(axle.rightCollider, axle.rightVisual);
            }
        }

        private static void UpdateWheel(WheelCollider wheel, Transform visual)
        {
            if (wheel == null || visual == null) return;
            wheel.GetWorldPose(out Vector3 position, out Quaternion rotation);
            visual.SetPositionAndRotation(position, rotation);
        }
    }
}
