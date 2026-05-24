using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class GenerateWaterMesh : MonoBehaviour
{
    [Header("Mesh")]
    public int resolution = 100;
    public float size = 100f;

    [Header("Materiais")]
    public Material waterMaterial;
    public Material darkWaterMaterial;

    [Header("Lava Opcional")]
    public bool useLavaMaterial;
    public Material lavaMaterial;

    [Header("Referencia")]
    public Transform referenceObject;

    [Header("Escurecimento")]
    public string boatTag = "Boat";
    public float darkStartDistance = 10f;
    public float darkMaxDistance = 100f;
    [Range(0f, 1f)] public float deepAreaThreshold = 0.35f;

    private Material runtimeDarkMaterial;
    private Transform boat;

    private void Awake()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
        Vector2[] uvs = new Vector2[(resolution + 1) * (resolution + 1)];
        int[] triangles = new int[resolution * resolution * 6];

        float halfSize = size * 0.5f;

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                float percentX = x / (float)resolution;
                float percentZ = z / (float)resolution;

                int i = z * (resolution + 1) + x;

                vertices[i] = new Vector3(
                    percentX * size - halfSize,
                    0,
                    percentZ * size - halfSize
                );

                uvs[i] = new Vector2(percentX, percentZ);
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

        if (ShouldUseDarkWaterMaterial())
            runtimeDarkMaterial = new Material(darkWaterMaterial);

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Material surfaceMaterial = GetSurfaceMaterial();

        if (runtimeDarkMaterial != null)
            meshRenderer.materials = new Material[] { surfaceMaterial, runtimeDarkMaterial };
        else
            meshRenderer.material = surfaceMaterial;

        GetComponent<MeshCollider>().sharedMesh = mesh;

        GameObject boatObject = GameObject.FindGameObjectWithTag(boatTag);

        if (boatObject != null)
            boat = boatObject.transform;
    }

    private void Start()
    {
        if (!ShouldUseDarkWaterMaterial())
            return;

        runtimeDarkMaterial = new Material(darkWaterMaterial);

        runtimeDarkMaterial.SetFloat("_Surface", 1);
        runtimeDarkMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        runtimeDarkMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        runtimeDarkMaterial.SetInt("_ZWrite", 0);
        runtimeDarkMaterial.DisableKeyword("_ALPHATEST_ON");
        runtimeDarkMaterial.EnableKeyword("_ALPHABLEND_ON");
        runtimeDarkMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        runtimeDarkMaterial.renderQueue = 3000;

        GetComponent<MeshRenderer>().materials = new Material[]
        {
            GetSurfaceMaterial(),
            runtimeDarkMaterial
        };
    }

    private void Update()
    {
        if (boat == null || runtimeDarkMaterial == null)
            return;

        float darkness = GetDarknessAtWorldPosition(boat.position);

        if (runtimeDarkMaterial.HasProperty("_BaseColor"))
        {
            Color baseColor = runtimeDarkMaterial.GetColor("_BaseColor");
            baseColor.a = darkness;
            runtimeDarkMaterial.SetColor("_BaseColor", baseColor);
        }
    }

    public float GetDarknessAtWorldPosition(Vector3 _worldPosition)
    {
        Transform reference = referenceObject != null ? referenceObject : transform;
        float zDistance = _worldPosition.z - reference.position.z;
        return Mathf.InverseLerp(darkStartDistance, darkMaxDistance, zDistance);
    }

    public bool IsDeepWater(Vector3 _worldPosition)
    {
        return GetDarknessAtWorldPosition(_worldPosition) >= deepAreaThreshold;
    }

    private bool ShouldUseLavaMaterial()
    {
        return useLavaMaterial && lavaMaterial != null;
    }

    private bool ShouldUseDarkWaterMaterial()
    {
        return !ShouldUseLavaMaterial() && darkWaterMaterial != null;
    }

    private Material GetSurfaceMaterial()
    {
        return ShouldUseLavaMaterial() ? lavaMaterial : waterMaterial;
    }
}
