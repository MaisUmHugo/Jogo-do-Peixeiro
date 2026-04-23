using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;

    private void Start()
    {
        if (AudioManager.Instance == null)
            return;

        if (_masterSlider != null)
        {
            _masterSlider.value = AudioManager.Instance.GetMasterVolume();
            _masterSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
        }

        if (_bgmSlider != null)
        {
            _bgmSlider.value = AudioManager.Instance.GetBgmVolume();
            _bgmSlider.onValueChanged.AddListener(AudioManager.Instance.SetBgmVolume);
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.value = AudioManager.Instance.GetSfxVolume();
            _sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSfxVolume);
        }
    }
}