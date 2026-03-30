using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GenerateWaterMesh : MonoBehaviour
{
    public int resolution;
    public float size;

    public Material waterMaterial;

    void Awake()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices =
            new Vector3[(resolution + 1) * (resolution + 1)];

        Vector2[] uvs =
            new Vector2[(resolution + 1) * (resolution + 1)];

        int[] triangles =
            new int[resolution * resolution * 6];

        float halfSize = size * 0.5f;

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                float percentX = x / (float)resolution;
                float percentZ = z / (float)resolution;

                int i = z * (resolution + 1) + x;

                vertices[i] =
                    new Vector3(
                        percentX * size - halfSize,
                        0,
                        percentZ * size - halfSize
                    );

                uvs[i] =
                    new Vector2(percentX, percentZ);
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
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = waterMaterial;
    }
}