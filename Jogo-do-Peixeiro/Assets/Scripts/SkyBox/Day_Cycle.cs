using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DayCycle : MonoBehaviour
{
    public event Action<int> DayChanged;
    public event Action ForcedSleepRequested;
    public event Action<bool> VisualModeChanged;

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
    [Tooltip("Valor legado. O prazo das quests e controlado pelo CampaignProgressSystem.")]
    [SerializeField] private int totalDays = 3;
    [SerializeField] private TextMeshProUGUI DayText;
    [SerializeField] private int elapsedDays = 1;

    [Header("HUD Visuals")]
    [SerializeField] private Image dayProgressBackplate;
    [SerializeField] private RectTransform dayProgressTrack;
    [SerializeField] private RectTransform dayProgressHandle;
    [SerializeField] private bool autoResolveDayCycleHud = true;
    [SerializeField] private bool keepDayProgressHandleInsideTrack = true;
    [SerializeField, Range(0f, 24f)] private float dayVisualStartHour = 6f;
    [SerializeField, Range(0f, 24f)] private float dayVisualEndHour = 18f;
    [SerializeField] private Color dayPrimaryTextColor = new Color(0.28f, 0.16f, 0.04f, 1f);
    [SerializeField] private Color daySecondaryTextColor = new Color(0.07f, 0.08f, 0.09f, 1f);
    [SerializeField] private Color nightPrimaryTextColor = new Color(1f, 0.82f, 0.2f, 1f);
    [SerializeField] private Color nightSecondaryTextColor = Color.white;

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
    private bool isForcedSleepPending;
    private bool isDayVisualMode;
    private bool hasInitializedVisualMode;

    public int CurrentDay => currentDay;
    public int TotalDays => totalDays;
    public int ElapsedDays => elapsedDays;
    public float NormalizedTime => currentTime;
    public bool IsHourTextVisible => HourText != null && HourText.gameObject.activeSelf;
    public bool IsDayTextVisible => IsDayCycleHudVisible();
    public bool IsDayVisualMode => isDayVisualMode;
    public Color PrimaryHudTextColor => isDayVisualMode ? dayPrimaryTextColor : nightPrimaryTextColor;
    public Color SecondaryHudTextColor => isDayVisualMode ? daySecondaryTextColor : nightSecondaryTextColor;
    public string PrimaryHudTextColorHex => $"#{ColorUtility.ToHtmlStringRGB(PrimaryHudTextColor)}";
    public string SecondaryHudTextColorHex => $"#{ColorUtility.ToHtmlStringRGB(SecondaryHudTextColor)}";

    void Start()
    {
        ResolveDayCycleHudReferences();
        HideHourText();
        UpdateDayUI();
        UpdateTime();
        UpdateDayProgress();
        UpdateHudVisualMode(true);
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
        UpdateDayProgress();
        UpdateHudVisualMode(false);
    }

    void UpdateDayUI()
    {
        if (DayText != null)
            DayText.text = $"Dia {currentDay}";
    }

    public void SetDayCycleHudVisible(bool _visible)
    {
        SetDayCycleHudVisible(_visible, _visible);
    }

    public void SetDayCycleHudVisible(bool _hourVisible, bool _dayVisible)
    {
        ResolveDayCycleHudReferences();
        HideHourText();

        if (DayText != null)
            DayText.gameObject.SetActive(_dayVisible);

        bool barVisible = _hourVisible || _dayVisible;

        if (dayProgressBackplate != null)
            dayProgressBackplate.gameObject.SetActive(barVisible);

        if (dayProgressTrack != null)
            dayProgressTrack.gameObject.SetActive(barVisible);

        if (dayProgressHandle != null)
            dayProgressHandle.gameObject.SetActive(barVisible);
    }

    private bool IsDayCycleHudVisible()
    {
        ResolveDayCycleHudReferences();

        if (DayText != null && DayText.gameObject.activeSelf)
            return true;

        if (dayProgressBackplate != null && dayProgressBackplate.gameObject.activeSelf)
            return true;

        if (dayProgressTrack != null && dayProgressTrack.gameObject.activeSelf)
            return true;

        return dayProgressHandle != null && dayProgressHandle.gameObject.activeSelf;
    }

    private void UpdateDayProgress()
    {
        float normalizedTime = Mathf.Clamp01(currentTime);
        UpdateDayProgressHandle(normalizedTime);
    }

    private void UpdateDayProgressHandle(float _normalizedTime)
    {
        if (dayProgressHandle == null)
            return;

        RectTransform track = GetDayProgressTrack();

        if (track == null)
            return;

        Rect trackRect = track.rect;
        float minX = trackRect.xMin;
        float maxX = trackRect.xMax;

        if (keepDayProgressHandleInsideTrack)
        {
            float halfHandleWidth = dayProgressHandle.rect.width * 0.5f;
            minX += halfHandleWidth;
            maxX -= halfHandleWidth;

            if (minX > maxX)
            {
                minX = trackRect.xMin;
                maxX = trackRect.xMax;
            }
        }

        float localX = Mathf.Lerp(minX, maxX, Mathf.Clamp01(_normalizedTime));
        Vector3 trackWorldPosition = track.TransformPoint(new Vector3(localX, trackRect.center.y, 0f));
        RectTransform handleParent = dayProgressHandle.parent as RectTransform;

        if (handleParent == null)
        {
            dayProgressHandle.position = trackWorldPosition;
            return;
        }

        Vector3 parentLocalPosition = handleParent.InverseTransformPoint(trackWorldPosition);
        Vector3 handleLocalPosition = dayProgressHandle.localPosition;
        handleLocalPosition.x = parentLocalPosition.x;
        handleLocalPosition.y = parentLocalPosition.y;
        dayProgressHandle.localPosition = handleLocalPosition;
    }

    private RectTransform GetDayProgressTrack()
    {
        if (dayProgressTrack != null)
            return dayProgressTrack;

        if (dayProgressBackplate != null)
            return dayProgressBackplate.rectTransform;

        return null;
    }

    private void ResolveDayCycleHudReferences()
    {
        if (!autoResolveDayCycleHud)
            return;

        if (DayText == null)
            DayText = FindHudComponentByName<TextMeshProUGUI>("Days", "DayText", "DiaText");

        if (HourText == null)
            HourText = FindHudComponentByName<TextMeshProUGUI>("Time", "HourText", "Horas");

        if (dayProgressBackplate == null)
            dayProgressBackplate = FindHudComponentByName<Image>("DayProgressBackplate");

        if (dayProgressTrack == null)
            dayProgressTrack = FindHudComponentByName<RectTransform>("DayProgressTrack");

        if (dayProgressHandle == null)
            dayProgressHandle = FindHudComponentByName<RectTransform>("DayProgressHandle");
    }

    private T FindHudComponentByName<T>(params string[] _names) where T : Component
    {
        if (_names == null || _names.Length == 0)
            return null;

        T[] components = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];

            if (component == null)
                continue;

            for (int j = 0; j < _names.Length; j++)
            {
                if (string.Equals(component.gameObject.name, _names[j], StringComparison.OrdinalIgnoreCase))
                    return component;
            }
        }

        return null;
    }

    private void HideHourText()
    {
        if (HourText != null)
            HourText.gameObject.SetActive(false);
    }

    private void UpdateHudVisualMode(bool _forceNotify)
    {
        bool nextDayVisualMode = IsCurrentHourInDayVisualRange();

        if (!_forceNotify && hasInitializedVisualMode && nextDayVisualMode == isDayVisualMode)
            return;

        isDayVisualMode = nextDayVisualMode;
        hasInitializedVisualMode = true;
        ApplyDayCycleTextColors();
        VisualModeChanged?.Invoke(isDayVisualMode);
    }

    private void ApplyDayCycleTextColors()
    {
        if (DayText != null)
            DayText.color = PrimaryHudTextColor;

        if (HourText != null)
            HourText.color = SecondaryHudTextColor;
    }

    private bool IsCurrentHourInDayVisualRange()
    {
        float currentHour = Mathf.Repeat(currentTime * 24f, 24f);
        float startHour = Mathf.Repeat(dayVisualStartHour, 24f);
        float endHour = Mathf.Repeat(dayVisualEndHour, 24f);

        if (Mathf.Approximately(startHour, endHour))
            return true;

        if (startHour < endHour)
            return currentHour >= startHour && currentHour < endHour;

        return currentHour >= startHour || currentHour < endHour;
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

        if (HourText != null)
            HourText.text = string.Empty;
    }

    public void NextDay()
    {
        AdvanceDay(true);
        ResetSleepDeadlineState();
    }

    public void DebugAdvanceDay()
    {
        AdvanceDay(true);
        ResetSleepDeadlineState();
    }

    public void SetCycleState(int _currentDay, int _elapsedDays, float _normalizedTime)
    {
        currentDay = Mathf.Max(1, Mathf.Max(_currentDay, _elapsedDays));
        elapsedDays = Mathf.Max(1, Mathf.Max(_elapsedDays, currentDay));
        currentTime = Mathf.Repeat(_normalizedTime, 1f);
        Clock = currentTime * 24f;
        ResetSleepDeadlineState();
        UpdateSun();
        UpdateSkybox();
        UpdateTime();
        UpdateDayProgress();
        UpdateHudVisualMode(true);
        UpdateDayUI();
    }

    private void AdvanceDay(bool _resetToMorning)
    {
        currentDay++;
        elapsedDays++;

        if (_resetToMorning)
        {
            SetTimeToHour(wakeUpHour);
        }

        UpdateSun();
        UpdateSkybox();
        UpdateTime();
        UpdateDayProgress();
        UpdateHudVisualMode(true);
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
        isForcedSleepPending = true;

        if (ForcedSleepRequested != null)
        {
            ForcedSleepRequested.Invoke();
            return;
        }

        CompleteForcedSleepWakeUp();
    }

    public void CompleteForcedSleepWakeUp()
    {
        if (!isForcedSleepPending)
            return;

        if (!hasAdvancedDaySinceLastWake)
            AdvanceDay(false);

        SetTimeToHour(wakeUpHour);
        UpdateSun();
        UpdateSkybox();
        UpdateTime();
        UpdateDayProgress();
        UpdateHudVisualMode(true);
        UpdateDayUI();

        ResetSleepDeadlineState();
        isForcedSleepPending = false;

        if (HUDWarningUI.Instance != null)
            HUDWarningUI.Instance.ShowWarning(forcedSleepMessage);
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
