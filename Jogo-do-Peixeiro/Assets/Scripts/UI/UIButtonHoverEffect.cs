using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target")]
    [SerializeField] private Transform visualTarget;

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

    [Header("Selection")]
    [SerializeField] private bool clearSelectionOnPointerExit;

    private bool isHovered;

    private void Update()
    {
        bool active = isHovered || IsCurrentlySelected();
        Transform target = GetVisualTarget();

        if (!active)
        {
            target.localScale = Vector3.Lerp(
                target.localScale,
                Vector3.one * normalScale,
                Time.unscaledDeltaTime * 10f
            );

            if (useTilt)
            {
                target.localRotation = Quaternion.Lerp(
                    target.localRotation,
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

        target.localScale = Vector3.Lerp(
            target.localScale,
            Vector3.one * scale,
            Time.unscaledDeltaTime * 10f
        );

        if (useTilt)
        {
            float tilt = Mathf.Sin(Time.unscaledTime * tiltSpeed) * tiltAmount;

            Quaternion targetRot = Quaternion.Euler(0f, 0f, tilt);

            target.localRotation = Quaternion.Lerp(
                target.localRotation,
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

    private Transform GetVisualTarget()
    {
        return visualTarget != null ? visualTarget : transform;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (clearSelectionOnPointerExit &&
            EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
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

        Transform target = GetVisualTarget();
        target.localScale = Vector3.one * normalScale;
        target.localRotation = Quaternion.identity;
    }
}
