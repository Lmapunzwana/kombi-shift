using UnityEngine;
using UnityEngine.SceneManagement;

namespace KombiShift2.UI
{
    public sealed class KombiMainMenu : MonoBehaviour
    {
        private const float W = 1600f;
        private const float H = 900f;
        private Texture2D hero, panelTexture, orangeTexture, greenTexture;
        private GUIStyle logo, kicker, heading, body, button, profile;
        private bool settingsOpen;
        private float volume;

        private void Awake()
        {
            Time.timeScale = 1f;
            hero = Resources.Load<Texture2D>("Menu/kombi-shift-hero");
            volume = PlayerPrefs.GetFloat("KombiShift.MasterVolume", .8f);
            AudioListener.volume = volume;
            panelTexture = MakeTexture(new Color(.025f, .035f, .035f, .92f));
            orangeTexture = MakeTexture(new Color(1f, .43f, .035f));
            greenTexture = MakeTexture(new Color(.12f, .58f, .20f));
        }

        private void OnDestroy()
        {
            Destroy(panelTexture); Destroy(orangeTexture); Destroy(greenTexture);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) && !settingsOpen) StartGame();
            if (Input.GetKeyDown(KeyCode.Escape) && settingsOpen) settingsOpen = false;
        }

        private void OnGUI()
        {
            EnsureStyles();
            float scale = Mathf.Min(Screen.width / W, Screen.height / H);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - W * scale) * .5f, (Screen.height - H * scale) * .5f), Quaternion.identity, Vector3.one * scale);
            DrawCover(hero, new Rect(0, 0, W, H));
            GUI.DrawTexture(new Rect(0, 0, 540, H), panelTexture);
            DrawGradientShade();
            GUI.Label(new Rect(70, 54, 440, 25), "A HARARE COMMUTER STORY", kicker);
            GUI.Label(new Rect(65, 88, 470, 105), "KOMBI", logo);
            GUI.Label(new Rect(65, 172, 470, 105), "SHIFT", logo);
            GUI.Label(new Rect(70, 284, 390, 58), "Own the route. Beat the clock.\nBring everyone home.", body);
            DrawProfile();
            if (settingsOpen) DrawSettings(); else DrawActions();
            GUI.Label(new Rect(70, 840, 430, 28), "WASD / ARROWS   •   CONTROLLER   •   v0.3", kicker);
        }

        private void DrawActions()
        {
            GUI.Label(new Rect(70, 390, 400, 30), "NEXT SHIFT", kicker);
            GUI.Label(new Rect(70, 422, 400, 44), "CBD → MBARE", heading);
            if (Action(new Rect(70, 500, 390, 68), "START SHIFT", orangeTexture)) StartGame();
            if (Action(new Rect(70, 584, 390, 58), "SETTINGS", greenTexture)) settingsOpen = true;
            GUI.Label(new Rect(70, 674, 390, 68), "Drive the kombi yourself. No autopilot.\nEnter starts • Escape returns", body);
        }

        private void DrawSettings()
        {
            GUI.Label(new Rect(70, 390, 390, 38), "SETTINGS", heading);
            GUI.Label(new Rect(70, 457, 390, 28), $"MASTER VOLUME     {Mathf.RoundToInt(volume * 100)}%", kicker);
            volume = GUI.HorizontalSlider(new Rect(70, 505, 390, 24), volume, 0f, 1f);
            AudioListener.volume = volume;
            if (Action(new Rect(70, 574, 185, 58), "SAVE", orangeTexture))
            {
                PlayerPrefs.SetFloat("KombiShift.MasterVolume", volume); PlayerPrefs.Save(); settingsOpen = false;
            }
            if (Action(new Rect(275, 574, 185, 58), "BACK", greenTexture)) settingsOpen = false;
        }

        private void DrawProfile()
        {
            GUI.DrawTexture(new Rect(1230, 42, 300, 62), panelTexture);
            GUI.DrawTexture(new Rect(1230, 42, 7, 62), orangeTexture);
            GUI.Label(new Rect(1255, 50, 250, 24), "DRIVER 01", profile);
            GUI.Label(new Rect(1255, 76, 250, 20), "ROOKIE  •  $0 EARNED", kicker);
        }

        private bool Action(Rect rect, string label, Texture2D texture)
        {
            GUIStyle style = new(button);
            style.normal.background = style.hover.background = style.active.background = texture;
            return GUI.Button(rect, label + "   ›", style);
        }

        private void DrawGradientShade()
        {
            for (int i = 0; i < 12; i++)
            {
                Color old = GUI.color;
                GUI.color = new Color(0, 0, 0, Mathf.Lerp(.42f, 0, i / 11f));
                GUI.DrawTexture(new Rect(540 + i * 50, 0, 52, H), Texture2D.whiteTexture);
                GUI.color = old;
            }
        }

        private static void DrawCover(Texture texture, Rect destination)
        {
            if (texture == null) return;
            float sourceAspect = (float)texture.width / texture.height;
            float destinationAspect = destination.width / destination.height;
            Rect uv = new(0, 0, 1, 1);
            if (sourceAspect > destinationAspect) { uv.width = destinationAspect / sourceAspect; uv.x = (1 - uv.width) * .5f; }
            else { uv.height = sourceAspect / destinationAspect; uv.y = (1 - uv.height) * .5f; }
            GUI.DrawTextureWithTexCoords(destination, texture, uv);
        }

        private void EnsureStyles()
        {
            if (logo != null) return;
            logo = new GUIStyle(GUI.skin.label) { fontSize = 92, fontStyle = FontStyle.Bold }; logo.normal.textColor = Color.white;
            kicker = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold }; kicker.normal.textColor = new Color(.76f, .82f, .75f);
            heading = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold }; heading.normal.textColor = Color.white;
            body = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true }; body.normal.textColor = new Color(.88f, .9f, .86f);
            profile = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold }; profile.normal.textColor = Color.white;
            button = new GUIStyle(GUI.skin.button) { fontSize = 23, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(28, 20, 0, 0) };
            button.normal.textColor = button.hover.textColor = button.active.textColor = Color.white;
        }

        private static Texture2D MakeTexture(Color colour)
        {
            Texture2D texture = new(1, 1); texture.SetPixel(0, 0, colour); texture.Apply(); return texture;
        }

        private static void StartGame() => SceneManager.LoadScene("KombiDrive");
    }
}
