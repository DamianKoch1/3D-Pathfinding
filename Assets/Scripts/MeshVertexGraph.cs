using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Non-uniform any-angle graph of nodes formed from the vertices of a mesh
    /// </summary>
    public class MeshVertexGraph : INodeGraph
    {
        public Dictionary<Vector3, Node> nodes;

        public IEnumerable<Node> Nodes => nodes.Values;

        public MeshVertexGraph xNeighbour;
        public MeshVertexGraph yNeighbour;
        public MeshVertexGraph zNeighbour;

        public Bounds bounds;

        /// <summary>
        /// Creates nodes for each vertex in the mesh, gets neighbours from triangle information
        /// </summary>
        /// <param name="filter">Mesh filter that contains the target mesh, need access to its transform to calculate world positions</param>
        /// <param name="_bounds">Bounds of owning chunk, used to check which nodes are on the bounds (and need to know cross chunk neighbours)</param>
        public MeshVertexGraph(MeshFilter filter, Bounds _bounds)
        {
            nodes = new Dictionary<Vector3, Node>();

            bounds = _bounds;

            var mesh = filter.sharedMesh;

            var localToWorldMatrix = filter.transform.localToWorldMatrix;

            var verts = mesh.vertices;
            var tris = mesh.triangles;
            var normals = mesh.normals;

            var vertexCount = verts.Length;
            for (int i = 0; i < vertexCount; i++)
            {
                var pos = localToWorldMatrix.MultiplyPoint3x4(verts[i]);
                if (nodes.ContainsKey(pos)) continue;
                nodes.Add(pos, new Node(pos, 1));
            }

            var triangleCount = tris.Length;
            for (int i = 0; i < triangleCount; i += 3)
            {
                var node0 = nodes[localToWorldMatrix.MultiplyPoint3x4(verts[tris[i]])];
                var node1 = nodes[localToWorldMatrix.MultiplyPoint3x4(verts[tris[i + 1]])];
                var node2 = nodes[localToWorldMatrix.MultiplyPoint3x4(verts[tris[i + 2]])];

                node0.neighbours.Add(node1);
                node0.neighbours.Add(node2);

                node1.neighbours.Add(node0);
                node1.neighbours.Add(node2);

                node2.neighbours.Add(node0);
                node2.neighbours.Add(node1);
            }
        }

        /// <summary>
        /// Sorts nodes by distance to position and returns the first one
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Node GetClosestNode(Vector3 position)
        {
            var orderedNodes = nodes.Values.OrderBy(n => (n.pos - position).sqrMagnitude);
            return orderedNodes.First();
        }

        public void ResetNodes()
        {
            foreach (var node in nodes.Values)
            {
                if (node.F != -1)
                {
                    node.cost = -1;
                    node.heuristic = -1;
                }
            }
        }

        public void StoreCrossChunkNeighbours()
        {
            var max = bounds.max;
            foreach (var node in nodes.Values)
            {
                if (xNeighbour != null)
                {
                    if ((max.x - node.pos.x) < 0.01f)
                    {
                        //dictionary lookup isn't reliable enough
                        if (!xNeighbour.nodes.TryGetValue(node.pos, out var neighbour))
                        {
                            neighbour = xNeighbour.GetClosestNode(node.pos);
                        }
                        Node.MergeNeighbours(node, neighbour);
                    }
                }

                if (yNeighbour != null)
                {
                    if ((max.y - node.pos.y) < 0.01f)
                    {
                        if (!yNeighbour.nodes.TryGetValue(node.pos, out var neighbour))
                        {
                            neighbour = yNeighbour.GetClosestNode(node.pos);
                        }
                        Node.MergeNeighbours(node, yNeighbour.GetClosestNode(node.pos));
                    }
                }

                if (zNeighbour != null)
                {
                    if ((max.z - node.pos.z) < 0.01f)
                    {
                        if (!zNeighbour.nodes.TryGetValue(node.pos, out var neighbour))
                        {
                            neighbour = zNeighbour.GetClosestNode(node.pos);
                        }
                        Node.MergeNeighbours(node, neighbour);
                    }
                }
            }
        }
    }

    [System.Serializable]
    public struct NavmeshHit
    {
        public Vector3 point;
        public int triangleIndex;
        public Vector3 normal;

        [Tooltip("1,0,0 = 1st triangle corner, 0,1,0 2nd, 0,0,1 3rd")]
        public Vector3 barycentric;

        public NavmeshHit(RaycastHit hit)
        {
            point = hit.point;
            triangleIndex = hit.triangleIndex;
            barycentric = hit.barycentricCoordinate;
            normal = hit.normal;
        }
    }
}