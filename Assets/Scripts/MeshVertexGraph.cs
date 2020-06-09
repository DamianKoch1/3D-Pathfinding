using Pathfinding.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Non-uniform any-angle graph of nodes formed from the vertices of a mesh
    /// </summary>
    [Serializable]
    public class MeshVertexGraph : INodeGraph
    {
        public SerializableNodeDictionary nodes;

        public IEnumerable<Node> Nodes => nodes.Values;

        public Chunk owner;

        public Bounds bounds;

        /// <summary>
        /// Creates nodes for each vertex in the mesh, gets neighbours from triangle information
        /// </summary>
        /// <param name="filter">Mesh filter that contains the target mesh, need access to its transform to calculate world positions</param>
        /// <param name="_bounds">Bounds of owning chunk, used to check which nodes are on the bounds (and need to know cross chunk neighbours)</param>
        public MeshVertexGraph(MeshFilter filter, Bounds _bounds, Chunk _owner)
        {
            owner = _owner;

            nodes = new SerializableNodeDictionary();

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

                node0.AddNeighbourIdentifier(node1.pos);
                node0.AddNeighbourIdentifier(node2.pos);

                node1.AddNeighbourIdentifier(node0.pos);
                node1.AddNeighbourIdentifier(node2.pos);

                node2.AddNeighbourIdentifier(node0.pos);
                node2.AddNeighbourIdentifier(node1.pos);
            }
        }

        /// <summary>
        /// Returns node with smallest distance to position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Node GetClosestNode(Vector3 position)
        {
            float lowestSqDistance = Mathf.Infinity;
            Node retVal = null;
            foreach (var node in nodes.Values)
            {
                float newSqDistance = (node.pos - position).sqrMagnitude;
                if ((node.pos - position).sqrMagnitude < lowestSqDistance)
                {
                    retVal = node;
                    lowestSqDistance = newSqDistance;
                }
            }
            return retVal;
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

        public void FindCrossChunkNeighbours()
        {
            var max = bounds.max;
            foreach (var node in nodes.Values)
            {
                if (owner.xNeighbour != null)
                {
                    if ((max.x - node.pos.x) < 0.5f)
                    {
                        //dictionary lookup isn't reliable enough
                        if (!owner.xNeighbour.graph.nodes.TryGetValue(node.pos, out var neighbour))
                        {
                            neighbour = owner.xNeighbour.graph.GetClosestNode(node.pos);
                        }
                        foreach (var identifier in neighbour.neighbourIdentifiers)
                        {
                            node.AddNeighbourIdentifier(identifier.key, ChunkNeighbourType.X);
                        }
                    }
                }

                if (owner.yNeighbour != null)
                {
                    if ((max.y - node.pos.y) < 0.5f)
                    {
                        if (!owner.yNeighbour.graph.nodes.TryGetValue(node.pos, out var neighbour))
                        {
                            neighbour = owner.yNeighbour.graph.GetClosestNode(node.pos);
                        }
                        foreach (var identifier in neighbour.neighbourIdentifiers)
                        {
                            node.AddNeighbourIdentifier(identifier.key, ChunkNeighbourType.Y);
                        }
                    }
                }

                if (owner.zNeighbour != null)
                {
                    if ((max.z - node.pos.z) < 0.5f)
                    {
                        if (!owner.zNeighbour.graph.nodes.TryGetValue(node.pos, out var neighbour))
                        {
                            neighbour = owner.zNeighbour.graph.GetClosestNode(node.pos);
                        }
                        foreach (var identifier in neighbour.neighbourIdentifiers)
                        {
                            node.AddNeighbourIdentifier(identifier.key, ChunkNeighbourType.Z);
                        }
                    }
                }
            }
        }

        public void AssignNeighbours()
        {
            foreach (var node in nodes.Values)
            {
                foreach (var identifier in node.neighbourIdentifiers)
                {
                    switch (identifier.chunkNeighbourType)
                    {
                        case ChunkNeighbourType.Same:
                            node.AddNeighbour(nodes[identifier.key]);
                            break;
                        case ChunkNeighbourType.X:
                            var xNeighbour = owner.xNeighbour.graph.nodes[identifier.key];
                            node.AddNeighbour(xNeighbour);
                            xNeighbour.AddNeighbour(node);
                            break;
                        case ChunkNeighbourType.Y:
                            var yNeighbour = owner.yNeighbour.graph.nodes[identifier.key];
                            node.AddNeighbour(yNeighbour);
                            yNeighbour.AddNeighbour(node);
                            break;
                        case ChunkNeighbourType.Z:
                            var zNeighbour = owner.zNeighbour.graph.nodes[identifier.key];
                            node.AddNeighbour(zNeighbour);
                            zNeighbour.AddNeighbour(node);
                            break;
                    }
                }
            }
        }
    }

    [Serializable]
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