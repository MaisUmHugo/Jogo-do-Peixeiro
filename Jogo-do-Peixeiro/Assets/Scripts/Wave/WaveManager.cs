using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager instance;

    public float amplitude, length, speed, offset;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("já existe, destruindo objeto");
            Destroy(this);
        }
    }
    private void Update()
    {
        offset += Time.deltaTime * speed;
    }

    public float GetWaveHeight(float x, float z)
    {
        return amplitude * Mathf.Sin((x + z) / length + offset);
    }
}
