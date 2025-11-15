using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using JetBrains.Annotations;


public class Node
{
    public Vector3 pos;
    public Vector3 rest_pos;
    public Vector3 lastPos;
    public float length;

    public Vector3 dir;
    public Vector3 local_rest_dir;
    public Vector3 wind;

    public Node[] children;
    public int children_num;
    public float resource;
    public float vitality;
    public bool is_leaf;
    public int buds_sum;
    public int internode_sum;
    public Node parent;
    public bool is_terminal;
    public int[] vertex_indices;
    public float radius;
    public int phyllotaxy;
    public Vector3 local_rotation_axis = new Vector3(1f,0f,0f);
    public float local_rotation_angle = 0;
    public float propagation_coefficient;
    public Vector3 angular_velocity = new Vector3(0f,0f,0f);
    public Quaternion worldRotation;
    public Quaternion inverseWorldRotation;

    public Vector3 propagation_force;
    public GameObject leaf;

    public Node()
    {
        is_leaf = true;
    }


    public static void preOrderTraverse(Node root, Action<Node> action)
    {
        if (root == null) return;

        Stack<Node> stack = new Stack<Node>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            Node current_node = stack.Pop(); 
            action(current_node);

            for(int i = 0; i< current_node.children_num; i++)
            {
                if (current_node.children[i] != null)
                {
                    stack.Push(current_node.children[i]);
                }
            }
        }

    }

    public static void postOrderTraverse(Node root, Action<Node> action)
    {
        if (root == null) return;

        Stack<(Node node, bool isProcessed)> stack = new Stack<(Node node, bool isProcessed)> ();
        stack.Push((root, false));

        while (stack.Count > 0)
        {
            var (current_node,isProcessed) = stack.Pop ();

            if (!isProcessed)
            {
                stack.Push((current_node,true));

                for (int i = 0; i < current_node.children_num; i++)
                {
                    if (current_node.children[i] != null)
                    {
                        stack.Push((current_node.children[i],false));
                    }
                }

            }
            else
            {
                action(current_node);
            }
        }
    }
}
