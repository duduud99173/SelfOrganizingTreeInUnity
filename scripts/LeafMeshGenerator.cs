using UnityEngine;

public class LeafMeshGenerator : MonoBehaviour
{
    [Header("叶子参数")]
    public float length = 0.3f;    // 叶子长度
    public float width = 0.15f;    // 叶子宽度
    public int segments = 5;       // 分段数（3-8即可）

    [Header("材质")]
    public Material leafMaterial;  // 叶子材质

    void Start()
    {
        GenerateLeaf();
    }

    // 在Inspector中右键菜单可直接生成
    [ContextMenu("生成叶子")]
    public void GenerateLeaf()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Simple Leaf";

        // 生成顶点（正面+背面）
        Vector3[] vertices = new Vector3[(segments + 1) * 4];
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            // 宽度随长度变化（中间宽，两端窄）
            float currentWidth = width * Mathf.Sin(t * Mathf.PI);

            // 正面顶点
            vertices[i * 2] = new Vector3(-currentWidth, 0, t * length);
            vertices[i * 2 + 1] = new Vector3(currentWidth, 0, t * length);

            // 背面顶点（Y轴偏移一点避免重叠）
            vertices[(segments + 1) * 2 + i * 2] = new Vector3(-currentWidth, -0.001f, t * length);
            vertices[(segments + 1) * 2 + i * 2 + 1] = new Vector3(currentWidth, -0.001f, t * length);
        }

        // 生成三角形
        int[] triangles = new int[segments * 4 * 3];
        int triIndex = 0;

        // 正面三角形
        for (int i = 0; i < segments; i++)
        {
            int v0 = i * 2;
            int v1 = i * 2 + 1;
            int v2 = (i + 1) * 2;
            int v3 = (i + 1) * 2 + 1;

            triangles[triIndex++] = v0;
            triangles[triIndex++] = v1;
            triangles[triIndex++] = v3;

            triangles[triIndex++] = v0;
            triangles[triIndex++] = v3;
            triangles[triIndex++] = v2;
        }

        // 背面三角形（顶点顺序反转，确保法线正确）
        int backOffset = (segments + 1) * 2;
        for (int i = 0; i < segments; i++)
        {
            int v0 = backOffset + i * 2;
            int v1 = backOffset + i * 2 + 1;
            int v2 = backOffset + (i + 1) * 2;
            int v3 = backOffset + (i + 1) * 2 + 1;

            triangles[triIndex++] = v1;
            triangles[triIndex++] = v0;
            triangles[triIndex++] = v2;

            triangles[triIndex++] = v1;
            triangles[triIndex++] = v2;
            triangles[triIndex++] = v3;
        }

        // 设置UV坐标（用于贴图）
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            // 正面UV
            uvs[i * 2] = new Vector2(0, t);
            uvs[i * 2 + 1] = new Vector2(0.5f, t);
            // 背面UV
            uvs[backOffset + i * 2] = new Vector2(0f, t);
            uvs[backOffset + i * 2 + 1] = new Vector2(0.5f, t);
        }

        // 应用数据到网格
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        // 赋值给组件
        GetComponent<MeshFilter>().mesh = mesh;
        if (leafMaterial != null)
        {
            GetComponent<MeshRenderer>().material = leafMaterial;
        }
    }
}
