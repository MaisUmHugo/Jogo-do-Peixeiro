using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    private enum UIButtonFeedbackKind
    {
        Hover,
        Selection,
        Click
    }

    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer _mainMixer;

    [Header("Sources")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField, InspectorName("SFX Source")] private AudioSource _sfxSource;

    [Header("UI SFX")]
    [SerializeField] private bool _autoAttachUIButtonSfx = true;
    [SerializeField, InspectorName("Button Hover SFX")] private AudioClip _uiButtonHoverSfx;
    [SerializeField, InspectorName("Button Selection SFX")] private AudioClip _uiButtonSelectionSfx;
    [SerializeField, InspectorName("Button Click SFX")] private AudioClip _uiButtonClickSfx;
    [SerializeField, Range(0f, 1f)] private float _uiButtonHoverVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float _uiButtonSelectionVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float _uiButtonClickVolume = 1f;
    [SerializeField, Min(0.05f)] private float _uiButtonScanInterval = 0.25f;
    [SerializeField, Min(0f)] private float _uiButtonPostSceneScanDuration = 2f;
    [SerializeField, Min(0f)] private float _uiDuplicateSuppressTime = 0.06f;

    private const string MasterVolumeKey = "MasterVolume";
    private const string BgmVolumeKey = "BGMVolume";
    private const string SfxVolumeKey = "SFXVolume";

    private const string MasterExposedName = "MasterVolume";
    private const string BgmExposedName = "BGMVolume";
    private const string SfxExposedName = "SFXVolume";

    private float _nextUIButtonScanTime;
    private float _uiButtonScanStopTime;
    private bool _isUIButtonScanBurstActive;
    private GameObject _lastUIButtonFeedbackObject;
    private UIButtonFeedbackKind _lastUIButtonFeedbackKind;
    private float _lastUIButtonFeedbackTime = -10f;
    private float _suppressUIButtonSelectionFeedbackUntil = -10f;

    public bool IsGlobalUIButtonFeedbackEnabled =>
        _autoAttachUIButtonSfx &&
        (_uiButtonHoverSfx != null ||
         _uiButtonSelectionSfx != null ||
         _uiButtonClickSfx != null);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplySavedVolumes();
        BeginUIButtonScanBurst(false);
    }

    private void OnEnable()
    {
        ApplySavedVolumes();

        if (Instance == this)
            SceneManager.sceneLoaded += HandleSceneLoaded;

        BeginUIButtonScanBurst(false);
    }

    private void OnDisable()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        ApplySavedVolumes();
        BeginUIButtonScanBurst(true);
    }

    private void Update()
    {
        if (!_autoAttachUIButtonSfx || !_isUIButtonScanBurstActive)
            return;

        float now = Time.unscaledTime;

        if (now < _nextUIButtonScanTime)
            return;

        RefreshUIButtonAudioFeedback();

        if (now >= _uiButtonScanStopTime)
        {
            _isUIButtonScanBurstActive = false;
            return;
        }

        _nextUIButtonScanTime = now + Mathf.Max(0.05f, _uiButtonScanInterval);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        ApplySavedVolumes();

        if (_bgmSource == null || clip == null)
            return;

        if (_bgmSource.clip == clip)
        {
            _bgmSource.loop = loop;

            if (!_bgmSource.isPlaying)
                _bgmSource.Play();

            return;
        }

        _bgmSource.clip = clip;
        _bgmSource.loop = loop;
        _bgmSource.Play();
    }

    public void StopMusic()
    {
        if (_bgmSource == null)
            return;

        _bgmSource.Stop();
    }

    public void PlaySfx(AudioClip clip, float volume = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f)
    {
        if (_sfxSource == null || clip == null)
            return;

        _sfxSource.pitch = Random.Range(pitchMin, pitchMax);

        _sfxSource.PlayOneShot(clip, volume);

        _sfxSource.pitch = 1f;
    }

    public void PlayUIButtonHover(GameObject source)
    {
        PlayUIButtonFeedback(_uiButtonHoverSfx, _uiButtonHoverVolume, source, UIButtonFeedbackKind.Hover);
    }

    public void PlayUIButtonSelection(GameObject source)
    {
        AudioClip clip = _uiButtonSelectionSfx != null ? _uiButtonSelectionSfx : _uiButtonHoverSfx;
        PlayUIButtonFeedback(clip, _uiButtonSelectionVolume, source, UIButtonFeedbackKind.Selection);
    }

    public void PlayUIButtonClick(GameObject source)
    {
        AudioClip clip = _uiButtonClickSfx != null
            ? _uiButtonClickSfx
            : _uiButtonSelectionSfx != null
                ? _uiButtonSelectionSfx
                : _uiButtonHoverSfx;

        PlayUIButtonFeedback(clip, _uiButtonClickVolume, source, UIButtonFeedbackKind.Click);
    }

    public void SuppressUIButtonSelectionFeedbackFor(float duration)
    {
        float suppressUntil = Time.unscaledTime + Mathf.Max(0f, duration);
        _suppressUIButtonSelectionFeedbackUntil = Mathf.Max(_suppressUIButtonSelectionFeedbackUntil, suppressUntil);
    }

    public void PlaySfxAtPosition(AudioClip clip, Vector3 position, float volume = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f)
    {
        if (clip == null)
            return;

        if (_sfxSource == null)
        {
            AudioSource.PlayClipAtPoint(clip, position, volume);
            return;
        }

        GameObject sfxObject = new GameObject($"SFX_{clip.name}");
        sfxObject.transform.position = position;

        AudioSource audioSource = sfxObject.AddComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = _sfxSource.outputAudioMixerGroup;
        audioSource.spatialBlend = 1f;
        audioSource.volume = volume;
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.clip = clip;
        audioSource.Play();

        Destroy(sfxObject, clip.length / Mathf.Max(0.01f, audioSource.pitch));
    }

    public void ApplySfxOutput(AudioSource audioSource)
    {
        if (audioSource == null || _sfxSource == null)
            return;

        if (audioSource.outputAudioMixerGroup == _sfxSource.outputAudioMixerGroup)
            return;

        audioSource.outputAudioMixerGroup = _sfxSource.outputAudioMixerGroup;
    }

    public void RefreshUIButtonAudioFeedback()
    {
        AttachUIButtonAudioFeedback();
    }

    public void SetMasterVolume(float value)
    {
        SetVolume(MasterExposedName, value);
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        PlayerPrefs.Save();
    }

    public void SetBgmVolume(float value)
    {
        SetVolume(BgmExposedName, value);
        PlayerPrefs.SetFloat(BgmVolumeKey, value);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float value)
    {
        SetVolume(SfxExposedName, value);
        PlayerPrefs.SetFloat(SfxVolumeKey, value);
        PlayerPrefs.Save();
    }

    public float GetMasterVolume()
    {
        return PlayerPrefs.GetFloat(MasterVolumeKey, 0.5f);
    }

    public float GetBgmVolume()
    {
        return PlayerPrefs.GetFloat(BgmVolumeKey, 0.5f);
    }

    public float GetSfxVolume()
    {
        return PlayerPrefs.GetFloat(SfxVolumeKey, 0.5f);
    }

    public void ApplySavedVolumes()
    {
        float master = PlayerPrefs.GetFloat(MasterVolumeKey, 0.5f);
        float bgm = PlayerPrefs.GetFloat(BgmVolumeKey, 0.5f);
        float sfx = PlayerPrefs.GetFloat(SfxVolumeKey, 0.5f);

        ApplyVolumes(master, bgm, sfx);
    }

    private void ApplyVolumes(float master, float bgm, float sfx)
    {
        SetVolume(MasterExposedName, master);
        SetVolume(BgmExposedName, bgm);
        SetVolume(SfxExposedName, sfx);
    }

    private void SetVolume(string exposedName, float value)
    {
        if (_mainMixer == null)
            return;

        value = Mathf.Clamp(value, 0.0001f, 1f);
        float dbValue = Mathf.Log10(value) * 20f;
        _mainMixer.SetFloat(exposedName, dbValue);
    }

    private void HandleSceneLoaded(Scene _scene, LoadSceneMode _mode)
    {
        BeginUIButtonScanBurst(true);
    }

    private void BeginUIButtonScanBurst(bool _scanImmediately)
    {
        if (!_autoAttachUIButtonSfx)
            return;

        float now = Time.unscaledTime;
        _isUIButtonScanBurstActive = true;
        _uiButtonScanStopTime = now + Mathf.Max(0f, _uiButtonPostSceneScanDuration);

        if (_scanImmediately)
        {
            RefreshUIButtonAudioFeedback();
            _nextUIButtonScanTime = now + Mathf.Max(0.05f, _uiButtonScanInterval);
            return;
        }

        _nextUIButtonScanTime = 0f;
    }

    private void AttachUIButtonAudioFeedback()
    {
        if (!_autoAttachUIButtonSfx)
            return;

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null || !button.gameObject.scene.IsValid())
                continue;

            if (button.GetComponent<UIButtonAudioFeedback>() == null)
                button.gameObject.AddComponent<UIButtonAudioFeedback>();
        }
    }

    private void PlayUIButtonFeedback(
        AudioClip clip,
        float volume,
        GameObject source,
        UIButtonFeedbackKind kind)
    {
        if (clip == null || ShouldSuppressUIButtonFeedback(source, kind))
            return;

        PlaySfx(clip, volume, 1f, 1f);
    }

    private bool ShouldSuppressUIButtonFeedback(GameObject source, UIButtonFeedbackKind kind)
    {
        if (source == null)
            return false;

        float now = Time.unscaledTime;

        if (kind == UIButtonFeedbackKind.Selection && now <= _suppressUIButtonSelectionFeedbackUntil)
            return true;

        bool sameSource = _lastUIButtonFeedbackObject == source;
        bool closeToLastFeedback = now - _lastUIButtonFeedbackTime <= _uiDuplicateSuppressTime;
        bool shouldSuppress =
            sameSource &&
            closeToLastFeedback &&
            (kind == _lastUIButtonFeedbackKind ||
             (kind == UIButtonFeedbackKind.Selection && _lastUIButtonFeedbackKind == UIButtonFeedbackKind.Hover));

        if (shouldSuppress)
            return true;

        _lastUIButtonFeedbackObject = source;
        _lastUIButtonFeedbackKind = kind;
        _lastUIButtonFeedbackTime = now;
        return false;
    }
}
