using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshVertexGraph : INodeGraph
{
    public Dictionary<Vector3, Node> nodes;

    IEnumerable<Node> INodeGraph.Nodes => nodes.Values;

    public MeshVertexGraph(MeshFilter filter)
    {
        nodes = new Dictionary<Vector3, Node>();
        var mesh = filter.sharedMesh;

        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            if (nodes.ContainsKey(mesh.vertices[i])) continue;
            nodes[mesh.vertices[i]] = new Node(filter.transform.localToWorldMatrix.MultiplyPoint3x4(mesh.vertices[i]), 1);
        }

        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            var node0 = GetTriangleNode(mesh, i, 0);
            var node1 = GetTriangleNode(mesh, i, 1);
            var node2 = GetTriangleNode(mesh, i, 2);

            node0.neighbours.Add(node1);
            node0.neighbours.Add(node2);

            node1.neighbours.Add(node0);
            node1.neighbours.Add(node2);

            node2.neighbours.Add(node0);
            node2.neighbours.Add(node1);
        }
    }

    /// <summary>
    /// Returns node with position of triangle corner
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="triangle">triangle index</param>
    /// <param name="corner">triangle corner index (0 - 2)</param>
    private Node GetTriangleNode(Mesh mesh, int triangle, int corner)
    {
        return nodes[mesh.vertices[mesh.triangles[triangle + corner]]];
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

    public LinkedList<Node> openNodes;
    public LinkedList<Node> closedNodes;
    public int neighbourChecks;

   
    public Stack<Vector3> FindPath(Vector3 start, Vector3 end, PathfindingSettings settings)
    {
        return AStar.FindPath(this, start, end, settings, -1, out neighbourChecks, out openNodes, out closedNodes);

    }


}

