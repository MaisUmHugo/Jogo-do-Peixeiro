using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform playerTransform;

    [Header("Offset")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float height = 1.5f;

    [Header("Sensitivity")]
    [SerializeField] private float sensitivityX = 2f;
    [SerializeField] private float sensitivityY = 2f;

    [Header("Clamp")]
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Follow")]
    [SerializeField] private float followSpeed = 10f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 2f;

    private float yaw;
    private float pitch;

    private void Start()
    {
        Vector3 currentRotation = transform.eulerAngles;
        yaw = currentRotation.y;
        pitch = currentRotation.x;

        if (pitch > 180f)
            pitch -= 360f;
    }

    private void LateUpdate()
    {
        if (target == null || playerTransform == null || InputHandler.instance == null)
            return;

        if (GameManager.instance != null)
        {
            if (GameManager.instance.currentState == GameManager.GameState.InUI ||
                GameManager.instance.currentState == GameManager.GameState.Paused)
                return;
        }

        Vector2 lookInput = InputHandler.instance.lookInput;

        yaw += lookInput.x * sensitivityX;
        pitch -= lookInput.y * sensitivityY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        float scroll = InputHandler.instance.zoomInput;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * zoomSpeed * Time.deltaTime;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 targetPosition = target.position + Vector3.up * height;
        Vector3 desiredPosition = targetPosition + rotation * new Vector3(0f, 0f, -distance);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.LookAt(targetPosition);
    }
}