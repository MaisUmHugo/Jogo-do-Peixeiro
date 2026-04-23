using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer _mainMixer;

    [Header("Sources")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

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

        LoadVolumes();
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (_bgmSource == null || clip == null)
            return;

        if (_bgmSource.clip == clip)
            return;

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

    private void LoadVolumes()
    {
        float master = PlayerPrefs.GetFloat(MasterVolumeKey, 0.5f);
        float bgm = PlayerPrefs.GetFloat(BgmVolumeKey, 0.5f);
        float sfx = PlayerPrefs.GetFloat(SfxVolumeKey, 0.5f);

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