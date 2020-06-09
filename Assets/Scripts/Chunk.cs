using SimplexNoise;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Pathfinding.NavMesh;
using Pathfinding.Serialization;

namespace Pathfinding
{
    /// <summary>
    /// One cell of the NavMesh, stores a grid and both NavMesh and its corresponding node graph
    /// </summary>
    public class Chunk : MonoBehaviour
    {
        public Grid grid;

        public MeshVertexGraph graph;

        public int x;

        public int y;

        public int z;

        public Chunk xNeighbour;
        public Chunk yNeighbour;
        public Chunk zNeighbour;


        [SerializeField]
        private bool drawNodes;

        [SerializeField]
        private bool drawNeighbours;

        private bool visualizePathfinding;

        private GridGenerationSettings gridSettings;

        private Bounds bounds;

        /// <summary>
        /// Sets local position / name according to index, calculates bounds and node array dimensions
        /// </summary>
        /// <param name="settings">GridGenerationSettings (grid size / step etc)</param>
        /// <param name="_x">X index</param>
        /// <param name="_y">Y index</param>
        /// <param name="_z">Z index</param>
        public void Initialize(GridGenerationSettings settings, int _x, int _y, int _z)
        {
            gridSettings = settings;
            x = _x;
            y = _y;
            z = _z;
            var maxX = (int)(gridSettings.chunkSize.x / gridSettings.step.x);
            var maxY = (int)(gridSettings.chunkSize.y / gridSettings.step.y);
            var maxZ = (int)(gridSettings.chunkSize.z / gridSettings.step.z);
            transform.localPosition = new Vector3(x * maxX * gridSettings.step.x, y * maxY * gridSettings.step.y, z * maxZ * gridSettings.step.z) + gridSettings.chunkSize / 2;
            bounds = new Bounds(transform.position, gridSettings.chunkSize);
            gameObject.name = "Chunk " + new Vector3Int(x, y, z);
        }

        /// <summary>
        /// Creates Grid using settings passed from Initialize
        /// </summary>
        public void GenerateGrid()
        {
            System.Func<Vector3, float> GetIsoValue = null;
            switch (gridSettings.mode)
            {
                case GenerationMode.Overlap:
                    GetIsoValue = (Vector3 pos) =>
                    {
                        var overlaps = Physics.OverlapSphere(pos, gridSettings.navMeshOffset, LayerMask.GetMask(NodesGenerator.OBSTACLE_LAYER));
                        if (overlaps.Length == 0) return 1;
                        var nearest = overlaps.OrderBy(o => Vector3.Distance(pos, o.ClosestPoint(pos))).First();
                        return Vector3.Distance(pos, nearest.ClosestPoint(pos)) / gridSettings.navMeshOffset;
                    };
                    break;
                case GenerationMode.Noise:
                    GetIsoValue = (Vector3 pos) => Noise.CalcPixel3D(pos.x, pos.y, pos.z, gridSettings.scale) / 255f;
                    break;
            }

            grid = new Grid(transform.position, gridSettings, GetIsoValue, this);
        }

        /// <summary>
        /// Creates node graph using mesh from attached Mesh filter and bounds
        /// </summary>
        public void GenerateGraph()
        {
            graph = new MeshVertexGraph(GetComponent<MeshFilter>(), bounds, this);
        }

        public void AssignNeighbours()
        {
            grid?.AssignNeighbours();
            graph?.AssignNeighbours();
        }

        /// <summary>
        /// Clears grid / node graph, resets mesh filter / collider
        /// </summary>
        public void Clear()
        {
            grid = null;
            graph = null;
            GetComponent<MeshFilter>().sharedMesh = null;
            GetComponent<MeshCollider>().sharedMesh = null;
        }


        private void OnDrawGizmos()
        {
            if (!gridSettings) return;
            if (gridSettings.drawExtents)
            {
                Gizmos.DrawWireCube(transform.position, gridSettings.chunkSize);
            }
            if (grid != null)
            {
                if (gridSettings.drawNodes)
                {
                    grid.DrawGizmos(gridSettings.nodeColor, gridSettings.isoLevel);
                }
            }

            if (graph?.nodes != null)
            {
                if (drawNodes || drawNeighbours)
                {
                    foreach (var node in graph.nodes.Values)
                    {
                        if (drawNodes)
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawWireCube(node.pos, Vector3.one * 0.1f);
                        }
                        if (drawNeighbours && node.neighbours != null)
                        {
                            Gizmos.color = Color.magenta;
                            foreach (var neighbour in node.neighbours)
                            {
                                Gizmos.DrawLine(node.pos, neighbour.pos);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Left here for documentation, creates a combined mesh of all meshes inside bounds expanded by a set amount, not suitable for NavMesh due to overlaps
        /// </summary>
        public void ExpandMeshes()
        {
            if (!gridSettings) return;
            var combine = new List<CombineInstance>();
            var filter = GetComponent<MeshFilter>();
            var collider = GetComponent<MeshCollider>();
            foreach (var obj in Physics.OverlapBox(transform.position, gridSettings.chunkSize / 2, Quaternion.identity, ~LayerMask.GetMask(NodesGenerator.NAVMESH_LAYER)))
            {
                var mesh = obj.GetComponent<MeshFilter>()?.sharedMesh;
                if (!mesh) continue;
                var scale = obj.transform.lossyScale;
                var newMesh = new Mesh();
                var verts = mesh.vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    verts[i] += new Vector3
                        (
                            Mathf.Sign(verts[i].x) / scale.x,
                            Mathf.Sign(verts[i].y) / scale.y,
                            Mathf.Sign(verts[i].z) / scale.z
                        ) * gridSettings.navMeshOffset;
                    verts[i] = obj.transform.localToWorldMatrix.MultiplyPoint3x4(verts[i]);
                    verts[i] -= transform.position;
                }
                newMesh.vertices = verts;
                newMesh.triangles = mesh.triangles;
                combine.Add(new CombineInstance() { mesh = newMesh });
            }
            var combinedMesh = new Mesh();
            combinedMesh.CombineMeshes(combine.ToArray(), true, false);
            combinedMesh.RecalculateNormals();
            combinedMesh.Optimize();
            filter.mesh = combinedMesh;
            collider.sharedMesh = combinedMesh;
        }

        /// <summary>
        /// Builds NavMesh from grid using MarchingCubes algorithm
        /// </summary>
        public void MarchCubes()
        {
            if (!gridSettings) return;
            var filter = GetComponent<MeshFilter>();
            var collider = GetComponent<MeshCollider>();
            filter.sharedMesh = null;
            collider.sharedMesh = null;
            if (grid == null)
            {
                GenerateGrid();
            }
            var mesh = MarchingCubes.March(grid, gridSettings.isoLevel);
            filter.sharedMesh = mesh;
            collider.sharedMesh = mesh;
        }
    }


    [System.Serializable]
    public class FlattenedChunk3DArray : Flattened3DArray<Chunk>
    {
        public FlattenedChunk3DArray(Flattened3DArray<Chunk> other) : base(other)
        {
        }
    }
}
