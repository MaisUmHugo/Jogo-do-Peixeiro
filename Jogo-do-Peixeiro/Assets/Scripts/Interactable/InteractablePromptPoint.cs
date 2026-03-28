using UnityEngine;

public class InteractablePromptPoint : MonoBehaviour
{
    [SerializeField] private Transform promptPoint;

    public Transform PromptPoint => promptPoint;
}