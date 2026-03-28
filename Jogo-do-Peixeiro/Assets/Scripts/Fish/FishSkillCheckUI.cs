using UnityEngine;
using UnityEngine.UI;

public class FishSkillCheckUI : MonoBehaviour
{
    private FishSkillCheck fishSkillCheck;
    private Transform positionIndicator;
    private Scrollbar fishingRangeIndicator;

    private void Awake()
    {
        fishSkillCheck = GetComponent<FishSkillCheck>();
        positionIndicator = transform.GetChild(1).transform.GetChild(0).transform.GetChild(1);
        fishingRangeIndicator = transform.GetChild(1).GetComponent<Scrollbar>();
    }

    private void MoveIndicator()
    {
        float spot = fishSkillCheck.currentSpotIndex / fishSkillCheck.fishingSpotSize;
        float newPos = (spot * 1000f) - 500f;

        positionIndicator.position = new Vector3(newPos + 960f, positionIndicator.position.y, 0f);
    }

    private void Update()
    {
        if (fishSkillCheck == null)
            return;

        fishingRangeIndicator.value = fishSkillCheck.fishingRangeStart;
        MoveIndicator();
    }
}