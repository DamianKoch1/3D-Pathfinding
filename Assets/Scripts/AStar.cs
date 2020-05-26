using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AStar
{
    public static Stack<Vector3> FindPath(INodeGraph graph,Vector3 start, Vector3 end, PathfindingSettings settings, float isoLevel, out int neighbourChecks, out LinkedList<Node> openNodes, out LinkedList<Node> closedNodes)
    {
        graph.ResetNodes();

        neighbourChecks = 0;
        //distance from start to end
        float distance = 0;

        //full length of path
        float pathLength = 0;

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        if (settings.benchmark)
        {
            distance = Vector3.Distance(start, end);
            sw.Start();
        }

        var startNode = graph.GetClosestNode(start);
        var endNode = graph.GetClosestNode(end);

        var startCapacity = graph.Nodes.Count() / 10;
        Stack<Vector3> path = new Stack<Vector3>(startCapacity);

        //linkedlist vs list
        openNodes = new LinkedList<Node>();
        closedNodes = new LinkedList<Node>();

        Node current = startNode;
        current.costHeuristicBalance = settings.greediness;
        current.cost = 0;
        current.heuristic = settings.Heuristic(current.pos, endNode.pos);
        openNodes.AddLast(startNode);

        while (openNodes.Count != 0 && !closedNodes.Contains(endNode))
        {
            current = openNodes.First();
            openNodes.RemoveFirst();
            closedNodes.AddLast(current);

            foreach (var neighbour in current.neighbours)
            {
                neighbourChecks++;
                if (neighbour.isoValue > isoLevel)
                {
                    if (!closedNodes.Contains(neighbour))
                    {
                        if (!openNodes.Contains(neighbour))
                        {
                            neighbour.costHeuristicBalance = settings.greediness;
                            neighbour.previousPathNode = current;
                            neighbour.heuristic = settings.Heuristic(neighbour.pos, endNode.pos);
                            neighbour.cost = neighbour.previousPathNode.cost + settings.CostIncrease(neighbour);
                            openNodes.AddLast(neighbour);
                            openNodes = new LinkedList<Node>(openNodes.OrderBy(n => n.F));
                        }
                    }
                }
            }
        }

        if (!closedNodes.Contains(endNode))
        {
            return null;
        }

        Node temp = current;
        path.Push(end);
        while (temp != null)
        {
            pathLength += Vector3.Distance(path.Peek(), temp.pos);
            path.Push(temp.pos);
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
