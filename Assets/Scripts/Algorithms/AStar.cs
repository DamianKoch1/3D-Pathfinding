using Pathfinding.Containers;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding.Algorithms
{
    /// <summary>
    /// One directional A* Algorithm that uses cost and estimate functions to find an optimal path
    /// </summary>
    public static class AStar
    {
        /// <summary>
        /// Tries to find a path from start to goal node using settings
        /// </summary>
        /// <param name="start">Start node</param>
        /// <param name="goal">Goal node</param>
        /// <param name="settings">PathfindingSettings (which heuristics to use etc)</param>
        /// <param name="isoLevel">Determines which nodes are considered walkable (iso value > this)</param>
        /// <param name="openNodes">Nodes that were still open after algorithm finished</param>
        /// <param name="closedNodes">Nodes that were fully explored after algorithm finished</param>
        /// <param name="maxIterations">Max number of loop iterations, prevents infinite loops</param>
        /// <param name="nodeCount">Approximate total node count, determines start capacity of path stack</param>
        /// <returns></returns>
        public static Stack<Vector3> FindPath(Node start, Node goal, PathfindingSettings settings, float isoLevel, out BucketList<Node> openNodes, out HashSet<Node> closedNodes, int maxIterations = 50000, int nodeCount = 1000)
        {
            int neighbourChecks = 0;

            int numIterations = 0;

            //euclidean distance from start to end
            float distance = Vector3.Distance(start.pos, goal.pos); 

            //full length of path
            float pathLength = 0;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            if (settings.benchmark)
            {
                sw.Start();
            }

            var startCapacity = nodeCount / 100;
            Stack<Vector3> path = new Stack<Vector3>(startCapacity);

            openNodes = new BucketList<Node>((int)distance / 100, Mathf.Min(settings.Heuristic(start.pos, goal.pos) * settings.greediness, settings.CostIncrease(start.pos, goal.pos) * (1 - settings.greediness)));
            //openNodes = new MinHeap<Node>(startCapacity * 10);

            closedNodes = new HashSet<Node>();

            Node current = null;
            start.costHeuristicBalance = settings.greediness;
            start.cost = 0;
            start.parent = start;
            start.heuristic = settings.Heuristic(start.pos, goal.pos);
            openNodes.Add(start);
            while (openNodes.Count != 0 && !closedNodes.Contains(goal))
            {
                if (++numIterations == maxIterations) break;

                current = openNodes.ExtractMin();

                closedNodes.Add(current);

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
                                neighbour.parent = current;
                                neighbour.heuristic = settings.Heuristic(neighbour.pos, goal.pos);
                                neighbour.cost = current.cost + settings.CostIncrease(current.pos, neighbour.pos);

                                openNodes.Add(neighbour);
                            }
                        }
                    }
                }
            }

            if (!closedNodes.Contains(goal))
            {
                Debug.Log("no goal, " + numIterations + " iterations, closed: " + closedNodes.Count + ", opened: " + openNodes.Count);
                return path;
            }

            path.Push(goal.pos);
            Node temp = goal.parent;
            while (temp != null)
            {
                pathLength += Vector3.Distance(path.Peek(), temp.pos);
                path.Push(temp.pos);
                if (temp == start) break;
                temp = temp.parent;
            }

            if (settings.benchmark)
            {
                sw.Stop();
                Debug.Log("A*, Heuristic: " + settings.heuristic + ", Cost increase: " + settings.costIncrease + ", Path length: " + pathLength * 100 / distance + "%, ms: " + sw.Elapsed.Milliseconds + ", closed: " + closedNodes.Count + ", opened: " + openNodes.Count + ", Neighbour checks: " + neighbourChecks);
            }

            return path;
        }


    }
}