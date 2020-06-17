using System.Collections.Generic;
using UnityEngine;
using static Pathfinding.NavMesh.MarchingTables;

namespace Pathfinding.NavMesh
{
    /// <summary>
    /// Algorithm that generates a mesh from a cuboid grid of nodes
    /// </summary>
    public static class MarchingCubes
    {
        /// <summary>
        /// Creates a mesh from given grid
        /// </summary>
        /// <param name="grid">Grid to build mesh for</param>
        /// <param name="isoLevel">Determines which nodes are walkable (iso value > this)</param>
        /// <returns></returns>
        public static Mesh March(Grid grid, float isoLevel)
        {
            var mesh = new Mesh();
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            var extents = grid.owner.bounds.extents;
            var pos = -extents;
            for (int x = 0; x < grid.xSize; x++)
            {
                for (int y = 0; y < grid.ySize; y++)
                {
                    for (int z = 0; z < grid.zSize; z++)
                    {
                        int cellType = GetCellType(grid, x, y, z, isoLevel);
                        for (int i = 0; Triangulation[cellType, i] != -1; i += 3)
                        {
                            int edge0 = Triangulation[cellType, i];
                            int edge1 = Triangulation[cellType, i + 1];
                            int edge2 = Triangulation[cellType, i + 2];

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
                    pos.z = -extents.z;
                    pos.y += grid.step.y;
                }
                pos.y = -extents.y;
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
        /// <param name="grid">Source grid</param>
        /// <param name="x">Index of grid node</param>
        /// <param name="y">Index of grid node</param>
        /// <param name="z">Index of grid node</param>
        /// <param name="edgeIndex">Index of edge</param>
        /// <param name="cornerEdge">Index of edge corner (0 / 1)</param>
        /// <returns></returns>
        public static float GetEdgeCornerIsoValue(Grid grid, int x, int y, int z, int edgeIndex, int cornerEdge)
        {
            Vector3 corner = CubeCorners[EdgeCornerIdx[edgeIndex, cornerEdge]];
            x += (int)corner.x;
            y += (int)corner.y;
            z += (int)corner.z;
            return grid[x, y, z];
        }

        /// <summary>
        /// Returns a point on given cube edge using corner iso values a and b
        /// </summary>
        /// <param name="cubeEdge">index of cube edge</param>
        /// <param name="a">corner a iso value</param>
        /// <param name="b">corner b iso value</param>
        /// <param name="isoLevel">walkability threshold</param>
        /// <returns></returns>
        public static Vector3 Interp(int cubeEdge, float a, float b, float isoLevel)
        {
            Vector3 p0 = CubeCorners[EdgeCornerIdx[cubeEdge, 0]];
            if (a == b) return p0;
            Vector3 p1 = CubeCorners[EdgeCornerIdx[cubeEdge, 1]];
            float t = Mathf.Min(Mathf.Max(0, (isoLevel - a) / (b - a)), 1);
            return p0 + t * (p1 - p0);
        }

        /// <summary>
        /// Returns corresponding index of lookup table for given combination of (un)blocked corners
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="isoLevel"></param>
        /// <returns></returns>
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
}
