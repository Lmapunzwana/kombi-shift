using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KombiShift2.Gameplay
{
    public enum PassengerState { Seated, Walking, Onboard, Returning, Alighting, Delivered }

    public sealed class StationPassenger : MonoBehaviour
    {
        [SerializeField] private PassengerPersona persona;
        [SerializeField] private Animator visual;
        [SerializeField] private Transform waitingSeat;
        [SerializeField] private float walkingSpeed = 1.65f;
        private static Dictionary<string,int> MuscleIndices;
        private static Dictionary<string,int> BuildMuscleIndices()
        {
            var result = new Dictionary<string,int>();
            var names = HumanTrait.MuscleName;
            for (int i=0;i<names.Length;i++) result[names[i]]=i;
            return result;
        }
        private HumanPoseHandler poseHandler;
        private HumanPose pose;
        private Vector3 standingBody;
        private float phase;
        private float standingHipHeight;
        private bool routeAborted;
        private float seatedBlend = 1f;
        private Transform stationParent;
        private bool sitOnArrival = true;
        private Vector3 walkingScale;
        public Transform WaitingSeat => waitingSeat;
        public void SetWaitingSeat(Transform seat) { waitingSeat=seat; stationParent=seat; }
        public PassengerState State { get; private set; } = PassengerState.Seated;
        public PassengerPersona Persona => persona;
        public bool Boarded => State == PassengerState.Onboard;
        public bool Collected => State == PassengerState.Onboard || State == PassengerState.Alighting || State == PassengerState.Delivered;
        public bool HasHumanRig => visual != null && visual.avatar != null && visual.avatar.isHuman && visual.avatar.isValid;

        public void Configure(PassengerPersona data, Animator model, Transform seat)
        {
            persona = data; visual = model; waitingSeat = seat;
            stationParent = transform.parent;
        }

        private void Awake()
        {
            stationParent = transform.parent;
            walkingScale = transform.localScale;
            InitialiseRig();
        }

        public void InitialiseRig()
        {
            if (poseHandler != null || !HasHumanRig) return;
            if (MuscleIndices == null) MuscleIndices = BuildMuscleIndices();
            visual.enabled = true;
            visual.Rebind(); visual.Update(0f);
            poseHandler = new HumanPoseHandler(visual.avatar, visual.transform);
            poseHandler.GetHumanPose(ref pose);
            standingBody = pose.bodyPosition;
            standingHipHeight = visual.GetBoneTransform(HumanBodyBones.Hips).position.y - transform.position.y;
            visual.enabled = false;
        }

        // Pose the imported humanoid skeleton directly; no character art is replaced.
        public void PoseSeatedPreview()
        {
            InitialiseRig(); seatedBlend = 1f; ApplyPose();
        }

        private void LateUpdate()
        {
            float desired = State == PassengerState.Seated || State == PassengerState.Onboard || (State == PassengerState.Delivered && sitOnArrival) ? 1f : 0f;
            seatedBlend = Mathf.MoveTowards(seatedBlend, desired, Time.deltaTime * 3.5f);
            if (State == PassengerState.Walking || State == PassengerState.Returning || State == PassengerState.Alighting) phase += Time.deltaTime * 7f;
            ApplyPose();
        }

        private void Muscle(string name, float value)
        {
            if (MuscleIndices.TryGetValue(name, out int index)) pose.muscles[index] = value;
        }

        private void ApplyPose()
        {
            if (poseHandler == null) return;
            Array.Clear(pose.muscles, 0, pose.muscles.Length);
            float stride = Mathf.Sin(phase) * (1f - seatedBlend);
            bool moving = State == PassengerState.Walking || State == PassengerState.Returning || State == PassengerState.Alighting;
            if (!moving) stride = 0f;
            Muscle("Left Upper Leg Front-Back", Mathf.Lerp(stride * .42f, .75f, seatedBlend));
            Muscle("Right Upper Leg Front-Back", Mathf.Lerp(-stride * .42f, .75f, seatedBlend));
            Muscle("Left Lower Leg Stretch", Mathf.Lerp(-Mathf.Max(0f, -stride) * .65f, -.85f, seatedBlend));
            Muscle("Right Lower Leg Stretch", Mathf.Lerp(-Mathf.Max(0f, stride) * .65f, -.85f, seatedBlend));
            Muscle("Left Arm Down-Up", -.78f); Muscle("Right Arm Down-Up", -.78f);
            Muscle("Left Arm Front-Back", Mathf.Lerp(-stride * .22f, .18f, seatedBlend));
            Muscle("Right Arm Front-Back", Mathf.Lerp(stride * .22f, .18f, seatedBlend));
            Muscle("Left Forearm Stretch", Mathf.Lerp(0f, -.35f, seatedBlend));
            Muscle("Right Forearm Stretch", Mathf.Lerp(0f, -.35f, seatedBlend));
            pose.bodyPosition = standingBody;
            poseHandler.SetHumanPose(ref pose);
            var hips = visual.GetBoneTransform(HumanBodyBones.Hips);
            // Every rig's pelvis must be centred over its actual seat, including its X/Z offsets.
            // Adjusting only Y left different character rigs hanging outside their seat anchors.
            Vector3 standingHip = hips.position;
            standingHip.y = transform.position.y + standingHipHeight;
            Vector3 seatedHip = transform.position + Vector3.up * (State == PassengerState.Onboard ? .5f * transform.lossyScale.y : .58f);
            hips.position = Vector3.Lerp(standingHip, seatedHip, seatedBlend);
            PoseSeatLeg(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes);
            PoseSeatLeg(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightToes);
            PoseRestingArm(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,-1f);
            PoseRestingArm(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,1f);
        }

        private void PoseRestingArm(HumanBodyBones armBone,HumanBodyBones elbowBone,HumanBodyBones handBone,float side)
        {
            if (seatedBlend <= 0f) return;
            var arm=visual.GetBoneTransform(armBone);var elbow=visual.GetBoneTransform(elbowBone);var hand=visual.GetBoneTransform(handBone);
            Vector3 direction=Vector3.down+transform.forward*.2f;
            arm.rotation=Quaternion.Slerp(arm.rotation,Quaternion.FromToRotation(elbow.position-arm.position,direction)*arm.rotation,seatedBlend);
            Vector3 hip=visual.GetBoneTransform(HumanBodyBones.Hips).position;
            Vector3 rest=hip+transform.right*side*.18f+transform.forward*.28f;
            elbow.rotation=Quaternion.Slerp(elbow.rotation,Quaternion.FromToRotation(hand.position-elbow.position,rest-elbow.position)*elbow.rotation,seatedBlend);
        }

        private void PoseSeatLeg(HumanBodyBones thighBone, HumanBodyBones kneeBone, HumanBodyBones footBone, HumanBodyBones toeBone)
        {
            if (seatedBlend <= 0f) return;
            var thigh = visual.GetBoneTransform(thighBone);var knee = visual.GetBoneTransform(kneeBone);var foot = visual.GetBoneTransform(footBone);
            thigh.rotation = Quaternion.Slerp(thigh.rotation, Quaternion.FromToRotation(knee.position-thigh.position, transform.forward)*thigh.rotation, seatedBlend);
            knee.rotation = Quaternion.Slerp(knee.rotation, Quaternion.FromToRotation(foot.position-knee.position, Vector3.down)*knee.rotation, seatedBlend);
            var toe = visual.GetBoneTransform(toeBone);
            if (toe != null) foot.rotation = Quaternion.Slerp(foot.rotation, Quaternion.FromToRotation(toe.position-foot.position, transform.forward)*foot.rotation, seatedBlend);
        }

        public IEnumerator Board(Transform entry, Transform vehicleSeat, Func<bool> mayContinue)
        {
            State = PassengerState.Walking;
            routeAborted = false;
            yield return new WaitForSeconds(.35f);
            Transform kombi = entry.parent.parent;
            if (kombi.InverseTransformPoint(transform.position).x > -.95f)
            {
                // Approach the left passenger door around the rear, never through the body.
                yield return WalkTo(kombi.TransformPoint(new Vector3(1.4f,.08f,-2.95f)),mayContinue);
                if (routeAborted) yield break;
                yield return WalkTo(kombi.TransformPoint(new Vector3(-1.4f,.08f,-2.95f)),mayContinue);
                if (routeAborted) yield break;
            }
            while (Vector3.Distance(transform.position, entry.position) > .14f)
            {
                if (!mayContinue()) { yield return ReturnToBench(); yield break; }
                Move(entry.position);
                yield return null;
            }
            // Walk through the doorway before taking a seat inside the vehicle.
            float step = 0f;
            Vector3 start = transform.position;
            while (step < 1f)
            {
                if (!mayContinue()) { yield return ReturnToBench(); yield break; }
                step += Time.deltaTime * 1.8f;
                transform.position = Vector3.Lerp(start, vehicleSeat.position, step);
                yield return null;
            }
            transform.SetParent(vehicleSeat, false);
            transform.localPosition = Vector3.zero; transform.localRotation = Quaternion.identity;
            State = PassengerState.Onboard;
        }

        private IEnumerator WalkTo(Vector3 target, Func<bool> mayContinue)
        {
            while (Vector3.Distance(transform.position,target) > .14f)
            {
                if (!mayContinue()) { routeAborted = true; yield return ReturnToBench(); yield break; }
                Move(target); yield return null;
            }
        }

        private IEnumerator ReturnToBench()
        {
            State = PassengerState.Returning;
            while (waitingSeat != null && Vector3.Distance(transform.position, waitingSeat.position) > .12f)
            { Move(waitingSeat.position); yield return null; }
            transform.SetParent(stationParent, true);
            if (waitingSeat != null) transform.SetPositionAndRotation(waitingSeat.position, waitingSeat.rotation);
            State = PassengerState.Seated;
        }

        private void Move(Vector3 target)
        {
            Vector3 delta = target - transform.position; delta.y = 0f;
            if (delta.sqrMagnitude > .001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), Time.deltaTime * 8f);
            transform.position = Vector3.MoveTowards(transform.position, target, walkingSpeed * Time.deltaTime);
        }

        public void LeaveAt(Transform destination, bool seatedAtDestination = true)
        {
            sitOnArrival = seatedAtDestination;
            transform.SetParent(null, true);
            transform.localScale = walkingScale;
            StartCoroutine(Alight(destination));
        }

        private IEnumerator Alight(Transform destination)
        {
            State = PassengerState.Alighting;
            while (destination != null && Vector3.Distance(transform.position, destination.position) > .15f)
            { Move(destination.position); yield return null; }
            if(destination!=null)
            {
                transform.SetParent(destination, false);
                transform.localPosition=Vector3.zero; transform.localRotation=Quaternion.identity;
            }
            State = PassengerState.Delivered;
        }

        private void OnDestroy() { poseHandler?.Dispose(); }
    }
}
