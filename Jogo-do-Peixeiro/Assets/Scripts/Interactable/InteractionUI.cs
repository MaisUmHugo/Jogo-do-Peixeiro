using UnityEngine;

public class InteractionUI : MonoBehaviour
{
    [SerializeField] private GameObject interactButton;

    private void Start()
    {
        Hide();
    }

    public void Show()
    {
        if (interactButton != null)
            interactButton.SetActive(true);
    }

    public void Hide()
    {
        if (interactButton != null)
            interactButton.SetActive(false);
    }
}