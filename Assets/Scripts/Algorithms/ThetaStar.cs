using Pathfinding.Containers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pathfinding.Algorithms
{
    /// <summary>
    /// Algorithm that finds path similar to A*, but skips nodes where LineOfSight exists between its parent and child, results in more optimal paths
    /// </summary>
    public static class ThetaStar
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

            openNodes = new BucketList<Node>((int)distance / 200);

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
                var first = openNodes.GetLowest();
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
                                neighbour.parent = null;
                                neighbour.cost = Mathf.Infinity;
                                neighbour.heuristic = settings.Heuristic(neighbour.pos, goal.pos);
                            }
                            UpdateNode(current, neighbour, settings, openNodes);
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
                Debug.Log("Theta*, Heuristic: " + settings.heuristic + ", Cost increase: " + settings.costIncrease + ", Path length: " + pathLength * 100 / distance + "%, ms: " + sw.Elapsed.Milliseconds + ", closed: " + closedNodes.Count + ", opened: " + openNodes.Count + ", Neighbour checks: " + neighbourChecks);
            }

            return path;
        }

        /// <summary>
        /// If neighbour has line of sight to currents parent and skipping current is cheaper, skips current node and sets neighbour cost / parent depending on whether current is used
        /// </summary>
        /// <param name="current"></param>
        /// <param name="neighbour"></param>
        /// <param name="settings"></param>
        /// <param name="openNodes"></param>
        private static void UpdateNode(Node current, Node neighbour, PathfindingSettings settings, BucketList<Node> openNodes)
        {
            if (LineOfSight(current.parent, neighbour))
            {
                var costSkippingCurrent = current.parent.cost + settings.CostIncrease(current.parent.pos, neighbour.pos);
                if (costSkippingCurrent < neighbour.cost)
                {
                    if (openNodes.Contains(neighbour))
                    {
                        openNodes.Remove(neighbour);
                    }
                    neighbour.cost = costSkippingCurrent;
                    neighbour.parent = current.parent;
                    openNodes.Add(neighbour);
                }
            }
            else
            {
                var costUsingCurrent = current.cost + settings.CostIncrease(current.pos, neighbour.pos);
                if (costUsingCurrent < neighbour.cost)
                {
                    if (openNodes.Contains(neighbour))
                    {
                        openNodes.Remove(neighbour);
                    }
                    neighbour.cost = costUsingCurrent;
                    neighbour.parent = current;
                    openNodes.Add(neighbour);
                }
            }
        }

        private static bool LineOfSight(Node node1, Node node2)
        {
            if (node1 == null || node2 == null) return false;
            if (!Physics.Linecast(node1.pos, node2.pos, NodesGenerator.NAVMESH_LAYER))
            {
                //doesnt work on non convex navmesh colliders, doesn't use navmesh offset...
                return Physics.OverlapSphere((node1.pos + node2.pos) / 2, 2, NodesGenerator.OBSTACLE_LAYER).Length == 0;
            }
            return false;
        }
    }
}
