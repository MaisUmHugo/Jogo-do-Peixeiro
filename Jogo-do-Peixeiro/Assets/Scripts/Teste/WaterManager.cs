using UnityEngine;
using UnityEngine.LightTransport;
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WaterManager : MonoBehaviour
{
        private MeshFilter meshFilter;
        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
        }
        private void Update()
        {
            Vector3[] vertices = meshFilter.mesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                float worldX = transform.position.x + vertices[i].x;
                float worldZ = transform.position.z + vertices[i].z;

                vertices[i].y = WaveManager.instance.GetWaveHeight(worldX, worldZ);

            }
                meshFilter.mesh.vertices = vertices;
                meshFilter.mesh.RecalculateNormals();
        }
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || WaveManager.instance == null)
            return;

        Gizmos.color = Color.cyan;

        float size = 20f;
        float step = 1f;

        for (float x = -size; x <= size; x += step)
        {
            for (float z = -size; z <= size; z += step)
            {
                float worldX = transform.position.x + x;
                float worldZ = transform.position.z + z;

                float y = WaveManager.instance.GetWaveHeight(worldX, worldZ);

                Gizmos.DrawSphere(
                    new Vector3(worldX, y, worldZ),
                    0.05f
                );
            }
        }
    }
}
