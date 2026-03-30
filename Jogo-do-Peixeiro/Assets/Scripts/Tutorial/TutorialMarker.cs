using UnityEngine;

public class TutorialMarker : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Offset")]
    [SerializeField] private float heightOffset = 1.5f;

    [Header("Animation")]
    [SerializeField] private float rotateSpeed = 50f;
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatSpeed = 2f;

    private Vector3 basePosition;

    private void Start()
    {
        if (target != null)
            basePosition = target.position;
        else
            basePosition = transform.position;
    }

    private void Update()
    {
        UpdatePosition();
        Animate();
    }

    private void UpdatePosition()
    {
        if (target != null)
        {
            basePosition = target.position;
        }

        float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

        transform.position = basePosition + Vector3.up * (heightOffset + floatOffset);
    }

    private void Animate()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    public void SetTarget(Transform _target)
    {
        target = _target;
    }
}