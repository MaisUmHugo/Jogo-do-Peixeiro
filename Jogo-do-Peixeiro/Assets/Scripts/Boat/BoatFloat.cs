using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatFloat : MonoBehaviour
{
    public Transform[] floatPoints; // pontos do casco que flutuam
    public float floatStrength = 15f;
    public float waterDrag = 1f;
    public float waterAngularDrag = 1f;
    public float heightOffset = 0.2f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (WaveManager.instance == null) return;

        int floatingPoints = 0;

        foreach (Transform point in floatPoints)
        {
            Vector3 pos = point.position;

            float waterHeight = WaveManager.instance.GetWaveHeight(pos.x, pos.z);

            if (pos.y < waterHeight + heightOffset)
            {
                floatingPoints++;

                float depth = (waterHeight + heightOffset) - pos.y;

                Vector3 force = Vector3.up * floatStrength * depth;

                rb.AddForceAtPosition(force, pos, ForceMode.Acceleration);
            }
        }

        // simula resistência da água
        if (floatingPoints > 0)
        {
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterAngularDrag;
        }
        else
        {
            rb.linearDamping = 0;
            rb.angularDamping = 0;
        }
    }
}