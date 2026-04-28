using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CameraSettingsUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityValueText;
    [SerializeField] private PlayerCamera playerCamera;

    [Header("Slider")]
    [SerializeField, Range(1, 100)] private int defaultSensitivitySliderValue = 35;

    [Header("Mouse Mapping")]
    [SerializeField] private float minMouseSensitivity = 0.02f;
    [SerializeField] private float maxMouseSensitivity = 0.4f;
    [SerializeField] private float mouseSensitivityCurve = 1.6f;

    [Header("Controller Mapping")]
    [SerializeField] private float minControllerSensitivity = 45f;
    [SerializeField] private float maxControllerSensitivity = 260f;
    [SerializeField] private float controllerSensitivityCurve = 1.25f;

    private const string LegacySensitivityKey = "CameraSensitivity";
    private const string SensitivitySliderKey = "CameraSensitivitySlider";
    private const string MouseSensitivityKey = "CameraMouseSensitivity";
    private const string ControllerSensitivityKey = "CameraControllerSensitivity";

    private void OnValidate()
    {
        defaultSensitivitySliderValue = Mathf.Clamp(defaultSensitivitySliderValue, 1, 100);
        minMouseSensitivity = Mathf.Max(0.001f, minMouseSensitivity);
        maxMouseSensitivity = Mathf.Max(minMouseSensitivity, maxMouseSensitivity);
        mouseSensitivityCurve = Mathf.Max(0.01f, mouseSensitivityCurve);
        minControllerSensitivity = Mathf.Max(1f, minControllerSensitivity);
        maxControllerSensitivity = Mathf.Max(minControllerSensitivity, maxControllerSensitivity);
        controllerSensitivityCurve = Mathf.Max(0.01f, controllerSensitivityCurve);
    }

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<PlayerCamera>();

        ConfigureSlider();

        int savedSliderValue = LoadSensitivitySliderValue();

        if (sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(savedSliderValue);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        ApplySensitivity(savedSliderValue);
        SaveSensitivity(savedSliderValue);
        UpdateValueText(savedSliderValue);
    }

    private void OnDestroy()
    {
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
    }

    private void ConfigureSlider()
    {
        if (sensitivitySlider == null)
            return;

        sensitivitySlider.minValue = 1f;
        sensitivitySlider.maxValue = 100f;
        sensitivitySlider.wholeNumbers = true;
    }

    private int LoadSensitivitySliderValue()
    {
        if (PlayerPrefs.HasKey(SensitivitySliderKey))
            return Mathf.Clamp(PlayerPrefs.GetInt(SensitivitySliderKey, defaultSensitivitySliderValue), 1, 100);

        if (PlayerPrefs.HasKey(LegacySensitivityKey))
        {
            float legacySensitivity = PlayerPrefs.GetFloat(LegacySensitivityKey, 0.1f);
            return ConvertMouseSensitivityToSlider(legacySensitivity);
        }

        return defaultSensitivitySliderValue;
    }

    private void OnSensitivityChanged(float _value)
    {
        int sliderValue = Mathf.RoundToInt(_value);
        ApplySensitivity(sliderValue);
        SaveSensitivity(sliderValue);
        UpdateValueText(sliderValue);
    }

    private void ApplySensitivity(int _sliderValue)
    {
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<PlayerCamera>();

        if (playerCamera == null)
            return;

        float mouseSensitivity = ConvertSliderToMouseSensitivity(_sliderValue);
        float controllerSensitivity = ConvertSliderToControllerSensitivity(_sliderValue);

        playerCamera.SetSensitivity(mouseSensitivity, controllerSensitivity);
    }

    private void SaveSensitivity(int _sliderValue)
    {
        PlayerPrefs.SetInt(SensitivitySliderKey, _sliderValue);
        PlayerPrefs.SetFloat(MouseSensitivityKey, ConvertSliderToMouseSensitivity(_sliderValue));
        PlayerPrefs.SetFloat(ControllerSensitivityKey, ConvertSliderToControllerSensitivity(_sliderValue));
        PlayerPrefs.Save();
    }

    private void UpdateValueText(int _sliderValue)
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = _sliderValue.ToString();
    }

    public void ResetSensitivity()
    {
        if (sensitivitySlider != null)
            sensitivitySlider.value = defaultSensitivitySliderValue;
        else
            OnSensitivityChanged(defaultSensitivitySliderValue);
    }

    private float ConvertSliderToMouseSensitivity(int _sliderValue)
    {
        float normalized = Mathf.InverseLerp(1f, 100f, _sliderValue);
        float curved = Mathf.Pow(normalized, mouseSensitivityCurve);
        return Mathf.Lerp(minMouseSensitivity, maxMouseSensitivity, curved);
    }

    private float ConvertSliderToControllerSensitivity(int _sliderValue)
    {
        float normalized = Mathf.InverseLerp(1f, 100f, _sliderValue);
        float curved = Mathf.Pow(normalized, controllerSensitivityCurve);
        return Mathf.Lerp(minControllerSensitivity, maxControllerSensitivity, curved);
    }

    private int ConvertMouseSensitivityToSlider(float _mouseSensitivity)
    {
        float normalized = Mathf.InverseLerp(minMouseSensitivity, maxMouseSensitivity, _mouseSensitivity);
        float sliderNormalized = Mathf.Pow(Mathf.Clamp01(normalized), 1f / mouseSensitivityCurve);
        return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 100f, sliderNormalized)), 1, 100);
    }
}
