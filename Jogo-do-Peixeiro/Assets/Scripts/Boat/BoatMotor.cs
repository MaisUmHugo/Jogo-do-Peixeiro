using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatMotor : MonoBehaviour
{
    public float engineForce = 30f;
    public float turnForce = 12f;
    public float maxSpeed = 12f;
    public float stabilization = 2f;
    public float lateralDrag = 2f;
    [SerializeField] private bool anchorWhileFishing = true;
    [SerializeField] private bool requireNeutralInputAfterFishing = true;
    [SerializeField] private float neutralInputThreshold = 0.15f;

    private Rigidbody rb;
    private Vector2 input;
    private Vector3 anchorPosition;
    private Quaternion anchorRotation;
    private bool wasKinematicBeforeAnchor;
    private bool isAnchored;
    private bool isWaitingForNeutralInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
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

        // Força do motor com limite de velocidade
        if (rb.linearVelocity.magnitude < maxSpeed)
        {
            rb.AddForce(forward * moveInput * engineForce, ForceMode.Acceleration);
        }

        // Giro
        rb.AddTorque(Vector3.up * turnInput * turnForce, ForceMode.Acceleration);

        // Reduz deslizamento lateral (Drift)
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
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
