using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;


class Leaves
{
    private Transform parent_transform;
    private GameObject prefab;
    private float leaf_threshold = 0.5f;
    private float leaf_scale = 1f;
    public Leaves(GameObject leaf_prefab, Transform parent, float local_scale, float grow_threshold)
    {
        parent_transform = parent;
        leaf_scale = local_scale;
        prefab = leaf_prefab;
        leaf_threshold = grow_threshold;

}

    public void addLeaves(Node node)
    {
        if (prefab == null)
        {
            Debug.LogError("no leave prefab");
            return;
        }

        if ((node.is_leaf && (node.parent.parent != null) && (node.resource > leaf_threshold))
            || (node.is_terminal && (node.parent.parent != null)))
        {
            GameObject leaf = UnityEngine.Object.Instantiate(prefab, parent_transform);
            node.leaf = leaf;
            leaf.transform.position = parent_transform.TransformPoint(node.pos) + node.dir * 0.1f;

            leaf.transform.forward = node.dir + new Vector3(0f, -1f, 0f) * UnityEngine.Random.value*2;

            leaf.transform.localScale = Vector3.one * leaf_scale;
        }
    }
}
