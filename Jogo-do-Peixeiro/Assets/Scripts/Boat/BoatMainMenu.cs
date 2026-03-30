using UnityEngine;

public class BoatMainMenu : MonoBehaviour
{
    private Rigidbody rb;

    [SerializeField] private float speed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        rb.AddForce(transform.forward * speed, ForceMode.Acceleration);
    }
}