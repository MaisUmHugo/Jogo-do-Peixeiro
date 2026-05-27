using UnityEngine;

public class SpinFan : MonoBehaviour
{
    enum Angle { X, Y, Z }

    [SerializeField] private Angle rotationAxis = Angle.Z;
    public float rotationSpeed = 720f;
    [SerializeField] private bool useLocalAxis = true;
    [SerializeField] private bool invertRotation;

    [Header("Boat Speed")]
    [SerializeField] private bool scaleWithBoatSpeed = true;
    [SerializeField] private Rigidbody boatRigidbody;
    [SerializeField] private BoatMotor boatMotor;
    [Tooltip("Se ficar 0, usa o Max Speed do BoatMotor quando existir.")]
    [SerializeField, Min(0f)] private float speedForMaxRotation;
    [SerializeField, Min(0f)] private float speedSmoothing = 8f;

    private float currentRotationSpeed;

    private void Awake()
    {
        ResolveBoatReferences();
    }

    void Update()
    {
        float targetRotationSpeed = GetTargetRotationSpeed();
        float followT = speedSmoothing > 0f
            ? 1f - Mathf.Exp(-speedSmoothing * Time.deltaTime)
            : 1f;

        currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, targetRotationSpeed, followT);

        if (Mathf.Abs(currentRotationSpeed) <= 0.01f)
            return;

        float signedRotationSpeed = invertRotation ? -currentRotationSpeed : currentRotationSpeed;
        Space rotationSpace = useLocalAxis ? Space.Self : Space.World;
        transform.Rotate(GetRotationAxis(rotationAxis), signedRotationSpeed * Time.deltaTime, rotationSpace);
    }

    private float GetTargetRotationSpeed()
    {
        if (!scaleWithBoatSpeed)
            return rotationSpeed;

        ResolveBoatReferences();

        if (boatRigidbody == null)
            return 0f;

        float currentSpeed = boatRigidbody.linearVelocity.magnitude;
        float maxReferenceSpeed = speedForMaxRotation;

        if (maxReferenceSpeed <= 0f && boatMotor != null)
            maxReferenceSpeed = boatMotor.maxSpeed;

        float speedPercent = Mathf.Clamp01(currentSpeed / Mathf.Max(0.01f, maxReferenceSpeed));
        return rotationSpeed * speedPercent;
    }

    private void ResolveBoatReferences()
    {
        if (boatMotor == null)
            boatMotor = GetComponentInParent<BoatMotor>();

        if (boatRigidbody == null)
        {
            if (boatMotor != null)
                boatRigidbody = boatMotor.GetComponent<Rigidbody>();
            else
                boatRigidbody = GetComponentInParent<Rigidbody>();
        }
    }

    private Vector3 GetRotationAxis(Angle angle)
    {
        switch (angle)
        {
            case Angle.X:
                return Vector3.right;
            case Angle.Y: 
                return Vector3.up; 
            case Angle.Z: 
                return Vector3.forward;
        }
        return Vector3.zero;
    }
}
