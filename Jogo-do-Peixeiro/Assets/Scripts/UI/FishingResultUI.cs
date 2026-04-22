using TMPro;
using UnityEngine;

public class FishingResultUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    private FishResultPanelUI fishResultPanelUI;
    [SerializeField] private TMP_Text resultText;

    [Header("Settings")]
    [SerializeField] private float minDisplayTime = 1f;

    private bool isShowing;
    private bool canSkip;

    private void Awake()
    {
        fishResultPanelUI = panel.GetComponent<FishResultPanelUI>();
        HideImmediate();
    }

    private void Start()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onAnyButtonPressed += TryClose;
    }

    private void OnDestroy()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onAnyButtonPressed -= TryClose;
    }

    public void ShowCatchResult(FishData _fish)
    {
        if (panel != null)
            panel.SetActive(true);

        if (fishResultPanelUI != null)
            fishResultPanelUI.SetNewFish(_fish);

        if (resultText != null)
            resultText.text = $"{_fish.typeOfFish.name} - {_fish.weight} kg";

        isShowing = true;
        canSkip = false;

        CancelInvoke();
        Invoke(nameof(EnableSkip), minDisplayTime);
    }

    private void EnableSkip()
    {
        canSkip = true;
    }

    private void TryClose()
    {
        if (!isShowing || !canSkip)
            return;

        HideImmediate();
    }

    private void HideImmediate()
    {
        if (panel != null)
            panel.SetActive(false);

        isShowing = false;
        canSkip = false;
    }
}