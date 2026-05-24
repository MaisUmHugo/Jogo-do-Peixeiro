using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField, InspectorName("SFX Slider")] private Slider _sfxSlider;

    private bool _isBound;

    private void OnEnable()
    {
        BindSliders();
    }

    private void Start()
    {
        BindSliders();
    }

    private void OnDisable()
    {
        UnbindSliders();
    }

    private void BindSliders()
    {
        if (_isBound)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.ApplySavedVolumes();

        if (_masterSlider != null)
        {
            _masterSlider.SetValueWithoutNotify(AudioManager.Instance.GetMasterVolume());
            _masterSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
        }

        if (_bgmSlider != null)
        {
            _bgmSlider.SetValueWithoutNotify(AudioManager.Instance.GetBgmVolume());
            _bgmSlider.onValueChanged.AddListener(HandleBgmVolumeChanged);
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.SetValueWithoutNotify(AudioManager.Instance.GetSfxVolume());
            _sfxSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
        }

        _isBound = true;
    }

    private void UnbindSliders()
    {
        if (!_isBound)
            return;

        if (_masterSlider != null)
            _masterSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);

        if (_bgmSlider != null)
            _bgmSlider.onValueChanged.RemoveListener(HandleBgmVolumeChanged);

        if (_sfxSlider != null)
            _sfxSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);

        _isBound = false;
    }

    private void HandleMasterVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMasterVolume(value);
    }

    private void HandleBgmVolumeChanged(float value)
    {
        AudioManager.Instance?.SetBgmVolume(value);
    }

    private void HandleSfxVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSfxVolume(value);
    }
}
