using System;
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

        public Mode mode;

        public LayerMask navmeshLayer;

        public LayerMask obstacleLayer;

        [Header("Overlap settings")]
        [Range(0, 10)]
        public float navMeshOffset;

        [Header("Noise settings")]
        [Range(0.001f, 0.04f)]
        public float scale;

        public int seed;

        public bool useRandomSeed;

        public Action onValidate;

        public void OnValidate()
        {
            onValidate?.Invoke();
        }
    }

    public enum Mode
    {
        Overlap,
        Noise
    }
}