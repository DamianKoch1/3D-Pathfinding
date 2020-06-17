using UnityEngine;

namespace Pathfinding
{
    [CreateAssetMenu]
    public class GridGenerationSettings : ScriptableObject
    {
        public bool drawNodes;

        public bool drawExtents;

        public Color nodeColor;

        public Vector3 chunkSize;

        [Range(0, 1), Tooltip("Node is walkable if it has iso value above this")]
        public float isoLevel;

        public bool allowDiagonalNeighbours;

        public Vector3 step;

        [Range(0, 50)]
        public float navMeshOffset;

    }
}