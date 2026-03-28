using UnityEngine;
using UnityEngine.InputSystem;

public class FishSkillCheck : MonoBehaviour
{
    public float fishingSpotSize = 1f;

    public float fishingRangeStart { get; private set; }
    public float fishingRangeEnd { get; private set; }

    [SerializeField] private float fishRangeEndSize = 0.15f;

    public float currentSpotIndex { get; private set; }

    private FishingManager fishingManager;

    public void StartSkillCheck(FishingManager _fishingManager)
    {
        fishingManager = _fishingManager;

        currentSpotIndex = 0f;
        fishingRangeStart = Random.Range(0.1f, 0.7f);
        fishingRangeEnd = fishingRangeStart + fishRangeEndSize;

        gameObject.SetActive(true);
        enabled = true;

        Debug.Log("Skill check iniciado");
    }

    private void Start()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed += CheckClick;
    }

    private void OnDestroy()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed -= CheckClick;
    }

    private void FailMinigame()
    {
        Debug.Log("Falhou no minigame");

        enabled = false;
        gameObject.SetActive(false);

        if (fishingManager != null)
            fishingManager.OnSkillCheckFail();
    }

    private void WinMinigame()
    {
        Debug.Log("Passou no minigame");

        enabled = false;
        gameObject.SetActive(false);

        if (fishingManager != null)
            fishingManager.OnSkillCheckSuccess();
    }

    private void CheckClick()
    {
        if (!enabled)
            return;

        float index = currentSpotIndex;

        if (index >= fishingRangeStart && index <= fishingRangeEnd)
        {
            WinMinigame();
            return;
        }

        FailMinigame();
    }

    private void AddSpotIndex()
    {
        currentSpotIndex += Time.deltaTime;

        if (currentSpotIndex >= fishingSpotSize)
            FailMinigame();
    }

    private void Update()
    {
        AddSpotIndex();
    }
}