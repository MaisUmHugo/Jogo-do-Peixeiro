using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[DefaultExecutionOrder(10)]
public class WaterManager : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Vector3[] baseVertices;
    private Vector3[] vertices;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();

        meshFilter.mesh = Instantiate(meshFilter.mesh);

        baseVertices = meshFilter.mesh.vertices;
        vertices = new Vector3[baseVertices.Length];
    }

    void Update()
    {
        if (WaveManager.instance == null)
            return;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos =
                transform.TransformPoint(baseVertices[i]);

            vertices[i] = baseVertices[i];

            vertices[i].y =
                WaveManager.instance.GetWaveHeight(
                    worldPos.x,
                    worldPos.z
                );
        }

        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.RecalculateNormals();
    }
}