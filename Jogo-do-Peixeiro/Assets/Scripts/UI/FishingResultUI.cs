using TMPro;
using UnityEngine;

public class FishingResultUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text resultText;

    [Header("Settings")]
    [SerializeField] private float minDisplayTime = 1f;

    private bool isShowing;
    private bool canSkip;

    private void Awake()
    {
        HideImmediate();
    }

    private void Start()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed += TryClose;
    }

    private void OnDestroy()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed -= TryClose;
    }

    public void ShowCatchResult(string _fishName, int _weight)
    {
        if (panel != null)
            panel.SetActive(true);

        if (resultText != null)
            resultText.text = $"{_fishName} - {_weight} kg";

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