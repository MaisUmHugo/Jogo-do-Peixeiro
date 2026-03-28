using UnityEngine;

public class BoatSetup : MonoBehaviour
{
    public Rigidbody rb;
    public Vector3 offset;

    void Start()
    {
        rb.centerOfMass = offset;
    }

    void OnDrawGizmos()
    {
        if (TryGetComponent(out Rigidbody rb))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(rb.centerOfMass), 0.1f);
        }
    }
}