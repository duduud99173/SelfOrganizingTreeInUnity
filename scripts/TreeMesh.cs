using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TreeMesh
{
    private Node root;
    
    public Mesh mesh;

    public TreeMesh(Node root)
    {
        mesh = new Mesh();
        this.root = root;
    }


    public Mesh getMesh()
    {
        return mesh;
    }

    public void updateMesh( float base_radius, int subdivision, float width_update_exponential = 2.5f)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();
        
        void updateMeshPerNode(Node node)
        {
            if (node.is_leaf)
            {
                node.radius = base_radius;
                return;
            }

            if (node.children_num == 1)
            {
                Node child = node.children[0];
                node.radius = child.radius;

                int top_start_index = vertices.Count;
                int bottom_start_index = top_start_index + subdivision;
                vertices.AddRange(Utility.circle(child.pos - node.pos, subdivision, child.radius, child.pos));
                vertices.AddRange(Utility.circle(child.pos - node.pos, subdivision, node.radius, node.pos));

                for (int i = 0; i < subdivision; i++)
                {
                    int[] trangles = new int[6];
                    trangles[0] = top_start_index + i;
                    trangles[1] = top_start_index + (i + 1) % subdivision;
                    trangles[2] = bottom_start_index + i;
                    trangles[3] = bottom_start_index + i;
                    trangles[4] = top_start_index + (i + 1) % subdivision;
                    trangles[5] = bottom_start_index + (i + 1) % subdivision;
                    indices.AddRange(trangles);
                }

                return;
            }
            else
            {
                Node child = node.children[0];
                Node next_child = node.children[1];
                float upper_radius = child.radius;
                float lower_radius;

                int upper_start_index = vertices.Count;
                int lower_start_index;
                var upper_circle = Utility.circle(child.pos - next_child.pos, subdivision, upper_radius, child.pos);
                vertices.AddRange(upper_circle);

                for (int i = 0; i < node.children_num - 2; i++)
                {
                    child = node.children[i];
                    next_child = node.children[i + 1];

                    lower_radius = updateRadius(upper_radius, next_child.radius, width_update_exponential);
                    var lower_circle = Utility.circle(child.pos - next_child.pos, subdivision, lower_radius, next_child.pos);
                    lower_start_index = upper_start_index + subdivision;
                    vertices.AddRange(lower_circle);

                    for (int j = 0; j < subdivision; j++)
                    {
                        int[] trangles = new int[6];
                        trangles[0] = upper_start_index + j;
                        trangles[1] = upper_start_index + (j + 1) % subdivision;
                        trangles[2] = lower_start_index + j;
                        trangles[3] = lower_start_index + j;
                        trangles[4] = upper_start_index + (j + 1) % subdivision;
                        trangles[5] = lower_start_index + (j + 1) % subdivision;
                        indices.AddRange(trangles);
                    }

                    upper_start_index = lower_start_index;
                    upper_radius = lower_radius;
                }
                Node last_child = node.children[node.children_num - 1];
                lower_radius = updateRadius(upper_radius, last_child.radius, width_update_exponential);
                vertices.AddRange(Utility.circle(node.dir, subdivision, lower_radius, node.pos));
                lower_start_index = upper_start_index + subdivision;

                node.radius = lower_radius;

                for (int j = 0; j < subdivision; j++)
                {
                    int[] trangles = new int[6];
                    trangles[0] = upper_start_index + j;
                    trangles[1] = upper_start_index + (j + 1) % subdivision;
                    trangles[2] = lower_start_index + j;
                    trangles[3] = lower_start_index + j;
                    trangles[4] = upper_start_index + (j + 1) % subdivision;
                    trangles[5] = lower_start_index + (j + 1) % subdivision;
                    indices.AddRange(trangles);
                }

            }
        }

        Action<Node> action = updateMeshPerNode;



        Node.postOrderTraverse(root, action);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();

    }

    private float updateRadius(float r1, float r2, float width_update_exponential)
    {
        return Mathf.Pow((Mathf.Pow(r1, width_update_exponential) + Mathf.Pow(r2, width_update_exponential)), 1f/width_update_exponential);
    }
}
