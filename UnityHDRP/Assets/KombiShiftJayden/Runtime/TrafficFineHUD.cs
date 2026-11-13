using UnityEngine;

namespace KombiShift.JaydenStarter
{
    public sealed class TrafficFineHUD : MonoBehaviour
    {
        ShiftGame game;
        GUIStyle heading;
        GUIStyle detail;

        void Awake()
        {
            game = GetComponent<ShiftGame>();
            heading = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            detail = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            heading.normal.textColor = Color.white;
            detail.normal.textColor = new Color(.78f, .82f, .82f);
        }

        void OnGUI()
        {
            if (game == null || !game.IsPlaying) return;

            float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3((Screen.width - 1600f * scale) / 2f, (Screen.height - 900f * scale) / 2f, 0f),
                Quaternion.identity, Vector3.one * scale);

            GUI.Box(new Rect(850, 153, 195, 74), GUIContent.none);
            GUI.Label(new Rect(861, 158, 180, 20),
                string.Format("TRAFFIC FINES  {0}", game.TrafficFineCount), heading);
            GUI.Label(new Rect(861, 179, 180, 20),
                string.Format("TOTAL  ${0:F2}", game.TrafficFinesTotal), detail);
            GUI.Label(new Rect(861, 198, 175, 25),
                game.LatestTrafficFine, detail);

            GUI.matrix = oldMatrix;
        }
    }
}
