using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pathfinding.Serialization
{
    [MessagePackObject]
    public class GeneratorData
    {
        [Key(0)]
        public ChunkData[] chunkData;

        public GeneratorData(ChunkData[] _chunkData)
        {
            chunkData = _chunkData;
        }

        public GeneratorData(NodesGenerator source)
        {
            chunkData = new ChunkData[source.chunks.Length];
            for (int i = 0; i < chunkData.Length; i++)
            {
                chunkData[i] = source.chunks[i].Serialize();
            }
        }
    }

    [MessagePackObject]
    public class ChunkData
    {
        [Key(0)]
        public Node[,,] gridNodes;

        [Key(1)]
        public Dictionary<Vector3, Node> graphNodes;

        [Key(2)]
        public Vector3[] vertices;

        [Key(3)]
        public int[] triangles;


        public ChunkData(Node[,,] _gridNodes, Dictionary<Vector3, Node> _graphNodes, Vector3[] _vertices, int[] _triangles)
        {
            gridNodes = _gridNodes;
            graphNodes = _graphNodes;
            vertices = _vertices;
            triangles = _triangles;
        }

        public ChunkData(Chunk source)
        {
            if (source.grid != null)
            {
                gridNodes = source.grid.nodes;
            }
            else
            {
                var filter = source.GetComponent<MeshFilter>();
                vertices = filter.sharedMesh?.vertices;
                triangles = filter.sharedMesh?.triangles;
            }

            if (source.graph != null)
            {
                graphNodes = source.graph.nodes;
            }
        }
    }
}
