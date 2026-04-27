using System.Collections.Generic;
using UnityEngine;

public class InteractablePromptPoint : MonoBehaviour
{
    [HideInInspector]
    [SerializeField] private Transform promptPoint;

    [Header("Prompt Points")]
    [SerializeField] private Transform[] promptPoints;

    public IEnumerable<Transform> GetPromptPoints()
    {
        bool hasPromptPoints = false;

        if (promptPoints != null)
        {
            foreach (Transform point in promptPoints)
            {
                if (point == null)
                    continue;

                hasPromptPoints = true;
                yield return point;
            }
        }

        if (hasPromptPoints)
            yield break;

        if (promptPoint != null)
        {
            yield return promptPoint;
            yield break;
        }

        yield return transform;
    }
}
