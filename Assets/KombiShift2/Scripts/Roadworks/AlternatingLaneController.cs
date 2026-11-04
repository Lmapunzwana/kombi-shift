using UnityEngine;

namespace KombiShift2.Roadworks
{
    public sealed class AlternatingLaneController : MonoBehaviour
    {
        [System.Serializable]
        private struct WorkStage
        {
            public string description;
            public GameObject leftLaneClosure;
            public GameObject rightLaneClosure;
            public GameObject temporaryLane;
        }

        [SerializeField] private WorkStage[] stages;
        [SerializeField, Min(5f)] private float stageDuration = 120f;
        [SerializeField] private bool cycleAutomatically;
        [SerializeField] private int currentStage;
        private float elapsed;

        private void Start() => ApplyStage();

        private void Update()
        {
            if (!cycleAutomatically || stages.Length < 2) return;
            elapsed += Time.deltaTime;
            if (elapsed < stageDuration) return;
            elapsed = 0f;
            SetStage((currentStage + 1) % stages.Length);
        }

        public void SetStage(int stage)
        {
            if (stages.Length == 0) return;
            currentStage = Mathf.Clamp(stage, 0, stages.Length - 1);
            ApplyStage();
        }

        private void ApplyStage()
        {
            for (int i = 0; i < stages.Length; i++)
            {
                bool active = i == currentStage;
                SetActive(stages[i].leftLaneClosure, active);
                SetActive(stages[i].rightLaneClosure, active);
                SetActive(stages[i].temporaryLane, active);
            }
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target) target.SetActive(value);
        }
    }
}
