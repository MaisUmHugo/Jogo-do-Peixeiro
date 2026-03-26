using UnityEngine;

public class WaterGrid : MonoBehaviour
{
    public GameObject waterTilePrefab;
    public Transform player;

    public int gridSize = 10;

    void Start()
    {
        GameObject temp = Instantiate(waterTilePrefab);

        float sizeX =
            temp.GetComponent<MeshFilter>().sharedMesh.bounds.size.x
            * temp.transform.localScale.x;

        float sizeZ =
            temp.GetComponent<MeshFilter>().sharedMesh.bounds.size.z
            * temp.transform.localScale.z;

        Destroy(temp);

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                Vector3 pos = new Vector3(
                    x * sizeX,
                    0,
                    z * sizeZ
                );

                GameObject tile =
                    Instantiate(waterTilePrefab, pos, Quaternion.identity, transform);

                // 🔹 passa o player para o tile
                //tile.GetComponent<WaterManager>().player = player;
            }
        }
    }
}