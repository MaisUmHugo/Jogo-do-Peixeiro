using UnityEngine;
using UnityEngine.InputSystem;
public class FishSkillCheck : MonoBehaviour
{
    public float fishingSpotSize;

    public float fishingRangeStart {  get; private set; }
    public float fishingRangeEnd   { get; private set; }

    [SerializeField] private float fishRangeEndSize;

    public float currentSpotIndex { get; private set; }

    private void FailMinigame()
    {

        Debug.Log("falhou no minigame");
        enabled = false;

    }

    private void WinMinigame()
    {

        Debug.Log("passou no minigame");
        enabled = false;

    }

    public void CheckClick(InputAction.CallbackContext input)
    {
        if (input.phase == InputActionPhase.Started)
        {
            float index = currentSpotIndex;

            if (index >= fishingRangeStart && index <= fishingRangeEnd)
            {

                WinMinigame();
                return;
            }

            FailMinigame();
        }
    }

    private void AddSpotIndex()
    {

       currentSpotIndex += Time.deltaTime;
       if (currentSpotIndex >= fishingSpotSize) { FailMinigame(); }

    }


    private void Awake()
    {
        fishingRangeStart = 0.3f * Random.Range(1,4);
        fishingRangeEnd = fishingRangeStart + fishRangeEndSize;

        Debug.Log("posição inicial do minigame: " + fishingRangeStart);
    }

    private void Update()
    {
        AddSpotIndex();


    }
}
