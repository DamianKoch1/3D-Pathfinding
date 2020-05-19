using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MarchingTables;

public static class MarchingCubes
{
    public static Mesh March(Grid grid, float isoLevel)
    {
        var mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        var pos = -grid.size / 2;
        for (int x = 0; x < grid.maxX - 1; x++)
        {
            for (int y = 0; y < grid.maxY - 1; y++)
            {
                for (int z = 0; z < grid.maxZ - 1; z++)
                {
                    int cellType = GetCellType(grid, x, y, z, isoLevel);
                    for (int i = 0; Triangulation[cellType, i] != -1; i += 3)
                    {
                        var edge0 = Triangulation[cellType, i];
                        var edge1 = Triangulation[cellType, i + 1];
                        var edge2 = Triangulation[cellType, i + 2];

                        Vector3 a = Interp(edge0, GetEdgeCornerAValue(grid, x, y, z, edge0), GetEdgeCornerBValue(grid, x, y, z, edge0), isoLevel);
                        Vector3 b = Interp(edge1, GetEdgeCornerAValue(grid, x, y, z, edge1), GetEdgeCornerBValue(grid, x, y, z, edge1), isoLevel);
                        Vector3 c = Interp(edge2, GetEdgeCornerAValue(grid, x, y, z, edge2), GetEdgeCornerBValue(grid, x, y, z, edge2), isoLevel);

                        a = Vector3.Scale(a, grid.step) + pos;
                        b = Vector3.Scale(b, grid.step) + pos;
                        c = Vector3.Scale(c, grid.step) + pos;

                        tris.Add(verts.Count);
                        tris.Add(verts.Count + 1);
                        tris.Add(verts.Count + 2);

                        verts.Add(a);
                        verts.Add(b);
                        verts.Add(c);
                    }

                    pos.z += grid.step.z;
                }
                pos.z = -grid.size.z / 2;
                pos.y += grid.step.y;
            }
            pos.y = -grid.size.y / 2;
            pos.x += grid.step.x;
        }

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        return mesh;
    }

    public static float GetEdgeCornerAValue(Grid grid, int x, int y, int z, int edgeIndex)
    {
        x += CubeCorners[EdgeCornerA[edgeIndex]].x;
        if (x >= grid.maxX) return 0;
        y += CubeCorners[EdgeCornerA[edgeIndex]].y;
        if (y >= grid.maxY) return 0;
        z += CubeCorners[EdgeCornerA[edgeIndex]].z;
        if (z >= grid.maxZ) return 0;
        return grid.nodes[x, y, z].isoValue;
    }

    public static float GetEdgeCornerBValue(Grid grid, int x, int y, int z, int edgeIndex)
    {
        x += CubeCorners[EdgeCornerB[edgeIndex]].x;
        if (x >= grid.maxX) return 0;
        y += CubeCorners[EdgeCornerB[edgeIndex]].y;
        if (y >= grid.maxY) return 0;
        z += CubeCorners[EdgeCornerB[edgeIndex]].z;
        if (z >= grid.maxZ) return 0;
        return grid.nodes[x, y, z].isoValue;
    }

    public static Vector3 Interp(int cubeEdge, float a, float b, float isoLevel)
    {
        var p0 = CubeCorners[EdgeCornerA[cubeEdge]];
        if (a == b) return p0; 
        var p1 = CubeCorners[EdgeCornerB[cubeEdge]];
        float t = Mathf.Min(Mathf.Max(0, (isoLevel - a) / (b - a)), 1);
        return p0 + t * (Vector3)(p1 - p0);
    }

    public static Vector3Int[] CubeCorners = new Vector3Int[8]
    {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 0, 1),
        new Vector3Int(0, 0, 1),
                  
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(1, 1, 1),
        new Vector3Int(0, 1, 1)
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

    //    c7--------c6
    //   /|         /|
    //  / |        / |
    // c4---e4---c5  |
    // |  |       |  |
    // e8 c3------|-c2
    // | /        | /
    // |/         |/
    // c0---e0---c1

    public static int GetCellType(Grid grid, int x, int y, int z, float isoLevel = 0.5f)
    {
        //0 - 255
        int cellType = 0;
        if (grid.nodes[x, y, z].isoValue < isoLevel)
        {
            cellType |= 1;
        }
        if (grid.nodes[x + 1, y, z].isoValue < isoLevel)
        {
            cellType |= 2;
        }
        if (grid.nodes[x + 1, y, z + 1].isoValue < isoLevel)
        {
            cellType |= 4;
        }
        if (grid.nodes[x, y, z + 1].isoValue < isoLevel)
        {
            cellType |= 8;
        }
        if (grid.nodes[x, y + 1, z].isoValue < isoLevel)
        {
            cellType |= 16;
        }
        if (grid.nodes[x + 1, y + 1, z].isoValue < isoLevel)
        {
            cellType |= 32;
        }
        if (grid.nodes[x + 1, y + 1, z + 1].isoValue < isoLevel)
        {
            cellType |= 64;
        }
        if (grid.nodes[x, y + 1, z + 1].isoValue < isoLevel)
        {
            cellType |= 128;
        }
        return cellType;
    }
}

