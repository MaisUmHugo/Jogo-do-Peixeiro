using UnityEngine;

public class FishDirectionPull : MonoBehaviour
{
    public enum FishForceDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    [Header("Settings")]
    [SerializeField] private bool _useDirectionalPull = true;
    [SerializeField, Range(0f, 1f)] private float _startInfluenceProgress = 0.65f;
    [SerializeField, Range(0.01f, 1f)] private float _fadeInRange = 0.25f;
    [SerializeField] private float _directionChangeInterval = 2f;
    [SerializeField] private float _inputThreshold = 0.35f;

    [Header("Progress Modifier")]
    [SerializeField] private float _correctPullProgressSpeed = 0.12f;
    [SerializeField] private float _wrongPullProgressPenalty = 0.1f;
    [SerializeField] private float _noInputProgressPenalty = 0.03f;

    public FishForceDirection CurrentFishDirection { get; private set; }
    public bool UseDirectionalPull => _useDirectionalPull;
    public bool IsPullActive { get; private set; }

    private float _directionTimer;

    public void StartPull()
    {
        IsPullActive = false;
        GenerateNewDirection();
    }

    public void StopPull()
    {
        IsPullActive = false;
        _directionTimer = 0f;
    }

    public float GetProgressModifier(Vector2 _input, float _progressNormalized)
    {
        if (!_useDirectionalPull)
            return 0f;

        float intensity = GetIntensityByProgress(_progressNormalized);
        IsPullActive = intensity > 0f;

        if (!IsPullActive)
            return 0f;

        _directionTimer -= Time.deltaTime;

        if (_directionTimer <= 0f)
            GenerateNewDirection();

        if (_input.magnitude < _inputThreshold)
            return -_noInputProgressPenalty * intensity;

        if (IsPullingOppositeDirection(_input))
            return _correctPullProgressSpeed * intensity;

        return -_wrongPullProgressPenalty * intensity;
    }

    private float GetIntensityByProgress(float _progressNormalized)
    {
        float endProgress = Mathf.Clamp01(_startInfluenceProgress + _fadeInRange);
        return Mathf.InverseLerp(_startInfluenceProgress, endProgress, _progressNormalized);
    }

    private void GenerateNewDirection()
    {
        int randomDirection = Random.Range(0, 4);
        CurrentFishDirection = (FishForceDirection)randomDirection;
        _directionTimer = _directionChangeInterval;
    }

    private bool IsPullingOppositeDirection(Vector2 _input)
    {
        switch (CurrentFishDirection)
        {
            case FishForceDirection.Left:
                return _input.x > _inputThreshold;

            case FishForceDirection.Right:
                return _input.x < -_inputThreshold;

            case FishForceDirection.Up:
                return _input.y < -_inputThreshold;

            case FishForceDirection.Down:
                return _input.y > _inputThreshold;

            default:
                return false;
        }
    }
}