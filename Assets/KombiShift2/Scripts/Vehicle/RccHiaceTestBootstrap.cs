using System.Collections;
using UnityEngine;

namespace KombiShift2.Vehicle
{
    public sealed class RccHiaceTestBootstrap : MonoBehaviour
    {
        [SerializeField] private RCCP_CarController vehicle;
        [SerializeField, Min(1f)] private float smokeTestDuration = 6f;
        [SerializeField, Range(0.1f, 1f)] private float smokeTestThrottle = 0.65f;

#if UNITY_EDITOR
        [System.NonSerialized] public bool verificationInput;
        [System.NonSerialized] public float verificationThrottle;
        [System.NonSerialized] public float verificationSteering;
        [System.NonSerialized] public float verificationBrake;
        [System.NonSerialized] public bool verificationPark;
#endif
        private KombiVehicleCondition condition;
        private KombiShift2.Gameplay.KombiShiftGameDirector director;
        private bool interactiveInputEnabled;
        private float smoothedSteering;
        private float smoothedThrottle;
        private float smoothedBrake;

        private IEnumerator Start()
        {
            if (vehicle == null)
            {
                Debug.LogError("KOMBI_RCC_TEST No RCC Hiace vehicle is assigned.");
                yield break;
            }

            if(vehicle.GetComponent<KombiTurnIndicators>()==null)
                vehicle.gameObject.AddComponent<KombiTurnIndicators>();

            yield return null;
            bool automated = Application.isBatchMode && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--kombi-smoke") >= 0;
            RCCP.RegisterPlayerVehicle(vehicle, true, true);
            var powertrain=vehicle.GetComponent<KombiPowertrain>();
            if(powertrain!=null)powertrain.Configure();

            if (!automated)
            {
                RCCP.SetControl(vehicle, true);
                RCCP.SetExternalControl(vehicle, true);
                RCCP.SetEngine(vehicle, true);
                RCCP.SetAutomaticGear(vehicle, true);
                interactiveInputEnabled = true;
                Debug.Log("KOMBI_RCC_TEST Direct keyboard controls enabled: WASD/arrows, Space handbrake.");
                yield break;
            }

            RCCP.SetControl(vehicle, true);
            RCCP.SetExternalControl(vehicle, true);
            RCCP.SetEngine(vehicle, true);
            RCCP.SetAutomaticGear(vehicle, true);

            // Allow the suspension to settle onto the test surface before the
            // measured throttle interval begins.
            yield return new WaitForSeconds(2f);
            Vector3 startPosition = vehicle.transform.position;
            Vector3 startForward = vehicle.transform.forward;
            Rigidbody rigidbody = vehicle.GetComponent<Rigidbody>();
            WheelCollider[] wheels = vehicle.GetComponentsInChildren<WheelCollider>(true);
            int groundedWheels = 0;
            foreach (WheelCollider wheel in wheels)
            {
                if (wheel.GetGroundHit(out _)) groundedWheels++;
            }
            Debug.Log($"KOMBI_RCC_CONTACT rootY={vehicle.transform.position.y:F2} rigidbodyKinematic={rigidbody.isKinematic} gravity={rigidbody.useGravity} wheelCount={wheels.Length} groundedWheels={groundedWheels}.");

            OverrideInputs(smokeTestThrottle, 0f, 0f);
            yield return new WaitForSeconds(smokeTestDuration);

            Vector3 accelerationDisplacement = vehicle.transform.position - startPosition;
            float forwardDistance = Vector3.Dot(accelerationDisplacement, startForward);
            float lateralDistance = Mathf.Abs(Vector3.Dot(accelerationDisplacement, Vector3.Cross(Vector3.up, startForward).normalized));
            float accelerationSpeed = vehicle.absoluteSpeed;
            bool accelerationPassed = forwardDistance > 2f && lateralDistance < 2f && accelerationSpeed > 5f;

            Vector3 steeringStartForward = vehicle.transform.forward;
            OverrideInputs(0.45f, 0.35f, 0f);
            yield return new WaitForSeconds(2f);
            float headingChange = Mathf.Abs(Vector3.SignedAngle(steeringStartForward, vehicle.transform.forward, Vector3.up));
            bool steeringPassed = headingChange > 3f;

            float brakingStartSpeed = vehicle.absoluteSpeed;
            OverrideInputs(0f, 0f, 1f);
            float brakingElapsed = 0f;
            while (vehicle.absoluteSpeed > 1f && brakingElapsed < 5f)
            {
                brakingElapsed += Time.deltaTime;
                yield return null;
            }
            float brakingEndSpeed = vehicle.absoluteSpeed;
            bool brakingPassed = brakingEndSpeed < 1f && brakingStartSpeed > 5f;

            Vector3 reverseStartPosition = vehicle.transform.position;
            Vector3 reverseReferenceForward = vehicle.transform.forward;
            OverrideInputs(0f, 0f, 1f);
            yield return new WaitForSeconds(3f);
            float reverseDistance = -Vector3.Dot(vehicle.transform.position - reverseStartPosition, reverseReferenceForward);
            bool reversePassed = reverseDistance > 1f && vehicle.direction < 0;

            vehicle.Inputs.DisableOverrideInputs();
            float uprightness = Vector3.Dot(vehicle.transform.up, Vector3.up);
            bool passed = accelerationPassed && steeringPassed && brakingPassed && reversePassed && uprightness > 0.65f && vehicle.IsGrounded;

            string result = $"KOMBI_RCC_SMOKE {(passed ? "PASS" : "FAIL")} accel={accelerationPassed} forward={forwardDistance:F2}m drift={lateralDistance:F2}m accelSpeed={accelerationSpeed:F1}kph steer={steeringPassed} heading={headingChange:F1}deg brake={brakingPassed} speed={brakingStartSpeed:F1}->{brakingEndSpeed:F1}kph reverse={reversePassed} reverseDistance={reverseDistance:F2}m direction={vehicle.direction} upright={uprightness:F2} grounded={vehicle.IsGrounded} engine={vehicle.engineRunning} rpm={vehicle.engineRPM:F0} gear={vehicle.currentGear}.";
            if (passed) Debug.Log(result);
            else Debug.LogError(result);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(passed ? 0 : 1);
#else
            Application.Quit(passed ? 0 : 1);
#endif
        }

        private void Update()
        {
            if (!interactiveInputEnabled || vehicle == null)
            {
                return;
            }

            float requestedThrottle = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f;
            float requestedBrake = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f;
#if UNITY_EDITOR
            if(verificationInput){requestedThrottle=verificationThrottle;requestedBrake=verificationBrake;}
#endif
            smoothedThrottle = Mathf.MoveTowards(smoothedThrottle, requestedThrottle, Time.deltaTime * (requestedThrottle > smoothedThrottle ? 1.2f : 2.2f));
            smoothedBrake = Mathf.MoveTowards(smoothedBrake, requestedBrake, Time.deltaTime * (requestedBrake > smoothedBrake ? 2.2f : 3.5f));
            float requestedSteering = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) requestedSteering -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) requestedSteering += 1f;
#if UNITY_EDITOR
            if(verificationInput)requestedSteering=verificationSteering;
#endif
            float speedSteeringLimit = Mathf.Lerp(1f, .28f, Mathf.InverseLerp(15f, 120f, vehicle.absoluteSpeed));
            float steeringRate = requestedSteering == 0f ? 1.9f : Mathf.Lerp(1.65f, .78f, Mathf.InverseLerp(0f, 100f, vehicle.absoluteSpeed));
            smoothedSteering = Mathf.MoveTowards(smoothedSteering, requestedSteering * speedSteeringLimit, Time.deltaTime * steeringRate);

            condition ??= vehicle.GetComponent<KombiVehicleCondition>();
            RCCP_Inputs inputs = new()
            {
                throttleInput = (condition != null ? condition.PowerFactor : 1f) * smoothedThrottle * KombiPowertrain.ThrottleFactor(condition!=null&&vehicle.direction>=0?vehicle.absoluteSpeed*105f/condition.MaximumSpeed:vehicle.absoluteSpeed, vehicle.direction<0),
                steerInput = Mathf.Clamp(smoothedSteering * (condition != null ? condition.SteeringFactor : 1f) + (condition != null ? condition.AlignmentPull : 0f),-1f,1f),
                brakeInput = smoothedBrake * (condition != null ? condition.BrakeFactor : 1f),
                handbrakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f
            };
#if UNITY_EDITOR
            if(verificationInput)inputs.handbrakeInput=verificationPark?1f:0f;
#endif
            director??=FindFirstObjectByType<KombiShift2.Gameplay.KombiShiftGameDirector>();
            if(director!=null&&director.Alighting){inputs.throttleInput=0;inputs.brakeInput=1;inputs.handbrakeInput=1;}
            if(inputs.handbrakeInput>.1f)inputs.throttleInput=0f;
            vehicle.Inputs.OverrideInputs(inputs);
        }

        private void OverrideInputs(float throttle, float steering, float brake)
        {
            RCCP_Inputs inputs = new()
            {
                throttleInput = throttle,
                steerInput = steering,
                brakeInput = brake,
                handbrakeInput = 0f
            };
            director??=FindFirstObjectByType<KombiShift2.Gameplay.KombiShiftGameDirector>();
            if(director!=null&&director.Alighting){inputs.throttleInput=0;inputs.brakeInput=1;inputs.handbrakeInput=1;}
            if(inputs.handbrakeInput>.1f)inputs.throttleInput=0f;
            vehicle.Inputs.OverrideInputs(inputs);
        }
    }
}
