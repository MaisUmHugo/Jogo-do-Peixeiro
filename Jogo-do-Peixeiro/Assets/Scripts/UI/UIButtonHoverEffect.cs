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
    [SerializeField] private bool useTilt = true;
    [SerializeField] private float tiltAmount = 5f;
    [SerializeField] private float tiltSpeed = 1f;

    private bool isHovered;
    private bool isKeyboardSelected;

    private bool IsReallySelected()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.currentSelectedGameObject == gameObject;
    }

    private void Update()
    {
        bool isActive = isHovered || isKeyboardSelected || IsReallySelected();

        if (!isActive)
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

        float targetScale = selectedScale;

        if (usePulse)
            targetScale += Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            Vector3.one * targetScale,
            Time.unscaledDeltaTime * 10f
        );

        if (useTilt)
        {
            float tilt = Mathf.Sin(Time.unscaledTime * tiltSpeed) * tiltAmount;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, tilt);

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                Time.unscaledDeltaTime * 10f
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isKeyboardSelected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isKeyboardSelected = false;
    }

    private void OnDisable()
    {
        ResetVisuals();
    }

    private void OnDestroy()
    {
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        isHovered = false;
        isKeyboardSelected = false;
        transform.localScale = Vector3.one * normalScale;
        transform.rotation = Quaternion.identity;
    }
}