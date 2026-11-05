using UnityEngine;

namespace KombiShift2.Roadworks
{
    [RequireComponent(typeof(Collider))]
    public sealed class PotholeHazard : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float impactVelocity = 1.5f;
        [SerializeField, Min(0f)] private float forwardSpeedLoss = 1.2f;
        [SerializeField, Min(0f)] private float retriggerDelay = 0.4f;
        [SerializeField] private AudioSource impactAudio;
        [SerializeField] private ParticleSystem debris;
        private float nextImpactTime;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time < nextImpactTime || other.attachedRigidbody == null) return;
            Rigidbody body = other.attachedRigidbody;
            nextImpactTime = Time.time + retriggerDelay;

            if (!body.isKinematic)
            {
                body.AddForceAtPosition(Vector3.up * impactVelocity, other.ClosestPoint(transform.position), ForceMode.VelocityChange);
                Vector3 local = body.transform.InverseTransformDirection(body.linearVelocity);
                local.z = Mathf.MoveTowards(local.z, 0f, forwardSpeedLoss);
                body.linearVelocity = body.transform.TransformDirection(local);
            }

            if (impactAudio != null) impactAudio.Play();
            if (debris != null) debris.Play();
        }
    }
}
