using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Node used in grids / graphs / pathfinding, knows its world position, (grid index), neighbours, cost, heuristic, and previous node of path
    /// </summary>
    public class Node : IComparable<Node>
    {
        public Vector3 pos;

        public Node parent;

        public float heuristic = -1;

        public float cost = -1;

        public int x;
        public int y;
        public int z;

        public float isoValue;

        public HashSet<Node> neighbours;

        public float costHeuristicBalance = 0.5f;

        public float F
        {
            get
            {
                if (heuristic != -1 && cost != -1)
                {
                    return heuristic * costHeuristicBalance + cost * (1 - costHeuristicBalance);
                }
                return -1;
            }
        }

        public Node(Vector3 _pos, float _isoValue, int iX = -1, int iY = -1, int iZ = -1)
        {
            neighbours = new HashSet<Node>();
            pos = _pos;
            x = iX;
            y = iY;
            z = iZ;
            isoValue = _isoValue;
        }

        public void DrawGizmos(Color color, float isoLevel)
        {
            Gizmos.color = isoValue >= isoLevel ? color : Color.red;
            Gizmos.DrawWireCube(pos, Vector3.one * 0.1f);
        }

        /// <summary>
        /// Nodes are considered equal if they share the position, otherwise compare their F (cost + heuristic) values
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Node other)
        {
            if ((other.pos - pos).sqrMagnitude < 0.1f) return 0;
            if (F > other.F) return 1;
            if (F < other.F) return -1;
            return 0;
        }

        /// <summary>
        /// Returns the hashcode of the position
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return pos.GetHashCode();
        }

        /// <summary>
        /// Merges neighbours of node1 and node2, Results in nodes being 2 steps away getting added too, shouldn't matter too much for pathfinding
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        public static void MergeNeighbours(Node node1, Node node2)
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
}
