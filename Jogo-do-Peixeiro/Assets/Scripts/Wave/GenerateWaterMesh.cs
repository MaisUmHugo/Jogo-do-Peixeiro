using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class GenerateWaterMesh : MonoBehaviour
{
    public int resolution = 100; // quantidade de subdivisões
    public float size = 50f;     // tamanho do tile

    void Awake()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices =
            new Vector3[(resolution + 1) * (resolution + 1)];

        int[] triangles =
            new int[resolution * resolution * 6];

        float halfSize = size * 0.5f;

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                float percentX = x / (float)resolution;
                float percentZ = z / (float)resolution;

                vertices[z * (resolution + 1) + x] =
                    new Vector3(
                        percentX * size - halfSize,
                        0,
                        percentZ * size - halfSize
                    );
            }
        }

        int index = 0;

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = z * (resolution + 1) + x;

                triangles[index++] = i;
                triangles[index++] = i + resolution + 1;
                triangles[index++] = i + 1;

                triangles[index++] = i + 1;
                triangles[index++] = i + resolution + 1;
                triangles[index++] = i + resolution + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}