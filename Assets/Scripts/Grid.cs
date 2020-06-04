﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Cubic / Cuboid grid of nodes
    /// </summary>
    public class Grid : INodeGraph
    {
        /// <summary>
        /// Used to get iso value of node with corresponding index, checks neighbours if index out of range, 1 if no neighbour
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public float this[int x, int y, int z]
        {
            get
            {
                if (x >= xSize)
                {
                    if (xNeighbour == null) return 1;
                    return xNeighbour[x - xSize, y, z];
                }
                if (y >= ySize)
                {
                    if (yNeighbour == null) return 1;
                    return yNeighbour[x, y - ySize, z];
                }
                if (z >= zSize)
                {
                    if (zNeighbour == null) return 1;
                    return zNeighbour[x, y, z - zSize];
                }
                return nodes[x, y, z].isoValue;
            }
        }

        public IEnumerable<Node> Nodes => nodes.Cast<Node>();


        public Node[,,] nodes;

        public Vector3 center;

        public Vector3 extents;

        public Vector3 step;

        public int xSize;
        public int ySize;
        public int zSize;

        //preventing chunk gaps
        public Grid xNeighbour;
        public Grid yNeighbour;
        public Grid zNeighbour;

        /// <summary>
        /// Builds a grid around center with given settings
        /// </summary>
        /// <param name="_center">Grid center</param>
        /// <param name="settings">Grid settings (size / step etc)</param>
        /// <param name="GetIsoValue">Function that nodes use to determine their iso value</param>
        public Grid(Vector3 _center, GridGenerationSettings settings, Func<Vector3, float> GetIsoValue)
        {
            settings.step.x = Mathf.Max(settings.step.x, 0.5f);
            settings.step.y = Mathf.Max(settings.step.y, 0.5f);
            settings.step.z = Mathf.Max(settings.step.z, 0.5f);

            center = _center;
            extents = settings.chunkSize;
            step = settings.step;
            xSize = (int)(extents.x / step.x);
            ySize = (int)(extents.y / step.y);
            zSize = (int)(extents.z / step.z);

            nodes = new Node[xSize, ySize, zSize];

            Vector3 pos = center - extents / 2;
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        nodes[x, y, z] = new Node(pos, GetIsoValue(pos), x, y, z);
                        pos.z += step.z;
                    }
                    pos.z = center.z - extents.z / 2;
                    pos.y += step.y;
                }
                pos.y = center.y - extents.y / 2;
                pos.x += step.x;
            }

            foreach (var node in nodes)
            {
                StoreNeighbours(node, settings.allowDiagonalNeighbours);
            }
        }

        public void DrawGizmos(Color color, float isoLevel)
        {
            foreach (var node in nodes)
            {
                node.DrawGizmos(color, isoLevel);
            }
        }

        /// <summary>
        /// Adds grid neighbour to node, direction ints should be in [-1, 1]
        /// </summary>
        /// <param name="node"></param>
        /// <param name="dirX"></param>
        /// <param name="dirY"></param>
        /// <param name="dirZ"></param>
        private void AddNeighbour(Node node, int dirX, int dirY, int dirZ)
        {
            int x = node.x + dirX;
            if (x < 0 || x > xSize - 1) return;

            int y = node.y + dirY;
            if (y < 0 || y > ySize - 1) return;

            int z = node.z + dirZ;
            if (z < 0 || z > zSize - 1) return;

            node.neighbours.Add(nodes[x, y, z]);
        }

        /// <summary>
        /// Stores all direct neighbours within this grid for a node
        /// </summary>
        /// <param name="node">Node that should save its neighbours</param>
        /// <param name="includeDiagonal">Whether to include diagonal neighbours or straight only</param>
        public void StoreNeighbours(Node node, bool includeDiagonal = true)
        {
            if (node.x < 0 || node.y < 0 || node.z < 0) return;
            AddNeighbour(node, -1, 0, 0);
            AddNeighbour(node, 1, 0, 0);
            AddNeighbour(node, 0, -1, 0);
            AddNeighbour(node, 0, 1, 0);
            AddNeighbour(node, 0, 0, -1);
            AddNeighbour(node, 0, 0, 1);
            if (includeDiagonal)
            {
                AddNeighbour(node, -1, 1, -1);
                AddNeighbour(node, -1, 1, 0);
                AddNeighbour(node, -1, 1, 1);
                AddNeighbour(node, 0, 1, 1);
                AddNeighbour(node, 1, 1, 1);
                AddNeighbour(node, 1, 1, 0);
                AddNeighbour(node, 1, 1, -1);
                AddNeighbour(node, 0, 1, -1);
                AddNeighbour(node, 1, 0, -1);
                AddNeighbour(node, 1, 0, 1);
                AddNeighbour(node, -1, 0, 1);
                AddNeighbour(node, -1, 0, -1);
                AddNeighbour(node, -1, -1, -1);
                AddNeighbour(node, -1, -1, 0);
                AddNeighbour(node, -1, -1, 1);
                AddNeighbour(node, 0, -1, 1);
                AddNeighbour(node, 1, -1, 1);
                AddNeighbour(node, 1, -1, 0);
                AddNeighbour(node, 1, -1, -1);
                AddNeighbour(node, 0, -1, -1);
            }
        }


        /// <summary>
        /// Only call this after each grid has figured out its and its nodes neighbours, ignoring diagonal chunk neighbours for now, shouldn't make a noticeable difference
        /// </summary>
        public void StoreCrossChunkNeighbours()
        {
            if (xNeighbour != null)
            {
                for (int y = 0; y < ySize; y++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        MergeNeighbours(nodes[xSize - 1, y, z], xNeighbour.nodes[0, y, z]);
                    }
                }
            }

            if (yNeighbour != null)
            {
                for (int x = 0; x < xSize; x++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        MergeNeighbours(nodes[x, ySize - 1, z], yNeighbour.nodes[x, 0, z]);
                    }
                }
            }

            if (zNeighbour != null)
            {
                for (int x = 0; x < xSize; x++)
                {
                    for (int y = 0; y < ySize; y++)
                    {
                        MergeNeighbours(nodes[x, y, zSize - 1], zNeighbour.nodes[x, y, 0]);
                    }
                }
            }

        }

        /// <summary>
        /// Merges neighbours of node1 and node2, Results in nodes being 2 steps away getting added too, shouldn't matter too much for pathfinding
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
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

        //TODO error if node isnt walkable, should also find adjacent node closest to target instead
        /// <summary>
        /// Calculates local position of position in this chunk, rounds it to node index
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Node GetClosestNode(Vector3 position)
        {
            Vector3 startCorner = center - extents / 2;


            int x = Mathf.RoundToInt((position.x - startCorner.x) / step.x);
            x = Mathf.Clamp(x, 0, xSize - 1);

            int y = Mathf.RoundToInt((position.y - startCorner.y) / step.y);
            y = Mathf.Clamp(y, 0, ySize - 1);

            int z = Mathf.RoundToInt((position.z - startCorner.z) / step.z);
            z = Mathf.Clamp(z, 0, zSize - 1);

            return nodes[x, y, z];
        }

        /// <summary>
        /// Resets cost / heuristic values of all nodes, not calling this before finding another similar path might speed it up
        /// </summary>
        public void ResetNodes()
        {
            foreach (var node in nodes)
            {
                if (node.F != -1)
                {
                    node.cost = -1;
                    node.heuristic = -1;
                }
            }
        }


        /// <summary>
        /// Only updates nodes where necessary, doesnt rebuild whole grid
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="GetIsoValue"></param>
        public void Update(GridGenerationSettings settings, Func<Vector3, float> GetIsoValue)
        {
            Vector3 pos = center - extents / 2;
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        if (nodes[x, y, z] == null)
                        {
                            nodes[x, y, z] = new Node(pos, GetIsoValue(pos), x, y, z);
                        }
                        else
                        {
                            nodes[x, y, z].pos = pos;
                            nodes[x, y, z].isoValue = GetIsoValue(pos);
                        }
                        pos.z += step.z;
                    }
                    pos.z = center.z - extents.z / 2;
                    pos.y += step.y;
                }
                pos.y = center.y - extents.y / 2;
                pos.x += step.x;
            }

            foreach (var node in nodes)
            {
                if (node.neighbours.Count == 26) continue;
                StoreNeighbours(node, settings.allowDiagonalNeighbours);
            }
        }
    }
}
