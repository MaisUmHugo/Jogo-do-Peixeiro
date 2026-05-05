using System;
using TMPro;
using UnityEngine;

public class DayCycle : MonoBehaviour
{
    public event Action<int> DayChanged;
    public event Action ForcedSleepRequested;

    [Header("Referências")]
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;
    [SerializeField] private TextMeshProUGUI HourText;

    [Header("Tempo")]
    [SerializeField] private float dayDuration = 120f;
    [SerializeField] private float currentTime = 0f;
    [SerializeField] private float Clock;

    [Header("Curvas")]
    [SerializeField] private AnimationCurve sunIntensity;
    [SerializeField] private Gradient skyColor;
    [SerializeField] private Gradient groundColor;

    [Header("Skybox Settings")]
    [SerializeField] private float atmosphereThickness = 1.0f;
    [SerializeField] private float exposure = 1.3f;

    [Header("Dias")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int totalDays = 3;
    [SerializeField] private TextMeshProUGUI DayText;
    [SerializeField] private int elapsedDays = 1;

    [Header("Sono Obrigatório")]
    [SerializeField] private bool forceSleepAfterDeadline = true;
    [SerializeField, Range(0f, 24f)] private float bedtimeWarningHour = 0f;
    [SerializeField, Range(0f, 24f)] private float forcedSleepHour = 1f;
    [SerializeField, Range(0f, 24f)] private float wakeUpHour = 6f;
    [SerializeField] private string bedtimeWarningMessage = "Está ficando tarde. Volte para dormir.";
    [SerializeField] private string forcedSleepMessage = "Você apagou de sono e acordou às 6 AM.";

    private bool hasShownBedtimeWarning;
    private bool hasForcedSleepThisNight;
    private bool hasAdvancedDaySinceLastWake;

    public int CurrentDay => currentDay;
    public int TotalDays => totalDays;
    public int ElapsedDays => elapsedDays;
    public float NormalizedTime => currentTime;
    public bool IsHourTextVisible => HourText != null && HourText.gameObject.activeSelf;
    public bool IsDayTextVisible => DayText != null && DayText.gameObject.activeSelf;

    void Start()
    {
        UpdateDayUI();
    }

    void Update()
    {
        float previousTime = currentTime;
        currentTime += Time.deltaTime / dayDuration;
        bool wrappedDay = false;

        while (currentTime >= 1f)
        {
            currentTime -= 1f;
            AdvanceDay(false);
            hasAdvancedDaySinceLastWake = true;
            wrappedDay = true;
        }

        UpdateSleepDeadline(previousTime, currentTime, wrappedDay);
        UpdateSun();
        UpdateSkybox();
        UpdateTime();
    }

    void UpdateDayUI()
    {
        if (DayText != null)
            DayText.text = $"Dia {currentDay}/{totalDays}";
    }

    public void SetDayCycleHudVisible(bool _visible)
    {
        SetDayCycleHudVisible(_visible, _visible);
    }

    public void SetDayCycleHudVisible(bool _hourVisible, bool _dayVisible)
    {
        if (HourText != null)
            HourText.gameObject.SetActive(_hourVisible);

        if (DayText != null)
            DayText.gameObject.SetActive(_dayVisible);
    }

    void UpdateSun()
    {
        if (sun == null) return;

        float sunAngle = currentTime * 360f - 90f;

        sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0);
        sun.intensity = Mathf.Max(0.2f, sunIntensity.Evaluate(currentTime));

        if (moon != null)
        {
            moon.transform.rotation = Quaternion.Euler(sunAngle + 180f, 170f, 0);
            moon.intensity = Mathf.Clamp01(1f - sun.intensity) * 0.3f;
        }
    }

    void UpdateSkybox()
    {
        if (skyboxMaterial == null) return;

        Color sky = skyColor.Evaluate(currentTime);
        skyboxMaterial.SetColor("_SkyTint", sky);

        Color ground = groundColor.Evaluate(currentTime);
        skyboxMaterial.SetColor("_GroundColor", ground);

        float dynamicExposure = Mathf.Lerp(exposure, 2.0f, 1f - sunIntensity.Evaluate(currentTime));
        skyboxMaterial.SetFloat("_Exposure", dynamicExposure);

        skyboxMaterial.SetFloat("_AtmosphereThickness", atmosphereThickness);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = sky * 0.3f;

        DynamicGI.UpdateEnvironment();
    }

    void UpdateTime()
    {
        Clock = currentTime * 24f;

        int hours24 = Mathf.FloorToInt(Clock);
        int minutes = Mathf.FloorToInt((Clock % 1f) * 60f);

        string period = hours24 >= 12 ? "PM" : "AM";

        int hours12 = hours24 % 12;
        if (hours12 == 0) hours12 = 12;

        if (HourText != null)
            HourText.text = $"{hours12:00}:00 {period}";
        //HourText.text = $"{hours12:00}:{minutes:00} {period}";
    }

    public void NextDay()
    {
        AdvanceDay(true);
        ResetSleepDeadlineState();
    }

    private void AdvanceDay(bool _resetToMorning)
    {
        currentDay++;
        elapsedDays++;

        if (currentDay > totalDays)
            currentDay = 1;

        if (_resetToMorning)
        {
            SetTimeToHour(wakeUpHour);
        }

        UpdateSun();
        UpdateSkybox();
        UpdateTime();
        UpdateDayUI();
        DayChanged?.Invoke(elapsedDays);
    }

    private void UpdateSleepDeadline(float _previousTime, float _currentTime, bool _wrappedDay)
    {
        if (!forceSleepAfterDeadline)
            return;

        if (!hasShownBedtimeWarning && HasCrossedHour(_previousTime, _currentTime, _wrappedDay, bedtimeWarningHour))
        {
            hasShownBedtimeWarning = true;

            if (HUDWarningUI.Instance != null)
                HUDWarningUI.Instance.ShowWarning(bedtimeWarningMessage);
        }

        if (!hasForcedSleepThisNight && HasCrossedHour(_previousTime, _currentTime, _wrappedDay, forcedSleepHour))
            ForceSleep();
    }

    private bool HasCrossedHour(float _previousTime, float _currentTime, bool _wrappedDay, float _targetHour)
    {
        float targetNormalized = Mathf.Repeat(_targetHour, 24f) / 24f;

        if (!_wrappedDay)
            return _previousTime < targetNormalized && _currentTime >= targetNormalized;

        return _previousTime < targetNormalized || _currentTime >= targetNormalized;
    }

    private void ForceSleep()
    {
        hasForcedSleepThisNight = true;

        if (!hasAdvancedDaySinceLastWake)
            AdvanceDay(false);

        SetTimeToHour(wakeUpHour);
        ResetSleepDeadlineState();

        UpdateSun();
        UpdateSkybox();
        UpdateTime();
        UpdateDayUI();

        if (HUDWarningUI.Instance != null)
            HUDWarningUI.Instance.ShowWarning(forcedSleepMessage);

        ForcedSleepRequested?.Invoke();
    }

    private void SetTimeToHour(float _hour)
    {
        float normalizedHour = Mathf.Repeat(_hour, 24f) / 24f;
        currentTime = normalizedHour;
        Clock = Mathf.Repeat(_hour, 24f);
    }

    private void ResetSleepDeadlineState()
    {
        hasShownBedtimeWarning = false;
        hasForcedSleepThisNight = false;
        hasAdvancedDaySinceLastWake = false;
    }
}
