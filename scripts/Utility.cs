using UnityEngine;

public class Utility
{
    static public Vector3[] circle(Vector3 normal, int sub_division, float radius, Vector3 pos)
    {
        Vector3[] vertices = new Vector3[sub_division];

        float theta = 360f / sub_division;
        float angle = 0f;

        for (int i = 0; i < sub_division; i++)
        {
            Quaternion rotate = Quaternion.AngleAxis(angle, new Vector3(0f, 1f, 0f));

            angle -= theta;


            vertices[i] = rotate * (new Vector3(radius, 0f, 0f));
        }

        Quaternion rotation = Quaternion.FromToRotation(new Vector3(0f,1f,0f), normal);

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertexRelativeToCenter = vertices[i];

            Vector3 rotatedVertex = rotation * vertexRelativeToCenter;

            vertices[i] = rotatedVertex + pos;
        }

        return vertices;
    }
}
