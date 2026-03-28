using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatMotor : MonoBehaviour
{
    public float engineForce = 30f;
    public float turnForce = 12f;
    public float maxSpeed = 12f;
    public float stabilization = 2f;
    public float lateralDrag = 2f; // reduz drift lateral

    private Rigidbody rb;

    private Vector2 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // lê input do InputHandler 
    void Update()
    {
        if (GameManager.instance == null) return;
        if (InputHandler.instance == null) return;

        // só recebe input quando estiver no barco
        if (GameManager.instance.currentState != GameManager.GameState.OnBoat)
        {
            input = Vector2.zero;
            return;
        }

        input = InputHandler.instance.moveInput;
    }

    void FixedUpdate()
    {
        if (WaveManager.instance == null) return;
        if (GameManager.instance == null) return;

        // só movimenta quando estiver no barco
        if (GameManager.instance.currentState != GameManager.GameState.OnBoat)
            return;

        Vector3 forward = transform.forward;

        float moveInput = input.y; // W/S
        float turnInput = input.x; // A/D

        // força do motor
        if (rb.linearVelocity.magnitude < maxSpeed)
        {
            rb.AddForce(forward * moveInput * engineForce, ForceMode.Acceleration);
        }

        // giro
        rb.AddTorque(Vector3.up * turnInput * turnForce, ForceMode.Acceleration);

        // reduz deslizamento lateral
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x *= 1f / (1f + lateralDrag * Time.fixedDeltaTime);
        rb.linearVelocity = transform.TransformDirection(localVel);

        // estabilização leve
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

        if (flatForward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatForward, Vector3.up);

            rb.MoveRotation(
                Quaternion.Slerp(
                    rb.rotation,
                    targetRot,
                    stabilization * Time.fixedDeltaTime
                )
            );
        }
    }
}