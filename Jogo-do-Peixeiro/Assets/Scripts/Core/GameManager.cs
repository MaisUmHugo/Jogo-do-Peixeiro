using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private const string AmbientLoopAudioSourceName = "AmbientLoopAudioSource";
    private const string LavaLoopAudioSourceName = "LavaLoopAudioSource";
    private const string VolcanoLoopAudioSourceName = "VolcanoLoopAudioSource";

    public static GameManager instance;

    [Header("Music")]
    [SerializeField] private AudioClip gameMusic;
    [SerializeField] private bool playGameMusicOnGameplayScenes = true;

    [Header("Scene Loop SFX")]
    [SerializeField, InspectorName("Ambient Loop SFX")] private AudioClip ambientLoopSfx;
    [SerializeField, InspectorName("Lava Loop SFX")] private AudioClip lavaLoopSfx;
    [SerializeField, InspectorName("Volcano Loop SFX")] private AudioClip volcanoLoopSfx;
    [SerializeField, InspectorName("Play Scene Loop SFX On Gameplay Scenes")] private bool playSceneLoopSfxOnGameplayScenes = true;
    [SerializeField, Range(0f, 1f), InspectorName("Ambient Loop SFX Volume")] private float ambientLoopSfxVolume = 0.35f;
    [SerializeField, Range(0f, 1f), InspectorName("Lava Loop SFX Volume")] private float lavaLoopSfxVolume = 0.45f;
    [SerializeField, Range(0f, 1f), InspectorName("Volcano Loop SFX Volume")] private float volcanoLoopSfxVolume = 0.45f;
    [SerializeField] private AudioSource ambientLoopSource;
    [SerializeField] private AudioSource lavaLoopSource;
    [SerializeField] private AudioSource volcanoLoopSource;

    public enum GameState
    {
        OnFoot,
        OnBoat,
        Fishing,
        InUI,
        Paused
    }

    public GameState currentState;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SetupSceneLoopSfxSources();
    }

    private void Start()
    {
        PlayGameMusicForCurrentScene();
        UpdateSceneLoopSfxForCurrentScene();
    }

    public void SetState(GameState _newState)
    {
        currentState = _newState;
        UpdateCursor();
    }

    private void UpdateCursor()
    {
        switch (currentState)
        {
            case GameState.OnFoot:
            case GameState.OnBoat:
            case GameState.Fishing:

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;

            case GameState.InUI:
            case GameState.Paused:

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }

    public bool IsGameplayBlocked()
    {
        return currentState == GameState.InUI || currentState == GameState.Paused;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Main Menu")
        {
            SetState(GameState.InUI); // libera o cursor no menu
            StopSceneLoopSfx();
        }
        else
        {
            SetState(GameState.OnFoot); // gameplay normal
            PlayGameMusic();
            PlaySceneLoopSfx();
        }
    }

    private void PlayGameMusicForCurrentScene()
    {
        if (SceneManager.GetActiveScene().name == "Main Menu")
            return;

        PlayGameMusic();
    }

    private void PlayGameMusic()
    {
        if (!playGameMusicOnGameplayScenes || AudioManager.Instance == null || gameMusic == null)
            return;

        AudioManager.Instance.PlayMusic(gameMusic);
    }

    private void UpdateSceneLoopSfxForCurrentScene()
    {
        if (SceneManager.GetActiveScene().name == "Main Menu")
        {
            StopSceneLoopSfx();
            return;
        }

        PlaySceneLoopSfx();
    }

    private void PlaySceneLoopSfx()
    {
        if (!playSceneLoopSfxOnGameplayScenes)
        {
            StopSceneLoopSfx();
            return;
        }

        SetupSceneLoopSfxSources();
        PlayLoopSource(ambientLoopSource, ambientLoopSfx, ambientLoopSfxVolume);
        PlayLoopSource(lavaLoopSource, lavaLoopSfx, lavaLoopSfxVolume);
        PlayLoopSource(volcanoLoopSource, volcanoLoopSfx, volcanoLoopSfxVolume);
    }

    private void StopSceneLoopSfx()
    {
        StopLoopSource(ambientLoopSource);
        StopLoopSource(lavaLoopSource);
        StopLoopSource(volcanoLoopSource);
    }

    private void SetupSceneLoopSfxSources()
    {
        ambientLoopSource = EnsureLoopSource(ambientLoopSource, AmbientLoopAudioSourceName);
        lavaLoopSource = EnsureLoopSource(lavaLoopSource, LavaLoopAudioSourceName);
        volcanoLoopSource = EnsureLoopSource(volcanoLoopSource, VolcanoLoopAudioSourceName);
    }

    private AudioSource EnsureLoopSource(AudioSource source, string sourceName)
    {
        if (source == null)
        {
            Transform sourceTransform = transform.Find(sourceName);

            if (sourceTransform != null)
                source = sourceTransform.GetComponent<AudioSource>();
        }

        if (source == null)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            source = sourceObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        AudioManager.Instance?.ApplySfxOutput(source);
        return source;
    }

    private void PlayLoopSource(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null)
            return;

        AudioManager.Instance?.ApplySfxOutput(source);

        if (clip == null)
        {
            StopLoopSource(source);
            return;
        }

        if (source.clip != clip)
            source.clip = clip;

        source.loop = true;
        source.volume = Mathf.Clamp01(volume);

        if (!source.isPlaying)
            source.Play();
    }

    private void StopLoopSource(AudioSource source)
    {
        if (source == null)
            return;

        if (source.isPlaying)
            source.Stop();

        source.clip = null;
    }
}
