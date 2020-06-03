using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AStar
{
    public static Stack<Vector3> FindPath(Node start, Node goal, PathfindingSettings settings, float isoLevel, out SortedSet<Node> openNodes, out HashSet<Node> closedNodes, int maxIterations = 50000, int nodeCount = 1000)
    {
        int neighbourChecks = 0;

        int numIterations = 0;

        //euclidean distance from start to end
        float distance = 0;

        //full length of path
        float pathLength = 0;

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        if (settings.benchmark)
        {
            distance = Vector3.Distance(start.pos, goal.pos);
            sw.Start();
        }

        var startCapacity = nodeCount / 100;
        Stack<Vector3> path = new Stack<Vector3>(startCapacity);

        //much faster .Contains than list
        openNodes = new SortedSet<Node>();

        closedNodes = new HashSet<Node>();

        Node current = null;
        start.costHeuristicBalance = settings.greediness;
        start.cost = 0;
        start.heuristic = settings.Heuristic(start.pos, goal.pos);
        openNodes.Add(start);
        while (openNodes.Count != 0 && !closedNodes.Contains(goal))
        {
            if (++numIterations == maxIterations) break;
            var first = openNodes.First();
            current = first;
            openNodes.Remove(first);
            closedNodes.Add(first);
            closedNodes.Contains(first);

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
                            neighbour.heuristic = settings.Heuristic(neighbour.pos, goal.pos);
                            neighbour.cost = current.cost + settings.CostIncrease(neighbour);

                            openNodes.Add(neighbour);
                        }
                    }
                }
            }
        }

        if (!closedNodes.Contains(goal))
        {
            Debug.Log("no goal, " + numIterations + " iterations");
            return path;
        }

        Node temp = current;
        path.Push(goal.pos);
        while (temp != null)
        {
            pathLength += Vector3.Distance(path.Peek(), temp.pos);
            path.Push(temp.pos);
            temp = temp.previousPathNode;
            if (temp.pos == start.pos) break;
        }
        pathLength += Vector3.Distance(path.Peek(), start.pos);

        if (settings.benchmark)
        {
            sw.Stop();
            Debug.Log("Heuristic: " + settings.heuristic + ", Cost increase: " + settings.costIncrease + ", Path length: " + pathLength * 100 / distance + "%, ms: " + sw.Elapsed.Milliseconds + ", closed: " + closedNodes.Count + ", visited: " + openNodes.Count + ", Neighbour checks: " + neighbourChecks);
        }

        return path;
    }
}
