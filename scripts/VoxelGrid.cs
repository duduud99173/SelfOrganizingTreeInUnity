using JetBrains.Annotations;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class VoxelGrid
{
    public Vector3Int size;
    public Vector3 range;
    public Vector3 voxelSize;

    public float[,,] values;

    public VoxelGrid(Vector2 radius_xz, float height_y, Vector3Int box_size, float init_value)
    {
        size = box_size;
        range = new Vector3(radius_xz.x, height_y, radius_xz.y);

        voxelSize.x = (2 * range.x) / size.x;
        voxelSize.z = (2 * range.z) / size.z;
        voxelSize.y = range.y / size.y;

        values = new float[size.x, size.y, size.z];

        reset(init_value);
    }

    public void  reset(float value)
    {
        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                for (int k = 0; k < size.z; k++)
                {
                    values[i, j, k] = value;
                }
            }
        }
    }

    public Vector3Int getIndex(Vector3 pos)
    {
        if (Mathf.Abs(pos.x) > range.x || pos.y < 0 || pos.y > range.y || Mathf.Abs(pos.z) > range.z)
        {
            Debug.Log("position out of voxel grid range!!!!");
        }

        float xOffset = Mathf.Abs(pos.x + range.x);
        int i = (int)(xOffset / voxelSize.x);

        int j = (int)(pos.y / voxelSize.y);

        float zOffset = Mathf.Abs(pos.z + range.z);
        int k = (int)(zOffset / voxelSize.z);

        i = Mathf.Clamp(i, 0, size.x - 1);
        j = Mathf.Clamp(j, 0, size.y - 1);
        k = Mathf.Clamp(k, 0, size.z - 1);

        return new Vector3Int(i,j,k);
    }

    public float getValue(Vector3 pos)
    {
        Vector3Int index = getIndex(pos);
        return values[index.x, index.y, index.z];
    }
}
