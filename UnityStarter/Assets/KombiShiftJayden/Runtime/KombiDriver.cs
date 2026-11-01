using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace KombiShift.JaydenStarter
{
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class KombiDriver : MonoBehaviour
    {
        public ShiftGame game;
        public float acceleration = 10f, maximumSpeed = 19f, steering = 80f;
        public float Speed => Body == null ? 0 : new Vector2(Body.linearVelocity.x, Body.linearVelocity.z).magnitude;
        public Rigidbody Body { get; private set; }
        float throttle, turn; bool brake;
        void Awake()
        {
            Body = GetComponent<Rigidbody>(); Body.mass = 1400;
            Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.centerOfMass = new Vector3(0, -.3f, 0);
        }
        void Update()
        {
            throttle = turn = 0; brake = false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null) {
                throttle = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0);
                turn = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
                brake = k.spaceKey.isPressed;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            throttle = Input.GetAxisRaw("Vertical"); turn = Input.GetAxisRaw("Horizontal"); brake = Input.GetKey(KeyCode.Space);
#endif
        }
        void FixedUpdate()
        {
            if (game == null || !game.IsPlaying) return;
            Vector3 local = transform.InverseTransformDirection(Body.linearVelocity);
            bool grounded = Physics.Raycast(transform.position, Vector3.down, 1.4f, ~0, QueryTriggerInteraction.Ignore);
            if (!grounded) return;
            float grip = game.Rainy ? 2.6f : 7f;
            local.x = Mathf.MoveTowards(local.x, 0, grip * Time.fixedDeltaTime);
            local.z += throttle * acceleration * Mathf.Lerp(.45f, 1, game.Condition / 100) * Time.fixedDeltaTime;
            local.z = Mathf.MoveTowards(local.z, 0, (brake ? 24 : 1.2f) * Time.fixedDeltaTime);
            local.z = Mathf.Clamp(local.z, -6, maximumSpeed);
            Body.linearVelocity = transform.TransformDirection(local);
            float angle = turn * steering * Mathf.Clamp(local.z / 6, -1, 1) * Time.fixedDeltaTime;
            Body.MoveRotation(Body.rotation * Quaternion.Euler(0, angle, 0));
        }
        float nextCollision;
        void OnCollisionEnter(Collision hit)
        {
            if (game == null || !game.IsPlaying || Time.time < nextCollision || hit.relativeVelocity.magnitude < 3) return;
            if (hit.GetContact(0).normal.y > .6f) return;
            nextCollision = Time.time + 1; game.Damage(hit.relativeVelocity.magnitude * 1.2f);
        }
    }
}
