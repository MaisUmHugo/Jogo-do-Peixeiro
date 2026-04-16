using UnityEngine;

public class BoatMainMenu : MonoBehaviour
{
    private Rigidbody rb;

    [SerializeField] private float speed;
    [SerializeField] private Transform startPos;
    [SerializeField] private Transform endPos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        rb.AddForce(transform.forward * speed, ForceMode.Acceleration);

        if (transform.position.x <= endPos.position.x) {
        
            transform.position = startPos.position;        
        }
    }
}