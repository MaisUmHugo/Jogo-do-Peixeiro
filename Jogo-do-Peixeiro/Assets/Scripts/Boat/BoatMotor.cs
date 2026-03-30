using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatMotor : MonoBehaviour
{
    public float engineForce = 30f;
    public float turnForce = 12f;
    public float maxSpeed = 12f;
    public float stabilization = 2f;
    public float lateralDrag = 2f;

    private Rigidbody rb;
    private Vector2 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (GameManager.instance == null || InputHandler.instance == null) return;

        // Só recebe input quando estiver navegando (OnBoat)
        // Se estiver pescando (Fishing), o input é zerado
        if (GameManager.instance.currentState == GameManager.GameState.OnBoat)
        {
            input = InputHandler.instance.moveInput;
        }
        else
        {
            input = Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        if (WaveManager.instance == null || GameManager.instance == null) return;

        // Se estiver pescando, paramos o barco gradualmente ou imediatamente
        if (GameManager.instance.currentState == GameManager.GameState.Fishing)
        {
            StopBoat();
            ApplyStabilization(); // Mantém o barco reto enquanto pesca
            return;
        }

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
}