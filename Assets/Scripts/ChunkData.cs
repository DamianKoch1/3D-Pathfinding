using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding.Serialization
{
    [Serializable]
    public class ChunkData
    {
        public int gridWidth;
        public int gridHeight;
        public int gridDepth;

        public List<Node> grid;

        public List<Vector3> graphKeys;
        public List<Node> graphNodes;
    }
}
