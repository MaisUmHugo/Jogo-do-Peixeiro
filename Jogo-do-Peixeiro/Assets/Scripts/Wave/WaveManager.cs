using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager instance;

    public float amplitude = 1f;
    public float length = 5f;
    public float speed = 1f;

    public Vector2 direction = new Vector2(1, 0);

    private float offset;

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        offset += Time.deltaTime * speed;
    }

    public float GetWaveHeight(float x, float z)
    {
        Vector2 dir = direction.normalized;

        float frequency = 1f / length;

        float waveCoord =
            (x * dir.x + z * dir.y) * frequency;

        return amplitude * Mathf.Sin(waveCoord + offset);
    }
}