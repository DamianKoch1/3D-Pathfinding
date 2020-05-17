using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MarchingTables;

public static class MarchingCubes
{
    public static Mesh March(bool[,,] voxels, Vector3 gridSize, Vector3 gridStep)
    {
        var maxX = voxels.GetUpperBound(0);
        var maxY = voxels.GetUpperBound(1);
        var maxZ = voxels.GetUpperBound(2);
        var mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        var pos = -gridSize / 2;
        for (int x = 0; x < maxX; x++)
        {
            for (int y = 0; y < maxY; y++)
            {
                for (int z = 0; z < maxZ; z++)
                {
                    int cellType = GetCellType(voxels, x, y, z);
                    for (int i = 0; Triangulation[cellType, i] != -1; i += 3)
                    {
                        Vector3 a = CubeEdges[Triangulation[cellType, i]];
                        Vector3 b = CubeEdges[Triangulation[cellType, i+1]];
                        Vector3 c = CubeEdges[Triangulation[cellType, i+2]];

                        a = Vector3.Scale(a, gridStep) + pos;
                        b = Vector3.Scale(b, gridStep) + pos;
                        c = Vector3.Scale(c, gridStep) + pos;

                        tris.Add(verts.Count);
                        tris.Add(verts.Count + 1);
                        tris.Add(verts.Count + 2);

                        verts.Add(a);
                        verts.Add(b);
                        verts.Add(c);


                    }

                    pos.z += gridStep.z;
                }
                pos.z = -gridSize.z / 2;
                pos.y += gridStep.y;
            }
            pos.y = -gridSize.y / 2;
            pos.x += gridStep.x;
        }

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        return mesh;
    }

    public static Vector3[] CubeCorners = new Vector3[8]
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(1, 0, 1),
        new Vector3(0, 0, 1),

        new Vector3(0, 1, 0),
        new Vector3(1, 1, 0),
        new Vector3(1, 1, 1),
        new Vector3(0, 1, 1)
    };


    public static Vector3[] CubeEdges = new Vector3[12]
    {
        new Vector3(0.5f, 0, 0),
        new Vector3(1,    0, 0.5f),
        new Vector3(0.5f, 0, 1),
        new Vector3(0,    0, 0.5f),

        new Vector3(0.5f, 1, 0),
        new Vector3(1,    1, 0.5f),
        new Vector3(0.5f, 1, 1),
        new Vector3(0,    1, 0.5f),

        new Vector3(0, 0.5f, 0),
        new Vector3(1, 0.5f, 0),
        new Vector3(1, 0.5f, 1),
        new Vector3(0, 0.5f, 1)
    };

    //    7----------6
    //   /|         /|
    //  / |        / |
    // 4----------5  |
    // |  |       |  |
    // |  3-------|--2
    // | /        | /
    // |/         |/
    // 0----0-----1

    public static int GetCellType(bool[,,] voxels, int x, int y, int z)
    {
        //0 - 255
        int cellType = 0;
        if (!voxels[x, y, z])
        {
            cellType |= 1;
        }
        if (!voxels[x + 1, y, z])
        {
            cellType |= 2;
        }
        if (!voxels[x + 1, y, z + 1])
        {
            cellType |= 4;
        }
        if (!voxels[x, y, z + 1])
        {
            cellType |= 8;
        }
        if (!voxels[x, y + 1, z])
        {
            cellType |= 16;
        }
        if (!voxels[x + 1, y + 1, z])
        {
            cellType |= 32;
        }
        if (!voxels[x + 1, y + 1, z + 1])
        {
            cellType |= 64;
        }
        if (!voxels[x, y + 1, z + 1])
        {
            cellType |= 128;
        }
        return cellType;
    }

    public static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
    {

    }
}

public struct Triangle
{

}
