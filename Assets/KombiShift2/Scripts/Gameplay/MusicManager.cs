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
    AudioClip radioOverride;
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
        // Radio stations play one after another.
        if (drivingMusicActive && !inOnboarding && !userPaused && radioOverride != null && !audioSource.isPlaying) NextRadioStation();

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

    // Music always plays while driving; when the kombi is stopped it is turned down, never paused.
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
        // Stopped anywhere, bus stops included: the music keeps playing, just quieter.
        ducked = true;
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

    // ---------- Radio: one button (or N) changes the station; stations play one after another ----------

    enum Style { KombiFm, AfroJazz, Amapiano, Sungura, Mbira, Dancehall }
    struct StationSpec { public string name; public float bpm, root; public int[] chords; public Style style; }
    // Original generated loops (no recorded music is copied). Styles follow the kinds of music heard in kombis:
    // Afro-jazz in the Mtukudzi tradition, amapiano, sungura, mbira/chimurenga and Zimdancehall.
    static readonly StationSpec[] Specs =
    {
        new() { name = "Kombi FM - Afro-jazz",        bpm = 100f, root = 196f, chords = new[] { 0, 5, 7, 5 },  style = Style.AfroJazz },
        new() { name = "Kombi FM - Amapiano",         bpm = 112f, root = 220f, chords = new[] { 0, 9, 5, 7 },  style = Style.Amapiano },
        new() { name = "Kombi FM - Sungura",          bpm = 138f, root = 247f, chords = new[] { 0, 5, 0, 7 },  style = Style.Sungura },
        new() { name = "Kombi FM - Mbira groove",     bpm = 96f,  root = 175f, chords = new[] { 0, 7, 9, 5 },  style = Style.Mbira },
        new() { name = "Kombi FM - Zimdancehall",     bpm = 95f,  root = 208f, chords = new[] { 0, 8, 3, 10 }, style = Style.Dancehall },
        new() { name = "Kombi FM - Township drive",   bpm = 104f, root = 220f, chords = new[] { 0, 7, 9, 5 },  style = Style.KombiFm },
        new() { name = "Kombi FM - Evening rank",     bpm = 120f, root = 196f, chords = new[] { 0, 5, 7, 5 },  style = Style.KombiFm },
    };
    readonly Dictionary<int, AudioClip> stationCache = new();

    public void NextRadioStation()
    {
        if (audioSource == null) return;
        if (radio.Count == 0)
        {
            foreach (var clip in drivingTracks) if (clip != null && !radio.Contains(clip)) radio.Add(clip);
            foreach (var clip in onboardingTracks) if (clip != null && !radio.Contains(clip)) radio.Add(clip);
            for (int i = 0; i < Specs.Length; i++) radio.Add(null);   // generated on first play
        }
        radioIndex = (radioIndex + 1) % radio.Count;
        int generated = radioIndex - (radio.Count - Specs.Length);
        if (radio[radioIndex] == null && generated >= 0)
        {
            if (!stationCache.TryGetValue(generated, out var made)) { made = MakeStation(Specs[generated]); stationCache[generated] = made; }
            radio[radioIndex] = made;
        }
        radioOverride = radio[radioIndex];
        userPaused = false; drivingMusicActive = true;
        audioSource.clip = radioOverride; audioSource.loop = false; audioSource.Play();
        Log("Radio: " + radioOverride.name);
    }

    public string RadioStationName => radioOverride != null ? radioOverride.name : "";

    static float Pluck(float freq, float t, float decay) => (2f * Mathf.Abs(2f * (t * freq - Mathf.Floor(t * freq + .5f))) - 1f) * Mathf.Exp(-t * decay);
    static float Noise(float t) => Mathf.PerlinNoise(t * 3100f, t * 17f) * 2f - 1f;

    static AudioClip MakeStation(StationSpec s)
    {
        const int rate = 22050; float beat = 60f / s.bpm; int bars = 16;
        int length = (int)(rate * beat * 4f * bars);
        var data = new float[length];
        int[] major = { 0, 4, 7, 12 }, minor = { 0, 3, 7, 10 };
        for (int i = 0; i < length; i++)
        {
            float t = i / (float)rate;
            int b = (int)(t / beat); float inBeat = t - b * beat; int bar = b / 4;
            int chord = s.chords[(bar / 2) % s.chords.Length];
            float root = s.root * Mathf.Pow(2f, chord / 12f);
            int[] scale = s.style == Style.Dancehall ? minor : major;
            float sixteenth = beat * .25f; int step16 = (int)(t / sixteenth); float in16 = t - step16 * sixteenth;
            float eighth = beat * .5f; int step8 = (int)(t / eighth); float in8 = t - step8 * eighth;
            float v = 0f;
            switch (s.style)
            {
                case Style.AfroJazz: // walking bass, plucked acoustic-guitar arpeggio, soft shaker
                    v += Mathf.Sin(2f * Mathf.PI * root * .5f * Mathf.Pow(2f, scale[b % 4] / 12f) * t) * Mathf.Exp(-inBeat * 2.5f) * .32f;
                    v += Pluck(root * 2f * Mathf.Pow(2f, scale[step8 % 4] / 12f), in8, 7f) * .2f;
                    v += Noise(t) * Mathf.Exp(-in8 * 40f) * .07f;
                    break;
                case Style.Amapiano: // log drum on the off-beats, piano chord stabs, 16th shakers
                    { float lt = (step8 % 2 == 1) ? in8 : in8 + eighth; float lf = root * .5f * (1f + Mathf.Exp(-lt * 18f) * .6f);
                      v += Mathf.Sin(2f * Mathf.PI * lf * lt) * Mathf.Exp(-lt * 4f) * .45f; }
                    if (b % 2 == 0) for (int n = 0; n < 3; n++) v += Mathf.Sin(2f * Mathf.PI * root * 2f * Mathf.Pow(2f, scale[n] / 12f) * t) * Mathf.Exp(-inBeat * 5f) * .08f;
                    v += Noise(t) * Mathf.Exp(-in16 * 55f) * .09f;
                    v += Mathf.Sin(2f * Mathf.PI * 55f * inBeat) * Mathf.Exp(-inBeat * 14f) * .35f; // kick
                    break;
                case Style.Sungura: // fast bright lead-guitar 16ths, driving bass, snare on 2 and 4
                    v += Pluck(root * 2f * Mathf.Pow(2f, scale[step16 % 4] / 12f), in16, 14f) * .22f;
                    v += Mathf.Sin(2f * Mathf.PI * root * .5f * t) * Mathf.Exp(-in8 * 5f) * .3f;
                    if (b % 2 == 1) v += Noise(t) * Mathf.Exp(-inBeat * 18f) * .18f;
                    break;
                case Style.Mbira: // two interlocking mbira lines and hosho shaker in triplets
                    { float trip = beat / 3f; int st = (int)(t / trip); float inT = t - st * trip;
                      v += Pluck(root * 2f * Mathf.Pow(2f, scale[st % 4] / 12f), inT, 9f) * .18f;
                      v += Pluck(root * 3f * Mathf.Pow(2f, scale[(st + 2) % 4] / 12f), (inT + trip * .5f) % trip, 9f) * .12f;
                      v += Noise(t) * Mathf.Exp(-inT * 45f) * .1f; }
                    v += Mathf.Sin(2f * Mathf.PI * root * .5f * t) * Mathf.Exp(-inBeat * 2f) * .22f;
                    break;
                case Style.Dancehall: // kick on 1 and 3, snare on the off-beat, deep synth bass, staccato chords
                    if (b % 2 == 0) v += Mathf.Sin(2f * Mathf.PI * 50f * inBeat) * Mathf.Exp(-inBeat * 10f) * .45f;
                    if (step8 % 4 == 3) v += Noise(t) * Mathf.Exp(-in8 * 25f) * .2f;
                    v += Mathf.Sin(2f * Mathf.PI * root * .25f * t) * .2f * (Mathf.Exp(-in8 * 3f));
                    if (step8 % 2 == 1) for (int n = 0; n < 3; n++) v += Mathf.Sin(2f * Mathf.PI * root * 2f * Mathf.Pow(2f, scale[n] / 12f) * t) * Mathf.Exp(-in8 * 16f) * .07f;
                    break;
                default: // Kombi FM: bass, arpeggio and hi-hat
                    v += Mathf.Sin(2f * Mathf.PI * root * .5f * t) * Mathf.Exp(-inBeat * 3f) * .35f;
                    v += Mathf.Sin(2f * Mathf.PI * root * 2f * Mathf.Pow(2f, scale[step8 % 4] / 12f) * t) * Mathf.Exp(-in8 * 9f) * .22f;
                    if (step8 % 2 == 1) v += Noise(t) * Mathf.Exp(-in8 * 60f) * .12f;
                    break;
            }
            // Fade in/out so stations change smoothly.
            float edge = Mathf.Clamp01(t * 2f) * Mathf.Clamp01((length / (float)rate - t) * 2f);
            data[i] = Mathf.Clamp(v, -1f, 1f) * .8f * edge;
        }
        var clip = AudioClip.Create(s.name, length, 1, rate, false); clip.SetData(data, 0); return clip;
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
            // Without driving tracks the radio takes over and plays its stations one after another.
            int index = FirstAssignedTrack(drivingTracks, drivingIndex);
            if (index < 0) { NextRadioStation(); return; }
            drivingIndex = index;
            chosen = drivingTracks[index];
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