using Pathfinding.Serialization;
using System;
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
                    if (owner.xNeighbour == null) return 1;
                    return owner.xNeighbour.grid[x - xSize, y, z];
                }
                if (y >= ySize)
                {
                    if (owner.yNeighbour == null) return 1;
                    return owner.yNeighbour.grid[x, y - ySize, z];
                }
                if (z >= zSize)
                {
                    if (owner.zNeighbour == null) return 1;
                    return owner.zNeighbour.grid[x, y, z - zSize];
                }
                return nodes[x, y, z].isoValue;
            }
        }

        public IEnumerable<Node> Nodes => nodes.items.Cast<Node>();

        //public GridNode[,,] nodes;

        //no noticeable generation performance diff, but serializable
        public FlattenedNode3DArray nodes;

        public Vector3 step;

        public int xSize;
        public int ySize;
        public int zSize;

        public Chunk owner;

        public Grid(Chunk _owner, FlattenedNode3DArray _nodes, Vector3 _step)
        {
            owner = _owner;
            nodes = _nodes;

            xSize = nodes.dimensions[0];
            ySize = nodes.dimensions[1];
            zSize = nodes.dimensions[2];

            step = _step;
        }

        /// <summary>
        /// Builds a grid around center with given settings
        /// </summary>
        /// <param name="_center">Grid center</param>
        /// <param name="settings">Grid settings (size / step etc)</param>
        /// <param name="GetIsoValue">Function that nodes use to determine their iso value</param>
        public Grid(GridGenerationSettings settings, Func<Vector3, float> GetIsoValue, Chunk _owner)
        {
            owner = _owner;
            settings.step.x = Mathf.Max(settings.step.x, 0.5f);
            settings.step.y = Mathf.Max(settings.step.y, 0.5f);
            settings.step.z = Mathf.Max(settings.step.z, 0.5f);

            var center = owner.bounds.center;
            var extents = settings.chunkSize;
            step = settings.step;
            xSize = (int)(extents.x / step.x);
            ySize = (int)(extents.y / step.y);
            zSize = (int)(extents.z / step.z);

            //nodes = new GridNode[xSize, ySize, zSize];
            nodes = new FlattenedNode3DArray(FlattenedArrayUtils.New<Node>(xSize, ySize, zSize));

            Vector3 min = owner.bounds.min;
            Vector3 pos = min;
            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        nodes[x, y, z] = new Node(pos, GetIsoValue(pos));
                        pos.z += step.z;
                    }
                    pos.z = min.z;
                    pos.y += step.y;
                }
                pos.y = min.y;
                pos.x += step.x;
            }

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        FindNeighbours(x, y, z, settings.allowDiagonalNeighbours);
                    }
                }
            }
        }

        public void AssignNeighbours()
        {
            foreach (var node in nodes.items)
            {
                foreach (var identifier in node.neighbourIdentifiers)
                {
                    switch (identifier.chunkNeighbourType)
                    {
                        case ChunkNeighbourType.Same:
                            node.AddNeighbour(nodes[(int)identifier.key.x, (int)identifier.key.y, (int)identifier.key.z]);
                            break;
                        case ChunkNeighbourType.X:
                            var xNeighbour = owner.xNeighbour.grid.nodes[(int)identifier.key.x, (int)identifier.key.y, (int)identifier.key.z];
                            node.AddNeighbour(xNeighbour);
                            xNeighbour.AddNeighbour(node);
                            break;
                        case ChunkNeighbourType.Y:
                            var yNeighbour = owner.yNeighbour.grid.nodes[(int)identifier.key.x, (int)identifier.key.y, (int)identifier.key.z];
                            node.AddNeighbour(yNeighbour);
                            yNeighbour.AddNeighbour(node);
                            break;
                        case ChunkNeighbourType.Z:
                            var zNeighbour = owner.zNeighbour.grid.nodes[(int)identifier.key.x, (int)identifier.key.y, (int)identifier.key.z];
                            node.AddNeighbour(zNeighbour);
                            zNeighbour.AddNeighbour(node);
                            break;
                    }
                }
            }
        }

        public void DrawGizmos(Color color, float isoLevel)
        {
            foreach (var node in nodes.items)
            {
                node.DrawGizmos(color, isoLevel);
            }
        }

        /// <summary>
        /// Adds grid neighbour to node, direction ints should be in [-1, 1]
        /// </summary>
        /// <param name="node"></param>
        /// <param name="x">neighbour x idx</param>
        /// <param name="y">neighbour y idx</param>
        /// <param name="z">neighbour z idx</param>
        private void AddNeighbour(Node node, int x, int y, int z)
        {
            if (x < 0 || x > xSize - 1) return;

            if (y < 0 || y > ySize - 1) return;

            if (z < 0 || z > zSize - 1) return;

            node.AddNeighbourIdentifier(new Vector3(x, y, z));
        }

        /// <summary>
        /// Stores all direct neighbours within this grid for a node
        /// </summary>
        /// <param name="x">x idx of Node that should save its neighbours</param>
        /// <param name="y">y idx of Node that should save its neighbours</param>
        /// <param name="z">z idx of Node that should save its neighbours</param>
        /// <param name="includeDiagonal">Whether to include diagonal neighbours or straight only</param>
        public void FindNeighbours(int x, int y, int z, bool includeDiagonal = true)
        {
            if (x < 0 || y < 0 || z < 0) return;
            var node = nodes[x, y, z];
            AddNeighbour(node, x - 1, y, z);
            AddNeighbour(node, x + 1, y, z);
            AddNeighbour(node, x, y - 1, z);
            AddNeighbour(node, x, y - 1, z);
            AddNeighbour(node, x, y, z - 1);
            AddNeighbour(node, x, y, z + 1);
            if (includeDiagonal)
            {
                AddNeighbour(node, x - 1, y + 1, z - 1);
                AddNeighbour(node, x - 1, y + 1, z);
                AddNeighbour(node, x - 1, y + 1, z + 1);
                AddNeighbour(node, x, y + 1, z + 1);
                AddNeighbour(node, x + 1, y + 1, z + 1);
                AddNeighbour(node, x + 1, y + 1, z);
                AddNeighbour(node, x + 1, y + 1, z - 1);
                AddNeighbour(node, x, y + 1, z - 1);
                AddNeighbour(node, x + 1, y, z - 1);
                AddNeighbour(node, x + 1, y, z + 1);
                AddNeighbour(node, x - 1, y, z + 1);
                AddNeighbour(node, x - 1, y, z - 1);
                AddNeighbour(node, x - 1, y - 1, z - 1);
                AddNeighbour(node, x - 1, y - 1, z);
                AddNeighbour(node, x - 1, y - 1, z + 1);
                AddNeighbour(node, x, y - 1, z + 1);
                AddNeighbour(node, x + 1, y - 1, z + 1);
                AddNeighbour(node, x + 1, y - 1, z);
                AddNeighbour(node, x + 1, y - 1, z - 1);
                AddNeighbour(node, x, y - 1, z - 1);
            }
        }


        /// <summary>
        /// Only call this after each grid has figured out its and its nodes neighbours, ignoring diagonal chunk neighbours for now, shouldn't make a noticeable difference, can allow for diagonal movement between chunks for now
        /// </summary>
        public void FindCrossChunkNeighbours()
        {
            if (owner.xNeighbour != null)
            {
                for (int y = 0; y < ySize; y++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        nodes[xSize - 1, y, z].AddNeighbourIdentifier(new Vector3(0, y, z), ChunkNeighbourType.X);
                    }
                }
            }

            if (owner.yNeighbour != null)
            {
                for (int x = 0; x < xSize; x++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        nodes[x, ySize - 1, z].AddNeighbourIdentifier(new Vector3(x, 0, z), ChunkNeighbourType.Y);
                    }
                }
            }

            if (owner.zNeighbour != null)
            {
                for (int x = 0; x < xSize; x++)
                {
                    for (int y = 0; y < ySize; y++)
                    {
                        nodes[x, y, zSize - 1].AddNeighbourIdentifier(new Vector3(x, y, 0), ChunkNeighbourType.Z);
                    }
                }
            }
        }



        /// <summary>
        /// Calculates local position of position in this chunk, rounds it to node index
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Node GetClosestNode(Vector3 position)
        {
            Vector3 min = owner.bounds.min;


            int x = Mathf.RoundToInt((position.x - min.x) / step.x);
            x = Mathf.Clamp(x, 0, xSize - 1);

            int y = Mathf.RoundToInt((position.y - min.y) / step.y);
            y = Mathf.Clamp(y, 0, ySize - 1);

            int z = Mathf.RoundToInt((position.z - min.z) / step.z);
            z = Mathf.Clamp(z, 0, zSize - 1);

            return nodes[x, y, z];
        }

        /// <summary>
        /// Resets cost / heuristic values of all nodes, not calling this before finding another similar path might speed it up
        /// </summary>
        public void ResetNodes()
        {
            foreach (var node in nodes.items)
            {
                if (node.F != -1)
                {
                    node.cost = -1;
                    node.heuristic = -1;
                }
            }
        }
    }
}
