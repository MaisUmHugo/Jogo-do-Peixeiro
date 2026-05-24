using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer _mainMixer;

    [Header("Sources")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField, InspectorName("SFX Source")] private AudioSource _sfxSource;

    private const string MasterVolumeKey = "MasterVolume";
    private const string BgmVolumeKey = "BGMVolume";
    private const string SfxVolumeKey = "SFXVolume";

    private const string MasterExposedName = "MasterVolume";
    private const string BgmExposedName = "BGMVolume";
    private const string SfxExposedName = "SFXVolume";

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
    }

    private void OnEnable()
    {
        ApplySavedVolumes();
    }

    private void Start()
    {
        ApplySavedVolumes();
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
        if (audioSource == null || _sfxSource == null || audioSource.outputAudioMixerGroup != null)
            return;

        audioSource.outputAudioMixerGroup = _sfxSource.outputAudioMixerGroup;
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
}
