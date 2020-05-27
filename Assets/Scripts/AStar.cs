using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AStar
{
    public static Stack<Vector3> FindPath(INodeGraph graph, Vector3 start, Vector3 end, PathfindingSettings settings, float isoLevel, out int neighbourChecks, out SortedSet<Node> openNodes, out HashSet<Node> closedNodes)
    {
        neighbourChecks = 0;

        //euclidean distance from start to end
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

        //much faster .Contains than list
        openNodes = new SortedSet<Node>();
        closedNodes = new HashSet<Node>();


        Node current = startNode;
        current.costHeuristicBalance = settings.greediness;
        current.cost = 0;
        current.heuristic = settings.Heuristic(current.pos, endNode.pos);
        openNodes.Add(startNode);

        while (openNodes.Count != 0 && !closedNodes.Contains(endNode))
        {
            var first = openNodes.First();
            current = first;
            openNodes.Remove(first);
            closedNodes.Add(first);

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

                            openNodes.Add(neighbour);
                        }
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
            pathLength += Vector3.Distance(path.Peek(), temp.pos);
            path.Push(temp.pos);
            temp = temp.previousPathNode;
            if (temp == startNode) break;
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
