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
        var pos = -grid.extents / 2;
        for (int x = 0; x < grid.xSize; x++)
        {
            for (int y = 0; y < grid.ySize; y++)
            {
                for (int z = 0; z < grid.zSize; z++)
                {
                    int cellType = GetCellType(grid, x, y, z, isoLevel);
                    for (int i = 0; Triangulation[cellType, i] != -1; i += 3)
                    {
                        var edge0 = Triangulation[cellType, i];
                        var edge1 = Triangulation[cellType, i + 1];
                        var edge2 = Triangulation[cellType, i + 2];

                        Vector3 a = Interp(edge0, GetEdgeCornerIsoValue(grid, x, y, z, edge0, 0), GetEdgeCornerIsoValue(grid, x, y, z, edge0, 1), isoLevel);
                        Vector3 b = Interp(edge1, GetEdgeCornerIsoValue(grid, x, y, z, edge1, 0), GetEdgeCornerIsoValue(grid, x, y, z, edge1, 1), isoLevel);
                        Vector3 c = Interp(edge2, GetEdgeCornerIsoValue(grid, x, y, z, edge2, 0), GetEdgeCornerIsoValue(grid, x, y, z, edge2, 1), isoLevel);

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
                pos.z = -grid.extents.z / 2;
                pos.y += grid.step.y;
            }
            pos.y = -grid.extents.y / 2;
            pos.x += grid.step.x;
        }

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();

        mesh.RecalculateNormals();
        mesh.Optimize();

        return mesh;
    }

    /// <summary>
    /// Gets the iso value of a certain corner in a grid
    /// </summary>
    /// <param name="grid">source grid</param>
    /// <param name="x">index of grid node</param>
    /// <param name="y">index of grid node</param>
    /// <param name="z">index of grid node</param>
    /// <param name="edgeIndex">index of edge</param>
    /// <param name="cornerEdge">index of edge corner (0 / 1)</param>
    /// <returns></returns>
    public static float GetEdgeCornerIsoValue(Grid grid, int x, int y, int z, int edgeIndex, int cornerEdge)
    {
        Vector3Int corner = CubeCorners[EdgeCornerIdx[edgeIndex, cornerEdge]];
        x += corner.x;
        y += corner.y;
        z += corner.z;
        return grid[x, y, z];
    }

    public static Vector3 Interp(int cubeEdge, float a, float b, float isoLevel)
    {
        Vector3 p0 = CubeCorners[EdgeCornerIdx[cubeEdge, 0]];
        if (a == b) return p0;
        Vector3 p1 = CubeCorners[EdgeCornerIdx[cubeEdge, 1]];
        float t = Mathf.Min(Mathf.Max(0, (isoLevel - a) / (b - a)), 1);
        return p0 + t * (p1 - p0);
    }


    public static int GetCellType(Grid grid, int x, int y, int z, float isoLevel)
    {
        //0 - 255
        int cellType = 0;
        if (grid[x, y, z] < isoLevel)
        {
            cellType |= 1;
        }
        if (grid[x + 1, y, z] < isoLevel)
        {
            cellType |= 2;
        }
        if (grid[x + 1, y, z + 1] < isoLevel)
        {
            cellType |= 4;
        }
        if (grid[x, y, z + 1] < isoLevel)
        {
            cellType |= 8;
        }
        if (grid[x, y + 1, z] < isoLevel)
        {
            cellType |= 16;
        }
        if (grid[x + 1, y + 1, z] < isoLevel)
        {
            cellType |= 32;
        }
        if (grid[x + 1, y + 1, z + 1] < isoLevel)
        {
            cellType |= 64;
        }
        if (grid[x, y + 1, z + 1] < isoLevel)
        {
            cellType |= 128;
        }
        return cellType;
    }
}

