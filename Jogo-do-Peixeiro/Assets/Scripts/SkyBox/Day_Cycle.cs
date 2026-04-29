using System;
using TMPro;
using UnityEngine;

public class DayCycle : MonoBehaviour
{
    public event Action<int> DayChanged;

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

    public int CurrentDay => currentDay;
    public int TotalDays => totalDays;
    public int ElapsedDays => elapsedDays;
    public float NormalizedTime => currentTime;

    void Start()
    {
        UpdateDayUI();
    }

    void Update()
    {
        currentTime += Time.deltaTime / dayDuration;
        if (currentTime > 1f)
        {
            currentTime = 0f;
        }

        UpdateSun();
        UpdateSkybox();
        UpdateTime();
    }

    void UpdateDayUI()
    {
        if (DayText != null)
            DayText.text = $"Dia {currentDay}/{totalDays}";
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
            HourText.text = $"{hours12:00}:{minutes:00} {period}";
    }

    public void NextDay()
    {
        currentDay++;
        elapsedDays++;

        if (currentDay > totalDays)
            currentDay = 1;

        currentTime = 6f / 24f;
        Clock = 6f;

        UpdateSun();
        UpdateSkybox();
        UpdateTime();
        UpdateDayUI();
        DayChanged?.Invoke(elapsedDays);
    }
}