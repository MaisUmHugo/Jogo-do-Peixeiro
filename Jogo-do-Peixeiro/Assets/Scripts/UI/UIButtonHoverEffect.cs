using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Scale")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float selectedScale = 1.1f;

    [Header("Pulse")]
    [SerializeField] private bool usePulse = true;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseAmount = 0.05f;

    [Header("Tilt")]
    [SerializeField] private bool useTilt = false;
    [SerializeField] private float tiltAmount = 5f;
    [SerializeField] private float tiltSpeed = 1f;

    private bool isHovered;
    private bool isSelected;

    private void Update()
    {
        bool active = isHovered || IsCurrentlySelected();

        if (!active)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                Vector3.one * normalScale,
                Time.unscaledDeltaTime * 10f
            );

            if (useTilt)
            {
                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    Quaternion.identity,
                    Time.unscaledDeltaTime * 10f
                );
            }

            return;
        }

        float scale = selectedScale;

        if (usePulse)
        {
            scale += Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
        }

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            Vector3.one * scale,
            Time.unscaledDeltaTime * 10f
        );

        if (useTilt)
        {
            float tilt = Mathf.Sin(Time.unscaledTime * tiltSpeed) * tiltAmount;

            Quaternion targetRot = Quaternion.Euler(0f, 0f, tilt);

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRot,
                Time.unscaledDeltaTime * 10f
            );
        }
    }

    private bool IsCurrentlySelected()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.currentSelectedGameObject == gameObject;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        // limpa seleção se esse botão ainda estiver selecionado
        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
    }

    private void OnDisable()
    {
        ResetVisuals();

        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void ResetVisuals()
    {
        isHovered = false;
        isSelected = false;

        transform.localScale = Vector3.one * normalScale;
        transform.rotation = Quaternion.identity;
    }
}