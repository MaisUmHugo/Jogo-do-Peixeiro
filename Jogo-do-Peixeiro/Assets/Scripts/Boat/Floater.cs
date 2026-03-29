using UnityEngine;
using System.Collections.Generic;

public class Floater : MonoBehaviour
{
    public Rigidbody rb;

    [Header("Buoyancy")]
    public float depthBeforeSubmerged = 0.5f;
    public float displacementAmount = 1.25f;

    [Header("Water Resistance")]
    public float waterDrag = 1.2f;
    public float waterAngularDrag = 2.2f;

    [Header("Safety")]
    public float maxSubmersionDepth = 0.4f;
    public float emergencyLift = 2.8f;

    [Header("Alignment")]
    public float heightOffset = 0.0f;

    [Header("Contact Stabilizer")]
    public float outOfWaterWeight = 0.6f;

    static List<Floater> allFloaters = new List<Floater>();

    float submersion;
    float depth;

    void Awake()
    {
        if (rb == null)
            rb = GetComponentInParent<Rigidbody>();
    }

    void OnEnable()
    {
        allFloaters.Add(this);
    }

    void OnDisable()
    {
        allFloaters.Remove(this);
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        if (WaveManager.instance == null) return;

        float waveHeight = WaveManager.instance.GetWaveHeight(
            transform.position.x,
            transform.position.z
        );

        float floaterLevel = transform.position.y + heightOffset;
        depth = waveHeight - floaterLevel;

        submersion = Mathf.Clamp01(depth / depthBeforeSubmerged);

        int submergedCount = 0;

        foreach (var f in allFloaters)
        {
            if (f.submersion > 0)
                submergedCount++;
        }

        if (depth > 0)
        {
            float floatersRatio =
                (float)submergedCount / allFloaters.Count;

            float depthForceMultiplier =
                1f + Mathf.Clamp01(depth / maxSubmersionDepth) * 2.0f;

            float smoothSubmersion =
                submersion * submersion * depthForceMultiplier;

            float distributedMass =
                rb.mass * floatersRatio;

            float buoyancyForce =
                (distributedMass * Mathf.Abs(Physics.gravity.y) / allFloaters.Count)
                * smoothSubmersion
                * displacementAmount;

            rb.AddForceAtPosition(
                Vector3.up * buoyancyForce,
                transform.position,
                ForceMode.Force
            );

            if (depth > maxSubmersionDepth)
            {
                float extraDepth = depth - maxSubmersionDepth;

                float emergencyForce =
                    extraDepth * emergencyLift * rb.mass;

                rb.AddForceAtPosition(
                    Vector3.up * emergencyForce,
                    transform.position,
                    ForceMode.Force
                );
            }

            rb.AddForce(
                -rb.GetPointVelocity(transform.position)
                * waterDrag
                * submersion,
                ForceMode.Force
            );

            rb.AddTorque(
                -rb.angularVelocity
                * waterAngularDrag
                * submersion,
                ForceMode.Force
            );
        }
        else
        {
            // força leve para baixo para manter contato com a água
            float downwardForce =
                rb.mass * Mathf.Abs(Physics.gravity.y) * outOfWaterWeight / allFloaters.Count;

            rb.AddForceAtPosition(
                Vector3.down * downwardForce,
                transform.position,
                ForceMode.Force
            );
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || WaveManager.instance == null)
            return;

        float waveHeight = WaveManager.instance.GetWaveHeight(
            transform.position.x,
            transform.position.z
        );

        Vector3 floaterPos =
            transform.position + Vector3.up * heightOffset;

        Vector3 waterPoint = new Vector3(
            floaterPos.x,
            waveHeight,
            floaterPos.z
        );

        Vector3 maxDepthPoint = new Vector3(
            floaterPos.x,
            waveHeight - maxSubmersionDepth,
            floaterPos.z
        );

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(floaterPos, waterPoint);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(waterPoint, 0.12f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(maxDepthPoint, 0.12f);

        Gizmos.DrawLine(waterPoint, maxDepthPoint);
    }
}