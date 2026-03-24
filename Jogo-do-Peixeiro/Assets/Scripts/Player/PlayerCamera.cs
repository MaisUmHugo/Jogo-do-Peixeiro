using UnityEngine;
public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform playerTransform;

    [Header("Offset")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private float height = 1.25f;

    [Header("Sensitivity")]
    [SerializeField] private float sensitivityX = 30f;
    [SerializeField] private float sensitivityY = 10f;

    [Header("Clamp")]
    [SerializeField] private float minPitch = -50f;
    [SerializeField] private float maxPitch = 100f;

    [Header("Follow")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float autoAlignSpeed = 5f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 50f;

    private float yaw;
    private float pitch;

    private void LateUpdate()
    {
        if (target == null || playerTransform == null || InputHandler.instance == null)
            return;

        Vector2 lookInput = InputHandler.instance.lookInput;

        yaw += lookInput.x * sensitivityX * Time.deltaTime;
        pitch -= lookInput.y * sensitivityY * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Vector2 moveInput = InputHandler.instance.moveInput;
        if (moveInput.sqrMagnitude > 0.01f)
        {
            float playerYaw = playerTransform.eulerAngles.y;
            yaw = Mathf.LerpAngle(yaw, playerYaw, autoAlignSpeed * Time.deltaTime);
        }

        float scroll = InputHandler.instance.zoomInput;
        if (scroll != 0f)
        {
            distance -= scroll * 0.01f * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 targetPosition = target.position + Vector3.up * height;
        Vector3 desiredPosition = targetPosition + rotation * new Vector3(0f, 0f, -distance);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.LookAt(targetPosition);
    }
}