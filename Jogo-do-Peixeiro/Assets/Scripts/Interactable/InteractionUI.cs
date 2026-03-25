using UnityEngine;

public class InteractionUI : MonoBehaviour
{
    [SerializeField] private RectTransform interactButton;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private float maxDistance = 4f;

    private Camera mainCamera;
    private Transform target;
    private Transform promptPoint;
    private Transform playerTransform;

    private void Awake()
    {
        mainCamera = Camera.main;
        Hide();
    }

    private void Update()
    {
        if (GameManager.instance != null &&
            GameManager.instance.currentState != GameManager.GameState.OnFoot)
        {
            interactButton.gameObject.SetActive(false);
            return;
        }

        if (target == null || mainCamera == null || playerTransform == null)
            return;

        float distanceToTarget = Vector3.Distance(playerTransform.position, target.position);

        if (distanceToTarget > maxDistance)
        {
            interactButton.gameObject.SetActive(false);
            return;
        }

        Vector3 worldPosition = promptPoint != null ? promptPoint.position : target.position + worldOffset;
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        if (screenPosition.z <= 0f)
        {
            interactButton.gameObject.SetActive(false);
            return;
        }

        interactButton.gameObject.SetActive(true);
        interactButton.position = screenPosition;
    }

    public void Show(Transform _target, Transform _playerTransform, Transform _promptPoint = null)
    {
        target = _target;
        playerTransform = _playerTransform;
        promptPoint = _promptPoint;

        interactButton.gameObject.SetActive(true);
    }

    public void Hide()
    {
        target = null;
        playerTransform = null;
        promptPoint = null;

        if (interactButton != null)
            interactButton.gameObject.SetActive(false);
    }
}