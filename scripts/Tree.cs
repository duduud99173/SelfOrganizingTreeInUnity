using JetBrains.Annotations;
using System;
using System.ComponentModel.Design.Serialization;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class Tree : MonoBehaviour
{
    public int max_grow_step;
    private int iter_depth = 0;

    [Header("网格参数")]
    public int sub_division;
    public float base_radius;
    public float width_update_exponential;

    [Header("叶子设置")]
    public GameObject leaf_prefab;
    public float leaf_scale;
    public float leaf_threshold;

    [Header("算法选择")]
    public EnvironmentAlgorithm Algorithm;
    public BudFateModel BudFateModel;

    [Header("生长参数")]
    public bool terminal_control;
    public int terminal_control_stop_step;
    public bool use_phyllotaxy;
    public float env_optimal_grow_dir_weight_Xi;

    public float max_branch_angle;
    public float internode_length;
    public float shedding_threshold;
    public Vector3 tropism_dir;
     // 增加间距区分层级
    [Header(" 向性随时间变化(smoothstep)")] // 二级标题（通过空格缩进模拟层级）
    public float tropism_start_weight;
    public float tropism_end_weight;
    public int tropism_time_start_change;
    public int tropism_time_end_change;



    [Header("Voxel grid参数")]
    public Vector3Int VoxelGridSize;
    public Vector2 VoxelGridRangeHalfXZ;
    public float VoxelGridHeightY;

    [Header("Shadow Propagation 参数")]
    public float full_exposure_constant;
    public float shadow_exponential_base;
    public int shadow_pyramid_height;
    public float shadow_scaler;

    [Header("BorchertHonda Model 参数")]
    public float main_axis_resource_bias_lambda;

    [Header("Priority Model 参数")]
    public float priority_weights_min;
    public float priority_weights_max;
    public float priority_weights_Kappa;
    public float priority_resource_vitality_conversion_rate;

    private SelfOrgTree self_organizing_tree;
    private TreeMesh tree_mesh;
    private Leaves leaves;

    private DistanceLevel cur_lod_level;

    public class DistanceLevel
    {
        public float maxDistance; // 该档位的最大距离
        public int edgeCount;     // 对应边数
    }

    public DistanceLevel[] levels = new DistanceLevel[]
    {
    new DistanceLevel { maxDistance = 5, edgeCount = 16 },
    new DistanceLevel { maxDistance = 15, edgeCount = 12 },
    new DistanceLevel { maxDistance = 30, edgeCount = 8 },
    new DistanceLevel { maxDistance = 50, edgeCount = 6 },
    new DistanceLevel { maxDistance = float.MaxValue, edgeCount = 5 }
    };



    public Node getRoot()
    {
        return self_organizing_tree.getRoot();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        TreeInitInfo tree_init_info = new TreeInitInfo();
        tree_init_info.environment_algorithm = Algorithm;
        tree_init_info.bud_fate_model = BudFateModel;

        tree_init_info.VoxelGridSize = VoxelGridSize;
        tree_init_info.VoxelGridRangeHalfXZ = VoxelGridRangeHalfXZ;
        tree_init_info.VoxelGridHeightY = VoxelGridHeightY;

        tree_init_info.terminal_control = terminal_control;
        tree_init_info.tropism_dir = tropism_dir.normalized;

        tree_init_info.env_optimal_grow_dir_weight_Xi = env_optimal_grow_dir_weight_Xi;
        tree_init_info.tropism_grow_dir_weight_eta = tropism_start_weight;
        tree_init_info.max_branch_angle = max_branch_angle;
        tree_init_info.internode_length = internode_length;
        tree_init_info.shedding_threshold = shedding_threshold;

        tree_init_info.full_exposure_constant = full_exposure_constant;
        tree_init_info.shadow_exponential_base = shadow_exponential_base;
        tree_init_info.shadow_pyramid_height = shadow_pyramid_height;
        tree_init_info.shadow_scaler = shadow_scaler;

        tree_init_info.main_axis_resource_bias_lambda = main_axis_resource_bias_lambda;

        tree_init_info.priority_weights_min = priority_weights_min;
        tree_init_info.priority_weights_max = priority_weights_max;
        tree_init_info.priority_weights_Kappa = priority_weights_Kappa;
        tree_init_info.priority_resource_vitality_conversion_rate = priority_resource_vitality_conversion_rate;
        tree_init_info.use_phyllotaxy = use_phyllotaxy;


        self_organizing_tree = new SelfOrgTree(tree_init_info);

        tree_mesh = new TreeMesh(self_organizing_tree.getRoot());

        leaves = new Leaves(leaf_prefab,transform, leaf_scale, leaf_threshold);

        for(int i = 0; i < max_grow_step; i++)
        {
            growOneYear();
        }

        addLeaves();
    }

    void lodUpdate()
    {
        if (getRoot() == null) return;

        Camera cam =  Camera.main;
        if (cam == null) return;


        Vector3 pointPosition = getRoot().pos;
        Vector3 cameraPosition = cam.transform.position;

        float distance = Vector3.Distance(pointPosition, cameraPosition);

        int edge_count = 5;

        foreach (var level in levels)
        {
            if (distance <= level.maxDistance)
                edge_count = level.edgeCount;
        }

        if (edge_count != sub_division)
        {
            sub_division = edge_count;
            //updateMesh();
        }
    }

    // Update is called once per frame
    void Update()
    {
        lodUpdate();        
    }

    float tropismAtStep(int step)
    {
        float x = (float)step;
        float t = Math.Clamp((x - tropism_time_start_change) / (tropism_time_end_change - tropism_time_start_change), 0f, 1f);
        float v = t * t * (3f - 2f * t);
        return tropism_start_weight + v * (tropism_end_weight - tropism_start_weight);

    }

    void growOneYear()
    {
        Debug.Log("Year" + iter_depth);
        if ( iter_depth == terminal_control_stop_step)
        {
            self_organizing_tree.stopTerminalControl();
        }

        self_organizing_tree.growOneYear();
        iter_depth++;

        self_organizing_tree.setTropismWeight(tropismAtStep(iter_depth));
        updateMesh();
    }

    public void updateMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        tree_mesh.updateMesh(base_radius, sub_division, width_update_exponential);
        Mesh mesh = tree_mesh.getMesh();
        mesh.RecalculateNormals();
        mesh.Optimize();
        meshFilter.sharedMesh =  mesh;
        
    }

    void addLeaves()
    {
        Action<Node> action = leaves.addLeaves;

        Node.postOrderTraverse(self_organizing_tree.getRoot(), action);
    }


    static void drawDebugLinePerNode(Node node)
    {
        if (node.children == null) return;

        Vector3 begin = node.pos;
        Vector3 end = node.children[node.children_num - 1].pos;
        Debug.DrawLine(begin, end);

        for (int i = node.children_num - 1; i > 0; i--)
        {
            begin = node.children[i].pos;
            end = node.children[i - 1].pos;
            Debug.DrawLine(begin, end);
        }
        


    }

    void drawDebugTree()
    {
        Node root = self_organizing_tree.getRoot();

        Action<Node> action = drawDebugLinePerNode;
        Node.preOrderTraverse(root, drawDebugLinePerNode);
    }
        
}
