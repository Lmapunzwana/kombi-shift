using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MusicManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] onboardingTracks;
    public AudioClip[] drivingTracks;

    [Header("Settings")]
    [Tooltip("Tick if onboarding music plays at the start of this scene.")]
    public bool startInOnboarding = true;
    [Tooltip("Speed in km/h above which the kombi counts as moving.")]
    public float movingSpeedKph = 3f;
    [Tooltip("Seconds the kombi must be stopped before the music pauses.")]
    public float pauseDelay = 2f;
    public bool showDebugMessages = true;

    [Header("On-screen controls")]
    public bool showControls = true;
    [Tooltip("Small status box at the top-left. Untick when everything works.")]
    public bool showDebugOnScreen = true;

    static MusicManager instance;

    int onboardingIndex = 0;
    int drivingIndex = 0;
    bool inOnboarding;
    bool drivingMusicActive = false;
    bool userPaused = false;
    bool panelOpen = false;
    float idleTimer = 0f;
    float holdUntil = 0f;
    float searchTimer = 0f;
    float statusTimer = 0f;
    string statusText = "starting...";
    RCCP_CarController[] cars = new RCCP_CarController[0];

    void Awake()
    {
        // Keep one MusicManager alive across scene loads
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        transform.SetParent(null);          // must be a root object
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (instance != this) return;       // a duplicate is being destroyed

        inOnboarding = startInOnboarding;
        Log("MusicManager started. Onboarding mode: " + inOnboarding);

        if (audioSource == null)
        {
            Debug.LogError("MusicManager: Audio Source slot is EMPTY. Assign it in the Inspector.");
            return;
        }

        if (inOnboarding) PlayOnboarding();
    }

    void Update()
    {
        if (audioSource == null) return;

        float dt = Time.unscaledDeltaTime;

        // Test key: press M to force the driving music (delete when finished testing)
        if (MKeyDown())
        {
            Log("M pressed: forcing driving music");
            if (inOnboarding) EndOnboarding();
            drivingMusicActive = true;
            idleTimer = 0f;
            PlayDriving();
        }

        // Refresh the list of cars every second, including inactive ones
        searchTimer -= dt;
        if (searchTimer <= 0f)
        {
            searchTimer = 1f;
            cars = FindObjectsByType<RCCP_CarController>(FindObjectsInactive.Include);
        }

        // Use the fastest ACTIVE car (so a parked showcase model doesn't matter)
        float fastest = 0f;
        int activeCount = 0;
        foreach (RCCP_CarController c in cars)
        {
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            activeCount++;
            if (c.absoluteSpeed > fastest) fastest = c.absoluteSpeed;
        }

        statusText = "cars: " + cars.Length + " | active: " + activeCount +
                     " | speed: " + fastest.ToString("F1") + " km/h | " +
                     (inOnboarding ? "onboarding" : "driving") +
                     " | timeScale " + Time.timeScale.ToString("F1");

        // Status line in the Console every 2 seconds
        if (showDebugMessages)
        {
            statusTimer -= dt;
            if (statusTimer <= 0f)
            {
                statusTimer = 2f;
                Log("Status | " + statusText);
            }
        }

        if (activeCount > 0) HandleSpeed(fastest, dt);
    }

    void HandleSpeed(float kph, float dt)
    {
        bool moving = kph > movingSpeedKph;

        if (inOnboarding)
        {
            if (!moving) return;
            Log("Kombi is moving, ending onboarding");
            EndOnboarding();
        }

        if (moving)
        {
            idleTimer = 0f;
            if (!drivingMusicActive)
            {
                drivingMusicActive = true;
                PlayDriving();
            }
        }
        else if (drivingMusicActive)
        {
            idleTimer += dt;
            // holdUntil keeps a track the player just picked playing for a few seconds
            if (idleTimer >= pauseDelay && Time.unscaledTime >= holdUntil)
            {
                drivingMusicActive = false;
                audioSource.Pause();
                Log("Kombi stopped, music paused");
            }
        }
    }

    // ---------- Track selection (public, so UI Button On Click events can use them) ----------

    public void NextOnboardingTrack()     { ChangeOnboardingTrack(+1); }
    public void PreviousOnboardingTrack() { ChangeOnboardingTrack(-1); }
    public void NextDrivingTrack()        { ChangeDrivingTrack(+1); }
    public void PreviousDrivingTrack()    { ChangeDrivingTrack(-1); }

    // Next / previous in whichever set is currently active
    public void NextTrack()     { if (inOnboarding) NextOnboardingTrack(); else NextDrivingTrack(); }
    public void PreviousTrack() { if (inOnboarding) PreviousOnboardingTrack(); else PreviousDrivingTrack(); }

    void ChangeOnboardingTrack(int direction)
    {
        if (audioSource == null || onboardingTracks.Length == 0) return;
        onboardingIndex = (onboardingIndex + direction + onboardingTracks.Length) % onboardingTracks.Length;

        // Plays now if onboarding is active, otherwise it is just remembered
        if (inOnboarding)
        {
            userPaused = false;
            PlayOnboarding();
        }
    }

    void ChangeDrivingTrack(int direction)
    {
        if (audioSource == null || drivingTracks.Length == 0) return;
        drivingIndex = (drivingIndex + direction + drivingTracks.Length) % drivingTracks.Length;

        // Plays now if we're in the driving part, otherwise it is just remembered
        if (!inOnboarding)
        {
            userPaused = false;
            drivingMusicActive = true;
            idleTimer = 0f;
            holdUntil = Time.unscaledTime + 8f;   // keep it playing a few seconds even if stopped
            audioSource.clip = null;              // forces the new track to start from the beginning
            PlayDriving();
        }
    }

    // Pause / Play button
    public void ToggleMusic()
    {
        if (audioSource == null) return;
        userPaused = !userPaused;

        if (userPaused)
        {
            audioSource.Pause();
            Log("Music paused by player");
        }
        else
        {
            Log("Music resumed by player");
            if (inOnboarding)
            {
                if (audioSource.clip == null) PlayOnboarding();
                else audioSource.UnPause();
            }
            else if (drivingMusicActive) PlayDriving();
        }
    }

    public void SetVolume(float value)
    {
        if (audioSource != null) audioSource.volume = Mathf.Clamp01(value);
    }

    // Open or close the music panel (can also be called from your own Settings button)
    public void TogglePanel() { panelOpen = !panelOpen; }

    string TrackName(AudioClip[] list, int index)
    {
        if (list == null || list.Length == 0 || list[index] == null) return "(no tracks)";
        string n = list[index].name;
        return n.Length > 20 ? n.Substring(0, 17) + "..." : n;
    }

    // ---------- On-screen display ----------

    void OnGUI()
    {
        if (audioSource == null) return;

        float s = Mathf.Clamp(Screen.height / 1000f, 0.75f, 1.5f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));

        // Status box (top-left) so you can see what the script detects
        if (showDebugOnScreen)
            GUI.Box(new Rect(8f, 8f, 420f, 24f), "[Music] " + statusText);

        if (!showControls) return;

        float sw = Screen.width / s;
        float bw = 72f, bh = 24f;
        float bx = sw - bw - 8f, by = 8f;

        if (GUI.Button(new Rect(bx, by, bw, bh), panelOpen ? "Close" : "Music"))
            panelOpen = !panelOpen;

        if (!panelOpen) return;

        float pw = 260f;
        float px = sw - pw - 8f;
        float py = by + bh + 4f;

        GUILayout.BeginArea(new Rect(px, py, pw, 150f), GUI.skin.box);

        // Onboarding set
        GUILayout.Label((inOnboarding ? "> " : "") + "Onboarding music");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<<", GUILayout.Width(36f))) PreviousOnboardingTrack();
        GUILayout.Label(TrackName(onboardingTracks, onboardingIndex));
        if (GUILayout.Button(">>", GUILayout.Width(36f))) NextOnboardingTrack();
        GUILayout.EndHorizontal();

        // Driving set
        GUILayout.Label((!inOnboarding ? "> " : "") + "Driving music");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<<", GUILayout.Width(36f))) PreviousDrivingTrack();
        GUILayout.Label(TrackName(drivingTracks, drivingIndex));
        if (GUILayout.Button(">>", GUILayout.Width(36f))) NextDrivingTrack();
        GUILayout.EndHorizontal();

        // Pause + volume
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(userPaused ? "Play" : "Pause", GUILayout.Width(60f))) ToggleMusic();
        GUILayout.Label("Vol", GUILayout.Width(28f));
        float v = GUILayout.HorizontalSlider(audioSource.volume, 0f, 1f);
        if (!Mathf.Approximately(v, audioSource.volume)) audioSource.volume = v;
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    // ---------- Kept so the bootstrap script still compiles ----------

    public void ReportSpeed(float kph) { }
    public void UnlockDriving() { }

    // ---------- Internals ----------

    bool MKeyDown()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k != null && k.mKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.M)) return true;
#endif
        return false;
    }

    // Also callable from a button if onboarding ends some other way
    public void EndOnboarding()
    {
        if (!inOnboarding) return;
        Log("EndOnboarding called");
        inOnboarding = false;
        drivingMusicActive = false;
        audioSource.Stop();
        audioSource.clip = null;
    }

    void PlayOnboarding()
    {
        if (userPaused) return;
        if (onboardingTracks.Length == 0)
        {
            Debug.LogWarning("MusicManager: Onboarding Tracks list is empty.");
            return;
        }
        audioSource.clip = onboardingTracks[onboardingIndex];
        audioSource.loop = true;
        audioSource.Play();
        Log("Playing onboarding track: " + audioSource.clip.name);
    }

    void PlayDriving()
    {
        if (userPaused) return;
        if (drivingTracks.Length == 0)
        {
            Debug.LogWarning("MusicManager: Driving Tracks list is empty.");
            return;
        }

        AudioClip chosen = drivingTracks[drivingIndex];

        if (audioSource.clip == chosen) audioSource.UnPause();
        else
        {
            audioSource.clip = chosen;
            audioSource.loop = true;
            audioSource.Play();
        }
        Log("Driving track: " + chosen.name);
    }

    // Hook these up to UI buttons if you want one button per track (type the track number)
    public void SelectOnboardingTrack(int index)
    {
        if (index < 0 || index >= onboardingTracks.Length) return;
        onboardingIndex = index;
        if (inOnboarding) PlayOnboarding();
    }

    public void SelectDrivingTrack(int index)
    {
        if (index < 0 || index >= drivingTracks.Length) return;
        drivingIndex = index;
        if (!inOnboarding && drivingMusicActive) PlayDriving();
    }

    void Log(string message)
    {
        if (showDebugMessages) Debug.Log("[Music] " + message);
    }
}