using UnityEngine;

namespace KombiShift2.Roadworks
{
    [RequireComponent(typeof(Collider))]
    public sealed class RoadSurfaceZone : MonoBehaviour
    {
        public enum Surface { Asphalt, Gravel, Dust, Potholes, Mud }

        [SerializeField] private Surface surface = Surface.Dust;
        [SerializeField, Range(0f, 2f)] private float dragMultiplier = 0.35f;
        [SerializeField, Range(0f, 2f)] private float vibrationStrength = 0.18f;
        [SerializeField] private ParticleSystem dustParticles;
        [SerializeField] private AudioSource surfaceAudio;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerStay(Collider other)
        {
            if (!other.attachedRigidbody) return;
            Rigidbody body = other.attachedRigidbody;
            Vector3 planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            body.AddForce(-planarVelocity * dragMultiplier, ForceMode.Acceleration);

            if (surface is Surface.Potholes or Surface.Gravel)
            {
                Vector3 shake = Random.insideUnitSphere * vibrationStrength;
                body.AddForceAtPosition(shake, other.ClosestPoint(transform.position), ForceMode.VelocityChange);
            }

            if (dustParticles && !dustParticles.isPlaying) dustParticles.Play();
            if (surfaceAudio && !surfaceAudio.isPlaying) surfaceAudio.Play();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.attachedRigidbody) return;
            if (dustParticles) dustParticles.Stop();
            if (surfaceAudio) surfaceAudio.Stop();
        }
    }
}
