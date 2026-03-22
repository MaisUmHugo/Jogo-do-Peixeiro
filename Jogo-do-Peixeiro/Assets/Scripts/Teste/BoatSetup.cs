using UnityEngine;

public class BoatSetup : MonoBehaviour
{
    public Rigidbody rb;

    void Start()
    {
        rb.centerOfMass = new Vector3(0, -2f, 0);
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