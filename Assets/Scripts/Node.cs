using MessagePack;
using Pathfinding.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Node used in grids / graphs / pathfinding, knows its world position, (grid index), neighbours, cost, heuristic, and previous node of path
    /// </summary>
    [MessagePackObject]
    public class Node : IComparable<Node>, IEquatable<Node>
    {
        [Key(0)]
        public Vector3 pos;

        [IgnoreMember]
        public Node parent;

        [IgnoreMember]
        public float heuristic = -1;

        [IgnoreMember]
        public float cost = -1;

        [Key(1)]
        public float isoValue;

        [IgnoreMember]
        public List<Node> neighbours;

        [Key(2)]
        public List<NodeIdentifier> neighbourIdentifiers;

        [IgnoreMember]
        public float costHeuristicBalance = 0.5f;

        [IgnoreMember]
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

        public Node(Vector3 _pos, float _isoValue, List<NodeIdentifier> _neighbourIdentifiers)
        {
            neighbours = new List<Node>(26);
            neighbourIdentifiers = _neighbourIdentifiers;
            pos = _pos;
            isoValue = _isoValue;
        }

        public Node(Vector3 _pos, float _isoValue)
        {
            neighbours = new List<Node>(26);
            neighbourIdentifiers = new List<NodeIdentifier>();
            pos = _pos;
            isoValue = _isoValue;
        }

        /// <summary>
        /// Adds a reference by which the correct node can be found, prevents circular serialization
        /// </summary>
        /// <param name="key">key used to find actual node later (grid index / graph position)</param>
        /// <param name="type">neighbour type (same chunk / x/y/z neighbour)</param>
        public void AddNeighbourIdentifier(Vector3 key, ChunkNeighbourType type = ChunkNeighbourType.Same)
        {
            var identifier = new NodeIdentifier(key, type);
            if (neighbourIdentifiers.Contains(identifier)) return;
            neighbourIdentifiers.Add(new NodeIdentifier(key, type));
        }

        public void AddNeighbour(Node neighbour)
        {
            if (neighbours == null) neighbours = new List<Node>(26);
            neighbours.Add(neighbour);
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

        public bool Equals(Node other)
        {
            return pos.Equals(other.pos);
        }
    }

    [MessagePackObject]
    public struct NodeIdentifier : IEquatable<NodeIdentifier>
    {
        [Key(0)]
        public Vector3 key;

        [Key(1)]
        public ChunkNeighbourType chunkNeighbourType;

        public NodeIdentifier(Vector3 _key, ChunkNeighbourType type)
        {
            key = _key;
            chunkNeighbourType = type;
        }

        //this is enough unless chunks have very few nodes which shouldn't happen
        public bool Equals(NodeIdentifier other)
        {
            return key.Equals(other.key);
        }
    }

    public enum ChunkNeighbourType
    {
        Same,
        X,
        Y,
        Z
    }
}
