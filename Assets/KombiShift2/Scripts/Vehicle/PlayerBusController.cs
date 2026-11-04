using UnityEngine;

namespace KombiShift2.Vehicle
{
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class PlayerBusController : MonoBehaviour
    {
        [Header("Speed")]
        [SerializeField] private float acceleration = 5.5f;
        [SerializeField] private float reverseAcceleration = 3.5f;
        [SerializeField] private float braking = 11f;
        [SerializeField] private float coastDeceleration = 1.1f;
        [SerializeField] private float maximumForwardSpeedKph = 75f;
        [SerializeField] private float maximumReverseSpeedKph = 16f;

        [Header("Steering")]
        [SerializeField] private float lowSpeedSteerRate = 42f;
        [SerializeField] private float highSpeedSteerRate = 15f;
        [SerializeField] private float steeringResponse = 5f;

        private Rigidbody body;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float speed;
        private float smoothSteering;
        private float throttleInput;
        private float steeringInput;
        private bool handbrake;

        public float SpeedKph => speed * 3.6f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            // Kinematic movement makes player input authoritative. The imported city road
            // contains dense legacy mesh colliders that prevented force-driven translation.
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        private void Update()
        {
            throttleInput = Mathf.Clamp(Input.GetAxisRaw("Vertical"), -1f, 1f);
            steeringInput = Mathf.Clamp(Input.GetAxisRaw("Horizontal"), -1f, 1f);
            handbrake = Input.GetKey(KeyCode.Space);
            if (Input.GetKeyDown(KeyCode.R)) ResetBus();
            if (Input.GetKeyDown(KeyCode.Escape)) UnityEngine.SceneManagement.SceneManager.LoadScene("KombiMainMenu");
        }

        private void FixedUpdate()
        {
            UpdateSpeed();
            UpdateSteering();

            Vector3 displacement = transform.forward * (speed * Time.fixedDeltaTime);
            body.MovePosition(body.position + displacement);
        }

        private void UpdateSpeed()
        {
            float forwardLimit = maximumForwardSpeedKph / 3.6f;
            float reverseLimit = maximumReverseSpeedKph / 3.6f;

            if (handbrake)
            {
                speed = Mathf.MoveTowards(speed, 0f, braking * 1.5f * Time.fixedDeltaTime);
                return;
            }

            if (throttleInput > .01f)
            {
                if (speed < -.1f) speed = Mathf.MoveTowards(speed, 0f, braking * Time.fixedDeltaTime);
                else speed = Mathf.MoveTowards(speed, forwardLimit, acceleration * throttleInput * Time.fixedDeltaTime);
            }
            else if (throttleInput < -.01f)
            {
                if (speed > .1f) speed = Mathf.MoveTowards(speed, 0f, braking * Time.fixedDeltaTime);
                else speed = Mathf.MoveTowards(speed, -reverseLimit, reverseAcceleration * -throttleInput * Time.fixedDeltaTime);
            }
            else
            {
                speed = Mathf.MoveTowards(speed, 0f, coastDeceleration * Time.fixedDeltaTime);
            }
        }

        private void UpdateSteering()
        {
            smoothSteering = Mathf.MoveTowards(smoothSteering, steeringInput, steeringResponse * Time.fixedDeltaTime);
            if (Mathf.Abs(speed) < .15f) return; // no stationary side-to-side rotation

            float speedRatio = Mathf.Clamp01(Mathf.Abs(speed) / (maximumForwardSpeedKph / 3.6f));
            float rate = Mathf.Lerp(lowSpeedSteerRate, highSpeedSteerRate, speedRatio);
            float reverseDirection = speed < 0f ? -1f : 1f;
            body.MoveRotation(body.rotation * Quaternion.Euler(0f, smoothSteering * reverseDirection * rate * Time.fixedDeltaTime, 0f));
        }

        private void ResetBus()
        {
            speed = 0f;
            smoothSteering = 0f;
            body.position = spawnPosition;
            body.rotation = spawnRotation;
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(Screen.width - 180f, 18f, 160f, 58f), GUIContent.none);
            GUI.Label(new Rect(Screen.width - 165f, 26f, 140f, 22f), $"SPEED  {Mathf.Abs(SpeedKph):0} km/h");
            GUI.Label(new Rect(Screen.width - 165f, 48f, 140f, 20f), speed < -.1f ? "REVERSE" : handbrake ? "HANDBRAKE" : "DRIVE");
        }
    }
}
