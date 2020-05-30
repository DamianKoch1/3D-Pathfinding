using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshVertexGraph : INodeGraph
{
    public Dictionary<Vector3, Node> nodes;

    public IEnumerable<Node> Nodes => nodes.Values;


    public MeshVertexGraph(MeshFilter filter)
    {
        nodes = new Dictionary<Vector3, Node>();
        var mesh = filter.sharedMesh;

        //mesh.vertices / .triangles returns array copy
        var verts = mesh.vertices;
        var tris = mesh.triangles;


        var vertexCount = verts.Length;
        for (int i = 0; i < vertexCount; i++)
        {
            if (nodes.ContainsKey(verts[i])) continue;
            nodes.Add(verts[i], new Node(filter.transform.localToWorldMatrix.MultiplyPoint3x4(verts[i]), 1));
        }

        var triangleCount = tris.Length;
        for (int i = 0; i < triangleCount; i += 3)
        {
            var node0 = nodes[verts[tris[i + 0]]];
            var node1 = nodes[verts[tris[i + 1]]];
            var node2 = nodes[verts[tris[i + 2]]];

            node0.AddUniqueNeighbour(node1);
            node0.AddUniqueNeighbour(node2);

            node1.AddUniqueNeighbour(node0);
            node1.AddUniqueNeighbour(node2);

            node2.AddUniqueNeighbour(node0);
            node2.AddUniqueNeighbour(node1);
        }
    }

    public Node GetClosestNode(Vector3 position)
    {
        var orderedNodes = nodes.Values.OrderBy(n => Vector3.Distance(n.pos, position));
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
   
}

[System.Serializable]
public struct NavmeshHit
{
    public Vector3 point;
    public int triangleIndex;

    [Tooltip("1,0,0 = 1st triangle corner, 0,1,0 2nd, 0,0,1 3rd")]
    public Vector3 barycentric;

    public NavmeshHit(RaycastHit hit)
    {
        point = hit.point;
        triangleIndex = hit.triangleIndex;
        barycentric = hit.barycentricCoordinate;
    }
}