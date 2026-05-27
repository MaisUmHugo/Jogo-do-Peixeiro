using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
public class UIButtonAudioFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private Button button;
    private Selectable selectable;
    private bool isBound;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindClick();
    }

    private void OnDisable()
    {
        UnbindClick();
    }

    private void OnDestroy()
    {
        UnbindClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsUsable())
            return;

        AudioManager.Instance?.PlayUIButtonHover(gameObject);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!IsUsable())
            return;

        AudioManager.Instance?.PlayUIButtonSelection(gameObject);
    }

    private void HandleClick()
    {
        if (!IsUsable())
            return;

        AudioManager.Instance?.PlayUIButtonClick(gameObject);
    }

    private void BindClick()
    {
        if (isBound || button == null)
            return;

        button.onClick.AddListener(HandleClick);
        isBound = true;
    }

    private void UnbindClick()
    {
        if (!isBound || button == null)
            return;

        button.onClick.RemoveListener(HandleClick);
        isBound = false;
    }

    private bool IsUsable()
    {
        ResolveReferences();
        return selectable != null &&
               selectable.gameObject.activeInHierarchy &&
               selectable.IsInteractable();
    }

    private void ResolveReferences()
    {
        if (selectable == null)
            selectable = GetComponent<Selectable>();

        if (button == null)
            button = selectable as Button;
    }
}
