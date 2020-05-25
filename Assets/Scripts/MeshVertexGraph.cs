using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshVertexGraph
{
    public Dictionary<Vector3, Node> nodes;

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

    public List<Node> openNodes;
    public List<Node> closedNodes;
    public int neighbourChecks;
    public Stack<Vector3> FindPath(Vector3 start, Vector3 end, PathfindingSettings settings)
    {
        neighbourChecks = 0;

        ResetNodes();

        //distance from start to end
        float distance = 0;

        //full length of path
        float pathLength = 0;

        System.Diagnostics.Stopwatch sw = null;

        if (settings.benchmark)
        {
            distance = Vector3.Distance(start, end);
            sw = System.Diagnostics.Stopwatch.StartNew();
        }

        var startNode = GetClosestNode(start);
        var endNode = GetClosestNode(end);

        var startCapacity = nodes.Count / 10;
        Stack<Vector3> path = new Stack<Vector3>(startCapacity);

        openNodes = new List<Node>(startCapacity);
        closedNodes = new List<Node>(startCapacity);

        Node current = startNode;
        current.costHeuristicBalance = settings.greediness;
        current.cost = 0;
        current.heuristic = settings.Heuristic(current.pos, endNode.pos);
        openNodes.Add(startNode);

        while (openNodes.Count != 0 && !closedNodes.Contains(endNode))
        {
            current = openNodes[0];
            openNodes.RemoveAt(0);
            closedNodes.Add(current);

            foreach (var neighbour in current.neighbours)
            {
                neighbourChecks++;
                if (!closedNodes.Contains(neighbour))
                {
                    if (!openNodes.Contains(neighbour))
                    {
                        neighbour.costHeuristicBalance = settings.greediness;
                        neighbour.previousPathNode = current;
                        neighbour.heuristic = settings.Heuristic(neighbour.pos, endNode.pos);
                        neighbour.cost = neighbour.previousPathNode.cost + settings.CostIncrease(neighbour);
                        openNodes.Add(neighbour);
                        openNodes = openNodes.OrderBy(n => n.F).ToList();
                    }
                }
            }
        }

        if (!closedNodes.Contains(endNode))
        {
            return path;
        }

        Node temp = current;
        path.Push(end);
        while (temp != null)
        {
            if (!path.Contains(temp.pos))
            {
                pathLength += Vector3.Distance(path.Peek(), temp.pos);
                path.Push(temp.pos);
                if (temp == startNode)
                {
                    break;
                }
            }
            temp = temp.previousPathNode;
        }
        pathLength += Vector3.Distance(path.Peek(), start);

        if (settings.benchmark)
        {
            sw.Stop();
            Debug.Log("Heuristic: " + settings.heuristic + ", Cost increase: " + settings.costIncrease + ", Path length: " + pathLength * 100 / distance + "%, ms: " + sw.Elapsed.Milliseconds + ", closed: " + closedNodes.Count + ", visited: " + openNodes.Count + ", Neighbour checks: " + neighbourChecks);
        }

        return path;

    }


}

