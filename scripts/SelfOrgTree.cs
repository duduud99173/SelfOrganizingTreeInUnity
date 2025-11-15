
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Profiling.Memory.Experimental;
using UnityEngine;


public enum EnvironmentAlgorithm
{
    SPACE_COLONIZATION,
    SHADOW_PROPAGATION
};

public enum BudFateModel
{
    BorchertHonda,
    Priority
};

public struct TreeInitInfo
{
    public EnvironmentAlgorithm environment_algorithm;
    public BudFateModel bud_fate_model;
    public Vector3Int VoxelGridSize;
    public Vector2 VoxelGridRangeHalfXZ;
    public float VoxelGridHeightY;
    public int shadow_pyramid_height;
    public float full_exposure_constant;
    public float shadow_scaler;
    public float shadow_exponential_base;
    public bool terminal_control;
    public float priority_weights_min;
    public float priority_weights_max;
    public float priority_weights_Kappa;
    public float priority_resource_vitality_conversion_rate;
    public Vector3 tropism_dir;
    public float env_optimal_grow_dir_weight_Xi;
    public float tropism_grow_dir_weight_eta;
    public float max_branch_angle;
    public float internode_length;
    public float shedding_threshold;
    public float main_axis_resource_bias_lambda;
    public bool use_phyllotaxy;
};



public class SelfOrgTree
{
    static private EnvironmentAlgorithm environment_algorithm;
    static private VoxelGrid environment_qualities;

    private BudFateModel bud_fate_model;

    private Node root;

    private int shadow_pyramid_height;
    private float full_exposure_constant;
    private float shadow_scaler;
    private float shadow_exponential_base;

    private bool terminal_control;
    private bool use_phyllotaxy;

    private float main_axis_resource_bias_lambda;

    private float priority_weights_max;
    private float priority_weights_min;
    private float priority_weights_Kappa;
    private float priority_resource_vitality_conversion_rate;

    private Vector3 tropism_dir = new Vector3(0.0f, -1.0f, 0.0f);
    private float env_optimal_grow_dir_weight_Xi;
    private float tropism_grow_dir_weight_eta;
    private float max_branch_angle;
    private float internode_length;
    private float shedding_threshold;


    private Mesh mesh = new Mesh();
    public SelfOrgTree(TreeInitInfo init_info)
    {
        environment_algorithm = init_info.environment_algorithm;
        bud_fate_model = init_info.bud_fate_model;
        shadow_pyramid_height = init_info.shadow_pyramid_height;
        full_exposure_constant = init_info.full_exposure_constant;
        shadow_scaler = init_info.shadow_scaler;
        shadow_exponential_base = init_info.shadow_exponential_base;
        terminal_control = init_info.terminal_control;
        priority_weights_max = init_info.priority_weights_max;
        priority_weights_min = init_info.priority_weights_min;
        priority_weights_Kappa = init_info.priority_weights_Kappa;
        priority_resource_vitality_conversion_rate = init_info.priority_resource_vitality_conversion_rate;
        tropism_dir = init_info.tropism_dir;
        env_optimal_grow_dir_weight_Xi = init_info.env_optimal_grow_dir_weight_Xi;
        tropism_grow_dir_weight_eta = init_info.tropism_grow_dir_weight_eta;
        max_branch_angle = init_info.max_branch_angle;
        internode_length = init_info.internode_length;
        shedding_threshold = init_info.shedding_threshold;
        main_axis_resource_bias_lambda = init_info.main_axis_resource_bias_lambda;
        use_phyllotaxy = init_info.use_phyllotaxy;
        //internode_length = Mathf.Max(init_info.VoxelGridHeightY / init_info.VoxelGridSize.y,
        //                         2f * init_info.VoxelGridRangeHalfXZ.x / init_info.VoxelGridSize.x,
        //                         2f * init_info.VoxelGridRangeHalfXZ.y / init_info.VoxelGridSize.z
        //    );


        if (environment_algorithm == EnvironmentAlgorithm.SPACE_COLONIZATION)
        {
            //spaceColonization();
        }
        else if (environment_algorithm == EnvironmentAlgorithm.SHADOW_PROPAGATION)
        {
            environment_qualities = new VoxelGrid(init_info.VoxelGridRangeHalfXZ, init_info.VoxelGridHeightY,
                init_info.VoxelGridSize,
                full_exposure_constant + shadow_scaler
                );
        }

        root = new Node();
        root.pos = new Vector3(0.0f, 0.0f, 0.0f);
        root.is_leaf = true;
        root.dir = new Vector3(0.0f, 1.0f, 0.0f);
        root.parent = null;
        root.is_terminal = false;

    }


    public void setTropismWeight(float weight)
    {
        tropism_grow_dir_weight_eta = weight;
    }

    public void stopTerminalControl()
    {
        terminal_control = false;
    }
    public Node getRoot()
    {
        return root;
    }

    public void growOneYear()
    {
        calculateLocalEnvironmentOfBuds();
        determineBudsFate();
        appendNewShoots();
        shedBranches();
    }

    void castShadow(Node node)
    {
        if (!node.is_leaf) return;

        if (environment_algorithm != EnvironmentAlgorithm.SHADOW_PROPAGATION) return;

        Vector3Int index = environment_qualities.getIndex(node.pos);

        for (int q = 0; q <= shadow_pyramid_height; q++)
        {
            for (int i = -q; i <= q; i++)
            {
                for (int j = -q; j <= q; j++)
                {
                    int x = index.x + i;
                    int y = index.y - q;
                    int z = index.z + j;

                    if (x >= 0 && x < environment_qualities.values.GetLength(0) &&
                        y >= 0 && y < environment_qualities.values.GetLength(1) &&
                        z >= 0 && z < environment_qualities.values.GetLength(2))
                    {
                        environment_qualities.values[x, y, z] -= shadow_scaler * MathF.Pow(shadow_exponential_base, -q);
                        environment_qualities.values[x, y, z] = MathF.Max(environment_qualities.values[x, y, z], 0f);
                    }
                }
            }
        }
    }

    void calculateLocalEnvironmentOfBuds()
    {
        environment_qualities.reset(full_exposure_constant + shadow_scaler);
        Action<Node> cast = castShadow;

        Node.preOrderTraverse(root, cast);

        void priorityGetEnvironmentQualityPerNode(Node node)
        {
            if (node.is_leaf)
            {
                Vector3Int index = environment_qualities.getIndex(node.pos);
                node.resource = environment_qualities.values[index.x, index.y, index.z];
            }
        }

        Action<Node> action;

        if (environment_algorithm == EnvironmentAlgorithm.SHADOW_PROPAGATION)
        {
            action = priorityGetEnvironmentQualityPerNode;
        }
        else if (environment_algorithm != EnvironmentAlgorithm.SPACE_COLONIZATION)
        {
            action = priorityGetEnvironmentQualityPerNode;//todo space colonization
        }
        else
        {
            Debug.Log("unknow enmvironment algorithm");
            return;
        }

        Debug.Log("start node get light");
        Node.preOrderTraverse(root, action);
    }


    void determineBudsFate()
    {
        gatherInfomation();
        if (bud_fate_model == BudFateModel.Priority)
        {
            priorityDistributeVitality();
        }
        else if (bud_fate_model == BudFateModel.BorchertHonda)
        {
            BHDistributeVitality();
        }

    }

    

    void gatherInfomation()
    {
        void gatherInfomationPerNode(Node node)
        {
            if (node.is_leaf)
            {
                node.buds_sum = 1;
                node.internode_sum = 0;
                return;
            }

            node.resource = 0.0f;
            node.buds_sum = 0;
            node.internode_sum = node.children_num;

            for (int i = 0; i < node.children_num; i++)
            {
                node.internode_sum += node.children[i].internode_sum;
                node.buds_sum += node.children[i].buds_sum;
                node.resource += node.children[i].resource;
            }
        }

        Action<Node> action = gatherInfomationPerNode;
        Node.postOrderTraverse(root, action);
        Debug.Log("all resource" + root.resource);
    }



    void priorityDistributeVitality()
    {
        root.vitality = root.resource * priority_resource_vitality_conversion_rate;

        Debug.Log("all vitality" + root.vitality);


        void priorityDistributeVitalityPerNode(Node node)
        {
            if (node.is_leaf)
            {
                return;
            }

            List<Node> priority_list = new List<Node>();

            if (terminal_control && node.parent == null)
            {
                priority_list.AddRange(node.children);

                if (priority_list.Count > 1)
                {
                    //假设第一个子节点为顶芽
                    Node firstNode = priority_list[0];

                    var remaining = priority_list.Skip(1).OrderByDescending(n => (n.resource / n.buds_sum)).ToList();

                    priority_list.Clear();
                    priority_list.Add(firstNode);
                    priority_list.AddRange(remaining);
                }
            }
            else
            {
                priority_list.AddRange(node.children);

                priority_list.Sort((n1, n2) => (n2.resource / n2.buds_sum).CompareTo(n1.resource / n1.buds_sum));
            }

            float[] weighted_resource = new float[node.children_num];

            float weight = priority_weights_max;

            float threshold = priority_weights_Kappa * node.children_num;

            float k = (priority_weights_max - priority_weights_min) / threshold;

            float weighted_sum_of_resource = 0.0f;

            for (int i = 0; i < node.children_num; i++)
            {

                weight = (i < threshold) ? (priority_weights_max - k*i) : priority_weights_min;

                weighted_resource[i] = weight * (priority_list[i].resource);

                weighted_sum_of_resource += weighted_resource[i];

            }

            for (int i = 0; i < node.children_num; i++)
            {
                priority_list[i].vitality = node.vitality * weighted_resource[i] / weighted_sum_of_resource;

            }
        }

        Action<Node> action = priorityDistributeVitalityPerNode;

        Node.preOrderTraverse(root, action);
    }



    void BHDistributeVitality()
    {
        root.vitality = root.resource * priority_resource_vitality_conversion_rate;

        Debug.Log("all vitality" + root.vitality);

        void BHDistributeVitalityPerNode(Node node)
        {
            if (node.is_leaf) return;

            float vM = node.vitality;
            float vL;
            float lambda = main_axis_resource_bias_lambda;

            float qM = node.resource;
            float qL;

            for (int i = node.children_num - 1;i > 0;i--)
            {
                qL = node.children[i].resource;
                qM -= qL;

                Node child = node.children[i];
                float denominator = lambda * qM + (1 - lambda) * qL;

                if (qM + qL <= 0) return;
                if (denominator <= 0) return; 

                vL = (vM * (1 - lambda) * qL) / denominator;
                vM = (vM * lambda * qM )/ denominator;

                child.vitality = vL;
            }

            node.children[0].vitality = vM;
        }

        Action<Node> action = BHDistributeVitalityPerNode;
        Node.preOrderTraverse(root, action);
    }

    Vector3 getEnvironmentGrowTendency(Vector3 pos)
    {
        Vector3Int index = environment_qualities.getIndex(pos);
        int x = index.x;
        int y = index.y;
        int z = index.z;

        int minX = 0;
        int maxX = environment_qualities.values.GetLength(0) - 1;
        int minY = 0;
        int maxY = environment_qualities.values.GetLength(1) - 1;
        int minZ = 0;
        int maxZ = environment_qualities.values.GetLength(2) - 1;

        float leftX = (x - 1 < minX) ? environment_qualities.values[minX, y, z] : environment_qualities.values[x - 1, y, z];
        float rightX = (x + 1 > maxX) ? environment_qualities.values[maxX, y, z] : environment_qualities.values[x + 1, y, z];
        float dx = rightX - leftX;

        float downY = (y - 1 < minY) ? 0 : environment_qualities.values[x, y - 1, z];
        float upY = (y + 1 > maxY) ? environment_qualities.values[x, maxY, z] : environment_qualities.values[x, y + 1, z];
        float dy = upY - downY;

        float backZ = (z - 1 < minZ) ? environment_qualities.values[x, y, minZ] : environment_qualities.values[x, y, z - 1];
        float frontZ = (z + 1 > maxZ) ? environment_qualities.values[x, y, maxZ] : environment_qualities.values[x, y, z + 1];
        float dz = frontZ - backZ;

        Vector3 gradient = new Vector3(dx, dy, dz);
        return gradient.sqrMagnitude > 0.0001f ? gradient.normalized : Vector3.zero;
    }

    Vector3 getEnvironmentGrowTendency26(Vector3 pos)
    {
        Vector3Int index = environment_qualities.getIndex(pos);
        int x = index.x;
        int y = index.y;
        int z = index.z;

        // 获取3D数组的边界
        int minX = 0;
        int maxX = environment_qualities.values.GetLength(0) - 1;
        int minY = 0;
        int maxY = environment_qualities.values.GetLength(1) - 1;
        int minZ = 0;
        int maxZ = environment_qualities.values.GetLength(2) - 1;

        // 初始化梯度分量
        float totalDx = 0;
        float totalDy = 0;
        float totalDz = 0;
        int sampleCount = 0;

        // 遍历所有26个相邻点（x、y、z三个方向各±1，排除自身）
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    // 跳过当前点自身
                    if (dx == 0 && dy == 0 && dz == 0)
                        continue;

                    // 计算相邻点坐标，确保在边界内
                    int neighborX = Mathf.Clamp(x + dx, minX, maxX);
                    int neighborY = Mathf.Clamp(y + dy, minY, maxY);
                    int neighborZ = Mathf.Clamp(z + dz, minZ, maxZ);

                    // 获取相邻点的环境质量值
                    float neighborValue = environment_qualities.values[neighborX, neighborY, neighborZ];
                    // 获取当前点的环境质量值
                    float currentValue = environment_qualities.values[x, y, z];

                    // 累加梯度贡献（方向 * 差值）
                    totalDx += dx * (neighborValue - currentValue);
                    totalDy += dy * (neighborValue - currentValue);
                    totalDz += dz * (neighborValue - currentValue);

                    sampleCount++;
                }
            }
        }

        // 计算平均梯度
        Vector3 gradient = new Vector3(
            totalDx / sampleCount,
            totalDy / sampleCount,
            totalDz / sampleCount
        );

        // 归一化或返回零向量
        return gradient.sqrMagnitude > 0.0001f ? gradient.normalized : Vector3.zero;
    }

    Vector3 calActualGrowDir(Vector3 default_dir, Vector3 pos)
    {
        //一个bud在将要生长时，原来的固有生长方向由其真实生长方向替代
        return (default_dir + env_optimal_grow_dir_weight_Xi * getEnvironmentGrowTendency26(pos) + tropism_grow_dir_weight_eta * tropism_dir).normalized;
    }

    Vector3 getAxiliaryDir(Vector3 dir, Node node)
    {


        Vector3 vx = new Vector3(1.0f, 0.0f, 0.0f);
        Vector3 vy = new Vector3(0.0f, 1.0f, 0.0f);
        Vector3 local_vx = vx;
        Vector3 local_vy;

        if (MathF.Abs(Vector3.Dot(dir, vx)) > (1.0f - 0.0001f))
        {
            local_vx = vy;
        }

        local_vy = Vector3.Cross(dir, local_vx);
        local_vx = Vector3.Cross(dir, local_vy);

        float r = UnityEngine.Random.value;
        float s;
        if (use_phyllotaxy)
        {
            s = 2.3998f * node.phyllotaxy;
        }
        else
        {
            s = UnityEngine.Random.value;
        }


        float h = MathF.Cos(max_branch_angle);
        float phi = 2 * Mathf.PI * s;
        float z = h + (1.0f - h) * r;
        float branch_angle_sin = MathF.Sqrt(1.0f - z * z);
        float x = branch_angle_sin * MathF.Cos(phi);
        float y = branch_angle_sin * MathF.Sin(phi);

        return dir * z + local_vx * x + local_vy * y;

    }

    void appendNewShoots()
    {
        void appendNewShootsPerNode(Node node)
        {
            if (!node.is_leaf) return;

            int num_metamers = (int)MathF.Floor(node.vitality);

            if (num_metamers <= 0) return;


            Vector3 default_dir = node.dir;
            node.dir = calActualGrowDir(node.dir, node.pos);
            node.is_leaf = false;

            float length = internode_length * node.vitality / num_metamers;


            Node terminal_bud = new Node();
            terminal_bud.is_leaf = true;
            terminal_bud.is_terminal = true;
            terminal_bud.parent = node;

            node.children_num = num_metamers;
            node.children = new Node[num_metamers];
            node.children[0] = terminal_bud;


            Vector3 grow_dir = node.dir;
            Vector3 pos_grow_from = node.pos;

            if (num_metamers > 1)
            {
                for (int i = num_metamers - 1; i > 0; i--)
                {
                    node.children[i] = new Node();
                    node.children[i].pos = pos_grow_from + length * grow_dir;
                    node.children[i].phyllotaxy = num_metamers - i;
                    node.children[i].dir = getAxiliaryDir(grow_dir, node.children[i]);
                    node.children[i].is_leaf = true;
                    node.children[i].parent = node;
                    node.children[i].is_terminal = false;
                    pos_grow_from = node.children[i].pos;
                    grow_dir = grow_dir + tropism_dir * tropism_grow_dir_weight_eta;
                }
            }
            node.children[0].pos = pos_grow_from + length * grow_dir;
            node.children[0].dir = grow_dir;

            //以下处理顶芽的方式默认了顶芽node是parent最后遍历到的node
            if (node.is_terminal)
            {
                Node parent = node.parent;

                Node[] new_children = new Node[parent.children_num + num_metamers];

                for (int i = 0; i < num_metamers; i++)
                {
                    new_children[i] = node.children[i];
                    new_children[i].parent = parent;

                }

                for (int i = 0; i < parent.children_num; i++)
                {
                    new_children[i + num_metamers] = parent.children[i];
                    new_children[i + num_metamers].parent = parent;

                }

                node.dir = getAxiliaryDir(default_dir, node);
                node.is_terminal = false;
                parent.children = new_children;
                parent.children_num = num_metamers + parent.children_num;
                node.children = null;
                node.is_leaf = true;
                node.children_num = 0;

                for(int i = node.children_num - 1; i > 0; i--)
                {
                    node.children[i].phyllotaxy = node.children_num - i;
                }

            }
        }

        Action<Node> action = appendNewShootsPerNode;

        Node.preOrderTraverse(root, action);

    }



    //void removeShadow(Node node)
    //{
    //    if (environment_algorithm != EnvironmentAlgorithm.SHADOW_PROPAGATION) return;

    //    Vector3Int index = environment_qualities.getIndex(node.pos);

    //    for (int q = 0; q <= shadow_pyramid_height; q++)
    //    {
    //        for (int i = -q; i <= q; i++)
    //        {
    //            for (int j = -q; j <= q; j++)
    //            {
    //                int x = index.x + i;
    //                int y = index.y - q;
    //                int z = index.z + j;

    //                if (x >= 0 && x < environment_qualities.values.GetLength(0) &&
    //                    y >= 0 && y < environment_qualities.values.GetLength(1) &&
    //                    z >= 0 && z < environment_qualities.values.GetLength(2))
    //                {
    //                    environment_qualities.values[x, y, z] += shadow_scaler * MathF.Pow(shadow_exponential_base, -q);
    //                }
    //            }
    //        }
    //    }
    //}

    void shedBranches()
    {
        void shedBranchesPerNode(Node node)
        {
            if (node.parent == null) return;

            float ratio = node.resource / node.internode_sum;

            if (ratio < shedding_threshold)
            {
                node.children = null;
                node.is_terminal = false;
                node.is_leaf = true;
                node.children_num = 0;
                node.buds_sum = 1;
                node.internode_sum = 0;

            }
        }

        Action<Node> action = shedBranchesPerNode;

        Node.preOrderTraverse(root, action);
    }
}