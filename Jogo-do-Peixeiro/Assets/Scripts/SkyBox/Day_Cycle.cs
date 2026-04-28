using System;
using TMPro;
using UnityEngine;

public class DayCycle : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private Light sun;
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

    void Update()
    {
        currentTime += Time.deltaTime / dayDuration;
        if (currentTime > 1f)
        {
            currentTime = 0f;
            NextDay();
        }

        UpdateSun();
        UpdateSkybox();
        UpdateTime();
    }
    void UpdateDayUI()
    {
        DayText.text = $"Dia {currentDay}/{totalDays}";
    }
    void Start()
    {
        UpdateDayUI();
    }

    void UpdateSun()
    {
        float sunAngle = currentTime * 360f - 90f;
        sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0);
        sun.intensity = sunIntensity.Evaluate(currentTime);
    }

    void UpdateSkybox()
    {
        if (skyboxMaterial == null) return;

        // Cor do céu
        Color sky = skyColor.Evaluate(currentTime);
        skyboxMaterial.SetColor("_SkyTint", sky);

        // Cor do chão
        Color ground = groundColor.Evaluate(currentTime);
        skyboxMaterial.SetColor("_GroundColor", ground);

        // Atmosfera
        skyboxMaterial.SetFloat("_AtmosphereThickness", atmosphereThickness);

        // Exposição
        skyboxMaterial.SetFloat("_Exposure", exposure);

        // Atualiza a iluminação global
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

        HourText.text = $"{hours12:00}:{minutes:00} {period}";
    }
    public void NextDay()
    {
        currentDay++;

        if (currentDay > totalDays)
            currentDay = 1;
        currentTime = 6f / 24f;
        Clock = 6f;

        UpdateSun();
        UpdateSkybox();
        UpdateTime();
        UpdateDayUI();
    }
}