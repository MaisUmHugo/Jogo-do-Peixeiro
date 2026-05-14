using UnityEngine;

public class DialogCameraFocusTarget : MonoBehaviour
{
    [Header("Focus")]
    [SerializeField] private Transform focusPoint;
    [SerializeField] private Vector3 focusOffset = new Vector3(0f, 1.45f, 0f);

    [Header("Camera Angle")]
    [SerializeField] private bool useFixedAngles;
    [SerializeField] private float fixedYaw;
    [SerializeField] private float fixedPitch = 12f;

    [Header("Transition")]
    [SerializeField, Min(0.01f)] private float transitionSpeed = 8f;

    public Transform FocusPoint => focusPoint != null ? focusPoint : transform;
    public Vector3 FocusPosition => FocusPoint.position + focusOffset;
    public bool UseFixedAngles => useFixedAngles;
    public float FixedYaw => fixedYaw;
    public float FixedPitch => fixedPitch;
    public float TransitionSpeed => transitionSpeed;
}
