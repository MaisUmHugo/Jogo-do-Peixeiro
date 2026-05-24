using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatMotor : MonoBehaviour
{
    private const string BoatWaterAudioSourceName = "BoatWaterAudioSource";

    public float engineForce = 30f;
    public float turnForce = 12f;
    public float maxSpeed = 12f;
    public float stabilization = 2f;
    public float lateralDrag = 2f;
    [SerializeField] private bool anchorWhileFishing = true;
    [SerializeField] private bool requireNeutralInputAfterFishing = true;
    [SerializeField] private float neutralInputThreshold = 0.15f;

    [Header("Audio")]
    [SerializeField, InspectorName("Boat Water Loop SFX")] private AudioClip boatWaterLoopSfx;
    [SerializeField] private AudioSource boatWaterAudioSource;
    [SerializeField, Range(0f, 1f), InspectorName("Boat Water Min SFX Volume")] private float boatWaterMinVolume = 0.15f;
    [SerializeField, Range(0f, 1f), InspectorName("Boat Water Max SFX Volume")] private float boatWaterMaxVolume = 0.65f;
    [SerializeField] private float boatWaterMinPitch = 0.9f;
    [SerializeField] private float boatWaterMaxPitch = 1.12f;
    [SerializeField] private float boatWaterSpeedForMaxVolume = 8f;
    [SerializeField, Range(0f, 1f)] private float boatWaterSpatialBlend = 1f;

    private Rigidbody rb;
    private Vector2 input;
    private Vector3 anchorPosition;
    private Quaternion anchorRotation;
    private bool wasKinematicBeforeAnchor;
    private bool isAnchored;
    private bool isWaitingForNeutralInput;
    private float baseEngineForce;
    private float baseMaxSpeed;
    private bool hasCapturedBaseSpeedValues;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        CaptureBaseSpeedValues();
        SetupBoatWaterAudioSource();
    }

    public void SetSpeedUpgradeMultiplier(float _multiplier)
    {
        CaptureBaseSpeedValues();

        float multiplier = Mathf.Max(0.01f, _multiplier);
        engineForce = baseEngineForce * multiplier;
        maxSpeed = baseMaxSpeed * multiplier;
    }

    public void ResetMotorState(bool _waitForNeutralInput = true)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        input = Vector2.zero;
        isAnchored = false;
        isWaitingForNeutralInput = _waitForNeutralInput;

        if (rb == null)
            return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep();
    }

    void Update()
    {
        UpdateBoatWaterSfx();

        if (GameManager.instance == null || InputHandler.instance == null) return;

        // Só recebe input quando estiver navegando (OnBoat)
        // Se estiver pescando (Fishing), o input é zerado
        if (GameManager.instance.currentState == GameManager.GameState.OnBoat)
        {
            Vector2 rawInput = InputHandler.instance.moveInput;

            if (isWaitingForNeutralInput)
            {
                input = Vector2.zero;

                if (rawInput.magnitude <= neutralInputThreshold)
                    isWaitingForNeutralInput = false;

                return;
            }

            input = rawInput;
        }
        else
        {
            input = Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        if (GameManager.instance == null) return;

        if (ShouldAnchorBoat())
        {
            AnchorBoat();
            return;
        }

        ReleaseAnchor();

        if (WaveManager.instance == null) return;

        // Se não estiver no barco, não faz nada
        if (GameManager.instance.currentState != GameManager.GameState.OnBoat)
            return;

        ApplyMovement();
        ApplyStabilization();
    }

    private void ApplyMovement()
    {
        Vector3 forward = transform.forward;

        float moveInput = input.y;
        float turnInput = input.x;

        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float forwardSpeed = Mathf.Abs(localVel.z);

        float speedPercent = Mathf.Clamp01(forwardSpeed / maxSpeed);

        if (Mathf.Abs(turnInput) > 0.05f)
        {
            moveInput += Mathf.Abs(turnInput) * 0.35f;
        }

        moveInput = Mathf.Clamp(moveInput, -1f, 1f);

        if (rb.linearVelocity.magnitude < maxSpeed)
        {
            rb.AddForce(forward * moveInput * engineForce, ForceMode.Acceleration);
        }

        float turnStrength = turnForce * Mathf.Lerp(0.2f, 1f, speedPercent);

        rb.AddTorque(Vector3.up * turnInput * turnStrength, ForceMode.Acceleration);

        localVel.x *= 1f / (1f + lateralDrag * Time.fixedDeltaTime);

        rb.linearVelocity = transform.TransformDirection(localVel);
    }

    private void ApplyStabilization()
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

        if (flatForward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatForward, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, stabilization * Time.fixedDeltaTime));
        }
    }

    private void StopBoat()
    {
        // Reduz a velocidade linear e angular rapidamente para o barco não ficar "fugindo" enquanto pesca
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);

        // Se a velocidade for muito baixa, mata ela de vez
        if (rb.linearVelocity.magnitude < 0.1f) rb.linearVelocity = Vector3.zero;
        if (rb.angularVelocity.magnitude < 0.1f) rb.angularVelocity = Vector3.zero;
    }

    private bool ShouldAnchorBoat()
    {
        if (!anchorWhileFishing)
            return false;

        if (GameManager.instance.currentState == GameManager.GameState.Fishing)
            return true;

        return FishingManager.instance != null && FishingManager.instance.IsFishing;
    }

    private void CaptureBaseSpeedValues()
    {
        if (hasCapturedBaseSpeedValues)
            return;

        baseEngineForce = engineForce;
        baseMaxSpeed = maxSpeed;
        hasCapturedBaseSpeedValues = true;
    }

    private void SetupBoatWaterAudioSource()
    {
        if (boatWaterAudioSource == null)
        {
            Transform sourceTransform = transform.Find(BoatWaterAudioSourceName);

            if (sourceTransform != null)
                boatWaterAudioSource = sourceTransform.GetComponent<AudioSource>();
        }

        if (boatWaterAudioSource == null)
        {
            GameObject sourceObject = new GameObject(BoatWaterAudioSourceName);
            sourceObject.transform.SetParent(transform, false);
            boatWaterAudioSource = sourceObject.AddComponent<AudioSource>();
        }

        boatWaterAudioSource.playOnAwake = false;
        boatWaterAudioSource.loop = true;
        boatWaterAudioSource.spatialBlend = boatWaterSpatialBlend;
        AudioManager.Instance?.ApplySfxOutput(boatWaterAudioSource);
    }

    private void UpdateBoatWaterSfx()
    {
        if (boatWaterLoopSfx == null)
        {
            StopBoatWaterSfx();
            return;
        }

        if (boatWaterAudioSource == null)
            SetupBoatWaterAudioSource();

        if (boatWaterAudioSource == null)
            return;

        AudioManager.Instance?.ApplySfxOutput(boatWaterAudioSource);

        bool shouldPlay =
            GameManager.instance != null &&
            (GameManager.instance.currentState == GameManager.GameState.OnBoat ||
             GameManager.instance.currentState == GameManager.GameState.Fishing);

        if (!shouldPlay)
        {
            StopBoatWaterSfx();
            return;
        }

        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
        float speedPercent = Mathf.Clamp01(speed / Mathf.Max(0.01f, boatWaterSpeedForMaxVolume));

        if (boatWaterAudioSource.clip != boatWaterLoopSfx)
            boatWaterAudioSource.clip = boatWaterLoopSfx;

        boatWaterAudioSource.volume = Mathf.Lerp(boatWaterMinVolume, boatWaterMaxVolume, speedPercent);
        boatWaterAudioSource.pitch = Mathf.Lerp(boatWaterMinPitch, boatWaterMaxPitch, speedPercent);
        boatWaterAudioSource.spatialBlend = boatWaterSpatialBlend;

        if (!boatWaterAudioSource.isPlaying)
            boatWaterAudioSource.Play();
    }

    private void StopBoatWaterSfx()
    {
        if (boatWaterAudioSource != null && boatWaterAudioSource.isPlaying)
            boatWaterAudioSource.Stop();
    }

    private void AnchorBoat()
    {
        if (!isAnchored)
        {
            anchorPosition = rb.position;
            anchorRotation = rb.rotation;
            wasKinematicBeforeAnchor = rb.isKinematic;
            isAnchored = true;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        rb.MovePosition(anchorPosition);
        rb.MoveRotation(anchorRotation);
    }

    private void ReleaseAnchor()
    {
        if (!isAnchored)
            return;

        rb.position = anchorPosition;
        rb.rotation = anchorRotation;
        rb.isKinematic = wasKinematicBeforeAnchor;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        input = Vector2.zero;
        isWaitingForNeutralInput = requireNeutralInputAfterFishing;
        isAnchored = false;
    }
}
