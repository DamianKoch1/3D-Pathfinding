using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding.Serialization
{
    [CreateAssetMenu, PreferBinarySerialization]
    public class GeneratorData : ScriptableObject
    {
        public ChunkData[] chunkData;
    }


    [Serializable]
    public class ChunkData
    {
        public FlattenedNode3DArray gridNodes;

        public List<Vector3> graphKeys;
        public List<Node> graphNodes;

        public ChunkData(Chunk source)
        {
            if (source.grid != null)
            {
                gridNodes = source.grid.nodes;
            }

            graphKeys = new List<Vector3>();
            graphNodes = new List<Node>();
            if (source.graph != null)
            {
                source.graph.nodes.Serialize(ref graphKeys, ref graphNodes);
            }
        }
    }
}
