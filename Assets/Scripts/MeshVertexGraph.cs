using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshVertexGraph : INodeGraph
{
    public Dictionary<Vector3, Node> nodes;

    public IEnumerable<Node> Nodes => nodes.Values;

    public MeshVertexGraph xNeighbour;
    public MeshVertexGraph yNeighbour;
    public MeshVertexGraph zNeighbour;

    public Bounds bounds;

    public MeshVertexGraph(MeshFilter filter, Bounds _bounds)
    {
        nodes = new Dictionary<Vector3, Node>();
        var mesh = filter.sharedMesh;

        var localToWorldMatrix = filter.transform.localToWorldMatrix;

        var verts = mesh.vertices;
        var tris = mesh.triangles;

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
            var node0 = nodes[localToWorldMatrix.MultiplyPoint3x4(verts[tris[i + 0]])];
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
                if ((max.x - node.pos.x) < 0.001f)
                {
                    if (xNeighbour.nodes.ContainsKey(node.pos))
                    {
                        MergeNeighbours(node, xNeighbour.nodes[node.pos]);
                    }
                }
            }

            if (yNeighbour != null)
            {
                if ((max.y - node.pos.y) < 0.001f)
                {
                    if (yNeighbour.nodes.ContainsKey(node.pos))
                    {
                        MergeNeighbours(node, yNeighbour.nodes[node.pos]);
                    }
                }
            }

            if (zNeighbour != null)
            {
                if ((max.z - node.pos.z) < 0.001f)
                {
                    if (zNeighbour.nodes.ContainsKey(node.pos))
                    {
                        MergeNeighbours(node, zNeighbour.nodes[node.pos]);
                    }
                }
            }
        }
    }

    private void MergeNeighbours(Node node1, Node node2)
    {
        foreach (var neighbour in node1.neighbours)
        {
            node2.neighbours.Add(neighbour);
        }
        foreach (var neighbour in node2.neighbours)
        {
            node1.neighbours.Add(neighbour);
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