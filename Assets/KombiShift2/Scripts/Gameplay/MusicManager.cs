using System.Collections.Generic;
using KombiShift2.Gameplay;
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
    PassengerStation[] busStops = new PassengerStation[0];
    RCCP_CarController player;
    float userVolume = -1f;
    bool atBusStop, ducked;
    AudioSource hornSource;
    AudioClip hornClip, radioOverride;
    readonly List<AudioClip> radio = new();
    int radioIndex = -1;
    const float DuckedVolume = .4f;

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

        showDebugOnScreen = false;             // debug status box is not for players
        userVolume = audioSource.volume;
        hornSource = gameObject.AddComponent<AudioSource>(); hornSource.spatialBlend = 0f; hornSource.playOnAwake = false;
        hornClip = MakeHooter();
        if (inOnboarding) PlayOnboarding();
    }

    void Update()
    {
        if (audioSource == null) return;

        float dt = Time.unscaledDeltaTime;
        // Smoothly follow the player's volume, lowered while waiting in traffic or at junctions.
        if (userVolume < 0f) userVolume = audioSource.volume;
        audioSource.volume = Mathf.MoveTowards(audioSource.volume, userVolume * (ducked ? DuckedVolume : 1f), dt * 1.5f);
        if (KeyDown(KeyCode.N)) NextRadioStation();

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
            busStops = FindObjectsByType<PassengerStation>(FindObjectsSortMode.None);
        }

        // Use the fastest ACTIVE car (so a parked showcase model doesn't matter)
        float fastest = 0f;
        int activeCount = 0;
        foreach (RCCP_CarController c in cars)
        {
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            activeCount++;
            if (player == null || !player.gameObject.activeInHierarchy || c.absoluteSpeed > fastest) player = c;
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

    // Music always plays while driving. Stopped at a bus stop: pause and sound the hooter.
    // Stopped anywhere else (junctions, traffic): keep playing at a lower volume.
    void HandleSpeed(float kph, float dt)
    {
        bool moving = kph > movingSpeedKph;
        if (inOnboarding)
        {
            if (!moving) return;
            Log("Kombi is moving, ending onboarding");
            EndOnboarding();
        }
        if (!drivingMusicActive) { drivingMusicActive = true; PlayDriving(); }
        if (moving)
        {
            idleTimer = 0f; ducked = false;
            if (atBusStop) { atBusStop = false; if (!userPaused) audioSource.UnPause(); }
            return;
        }
        idleTimer += dt;
        if (idleTimer < pauseDelay) return;
        if (PlayerAtBusStop())
        {
            if (!atBusStop)
            {
                atBusStop = true; ducked = false; audioSource.Pause();
                if (hornSource != null && hornClip != null) hornSource.PlayOneShot(hornClip, .9f);
                Log("At a bus stop: music paused, hooter sounded");
            }
        }
        else ducked = true;
    }

    bool PlayerAtBusStop()
    {
        if (player == null) return false;
        foreach (var stop in busStops)
        {
            if (stop == null || !stop.isActiveAndEnabled || stop.ServicePoint == null) continue;
            Vector3 d = stop.ServicePoint.position - player.transform.position; d.y = 0f;
            if (d.sqrMagnitude < 14f * 14f) return true;
        }
        return false;
    }

    // ---------- Radio: one button (or N) changes the station ----------

    public void NextRadioStation()
    {
        if (audioSource == null) return;
        if (radio.Count == 0)
        {
            foreach (var clip in drivingTracks) if (clip != null && !radio.Contains(clip)) radio.Add(clip);
            foreach (var clip in onboardingTracks) if (clip != null && !radio.Contains(clip)) radio.Add(clip);
            radio.Add(MakeStation("Kombi FM 1 (generated)", 104f, new[] { 0, 7, 9, 5 }, 220f));
            radio.Add(MakeStation("Kombi FM 2 (generated)", 120f, new[] { 0, 5, 7, 5 }, 196f));
        }
        radioIndex = (radioIndex + 1) % radio.Count;
        radioOverride = radio[radioIndex];
        userPaused = false;
        audioSource.clip = radioOverride; audioSource.loop = true; audioSource.Play();
        if (atBusStop) audioSource.Pause();
        Log("Radio: " + radioOverride.name);
    }

    static AudioClip MakeHooter()
    {
        const int rate = 22050; int length = (int)(rate * .75f);
        var data = new float[length];
        for (int i = 0; i < length; i++)
        {
            float time = i / (float)rate;
            float envelope = Mathf.Clamp01(time * 30f) * Mathf.Clamp01((.75f - time) * 12f);
            // Two-tone vehicle horn (roughly 350 Hz and 440 Hz), slightly squared for a brassy sound.
            float s = Mathf.Sin(2f * Mathf.PI * 350f * time) + .8f * Mathf.Sin(2f * Mathf.PI * 440f * time);
            data[i] = Mathf.Clamp(s * 1.6f, -1f, 1f) * .35f * envelope;
        }
        var clip = AudioClip.Create("Kombi hooter", length, 1, rate, false); clip.SetData(data, 0); return clip;
    }

    // Simple original loop (bass + arpeggio + hi-hat) so the radio always has more than one station.
    static AudioClip MakeStation(string name, float bpm, int[] progression, float root)
    {
        const int rate = 22050; float beat = 60f / bpm; int bars = progression.Length * 2;
        int length = (int)(rate * beat * 4f * bars);
        var data = new float[length];
        int[] major = { 0, 4, 7, 12 };
        for (int i = 0; i < length; i++)
        {
            float time = i / (float)rate; int beatIndex = (int)(time / beat); int bar = (beatIndex / 4) / 2;
            float chordRoot = root * Mathf.Pow(2f, progression[bar % progression.Length] / 12f);
            float inBeat = time - beatIndex * beat;
            float bass = Mathf.Sin(2f * Mathf.PI * chordRoot * .5f * time) * Mathf.Exp(-inBeat * 3f) * .35f;
            float eighth = beat * .5f; int step = (int)(time / eighth); float inStep = time - step * eighth;
            float note = chordRoot * 2f * Mathf.Pow(2f, major[step % 4] / 12f);
            float arp = Mathf.Sin(2f * Mathf.PI * note * time) * Mathf.Exp(-inStep * 9f) * .22f;
            float hat = (step % 2 == 1 ? 1f : 0f) * Mathf.Exp(-inStep * 60f) * (Mathf.PerlinNoise(time * 3000f, 0f) - .5f) * .25f;
            data[i] = Mathf.Clamp(bass + arp + hat, -1f, 1f) * .8f;
        }
        var clip = AudioClip.Create(name, length, 1, rate, false); clip.SetData(data, 0); return clip;
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
        if (audioSource != null) userVolume = Mathf.Clamp01(value);
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
        if (GUI.Button(new Rect(bx - 96f, by, 92f, bh), "Radio (N)")) NextRadioStation();

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
        float v = GUILayout.HorizontalSlider(userVolume, 0f, 1f);
        if (!Mathf.Approximately(v, userVolume)) userVolume = v;
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

    // Returns the first slot from 'start' onwards (wrapping) that has a clip, or -1 if none do.
    // Lets the game run when a track file has been removed but its Inspector slot remains.
    static int FirstAssignedTrack(AudioClip[] list, int start)
    {
        if (list == null || list.Length == 0) return -1;
        for (int i = 0; i < list.Length; i++)
        {
            int index = (start + i) % list.Length;
            if (list[index] != null) return index;
        }
        return -1;
    }

    static bool KeyDown(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        if (k != null && key == KeyCode.N && k.nKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(key)) return true;
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
        int index = FirstAssignedTrack(onboardingTracks, onboardingIndex);
        if (index < 0)
        {
            Debug.LogWarning("MusicManager: Onboarding Tracks list has no assigned clips.");
            return;
        }
        onboardingIndex = index;
        audioSource.clip = onboardingTracks[onboardingIndex];
        audioSource.loop = true;
        audioSource.Play();
        Log("Playing onboarding track: " + audioSource.clip.name);
    }

    void PlayDriving()
    {
        if (userPaused) return;
        AudioClip chosen = radioOverride;
        if (chosen == null)
        {
            // Without driving tracks, keep the onboarding music going so there is always music.
            var list = FirstAssignedTrack(drivingTracks, 0) >= 0 ? drivingTracks : onboardingTracks;
            int index = FirstAssignedTrack(list, list == drivingTracks ? drivingIndex : onboardingIndex);
            if (index < 0) { Debug.LogWarning("MusicManager: no music clips assigned."); return; }
            if (list == drivingTracks) drivingIndex = index;
            chosen = list[index];
        }

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