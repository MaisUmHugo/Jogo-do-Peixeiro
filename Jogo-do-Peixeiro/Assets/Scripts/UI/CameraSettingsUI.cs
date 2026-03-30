using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CameraSettingsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityValueText;
    [SerializeField] private PlayerCamera playerCamera;

    [Header("Settings")]
    [SerializeField] private float defaultSensitivity = 2f;

    private const string SensitivityKey = "CameraSensitivity";

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<PlayerCamera>();

        float savedSensitivity = PlayerPrefs.GetFloat(SensitivityKey, defaultSensitivity);

        if (sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(savedSensitivity);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        ApplySensitivity(savedSensitivity);
        UpdateValueText(savedSensitivity);
    }

    private void OnDestroy()
    {
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
    }

    private void OnSensitivityChanged(float _value)
    {
        ApplySensitivity(_value);
        SaveSensitivity(_value);
        UpdateValueText(_value);
    }

    private void ApplySensitivity(float _value)
    {
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<PlayerCamera>();

        if (playerCamera != null)
            playerCamera.SetSensitivity(_value);
    }

    private void SaveSensitivity(float _value)
    {
        PlayerPrefs.SetFloat(SensitivityKey, _value);
        PlayerPrefs.Save();
    }

    private void UpdateValueText(float _value)
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = _value.ToString("F1");
    }

    public void ResetSensitivity()
    {
        if (sensitivitySlider != null)
            sensitivitySlider.value = defaultSensitivity;
        else
            OnSensitivityChanged(defaultSensitivity);
    }
}