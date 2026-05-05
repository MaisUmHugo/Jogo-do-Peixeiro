using UnityEngine;
using UnityEngine.Serialization;

public class TutorialMarker : MonoBehaviour
{
    [Header("Animation")]
    [FormerlySerializedAs("rotateSpeed")]
    [SerializeField] private float _rotateSpeed = 50f;

    [FormerlySerializedAs("floatAmplitude")]
    [SerializeField] private float _floatAmplitude = 0.2f;

    [FormerlySerializedAs("floatSpeed")]
    [SerializeField] private float _floatSpeed = 2f;

    private Vector3 _basePosition;

    private void OnEnable()
    {
        _basePosition = transform.position;
    }

    private void Update()
    {
        AnimateFloat();
        AnimateRotation();
    }

    private void AnimateFloat()
    {
        float floatOffset = Mathf.Sin(Time.time * _floatSpeed) * _floatAmplitude;
        transform.position = _basePosition + Vector3.up * floatOffset;
    }

    private void AnimateRotation()
    {
        transform.Rotate(Vector3.up, _rotateSpeed * Time.deltaTime, Space.World);
    }
}
