using UnityEngine;

namespace Pathfinding
{
    [CreateAssetMenu]
    public class ObstacleGenerationSettings : ScriptableObject
    {
        public bool generate;

        public GameObject prefab;

        public int count;

        public float minScale = 0.1f;

        public float maxScale = 2f;

    }
}
