using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Generates navmesh obstacles for testing a generator
    /// </summary>
    public class ObstacleGenerator : MonoBehaviour
    {
        public ObstacleGenerationSettings obstacleSettings;

        public NodesGenerator nodesGenerator;

        [SerializeField]
        private Transform obstacles;

        /// <summary>
        /// Randomizes obstacle scale / positions within chunk boundaries, instantiates or removes obstacles if necessary
        /// </summary>
        public void RandomizeObstacles()
        {
            if (!obstacles) return;
            if (!obstacleSettings) return;

            if (!obstacleSettings.prefab) return;

            var childCount = obstacles.childCount;
            if (childCount < obstacleSettings.count)
            {
                for (int i = childCount; i < obstacleSettings.count; i++)
                {
                    Instantiate(obstacleSettings.prefab, obstacles);
                }
            }
            else
            {
                for (int i = 0; i < childCount - obstacleSettings.count; i++)
                {
                    DestroyImmediate(obstacles.GetChild(0).gameObject);
                }
            }
            var max = Vector3.Scale(nodesGenerator.gridSettings.chunkSize, nodesGenerator.chunkCount);
            foreach (Transform child in obstacles)
            {
                child.position = obstacles.position + new Vector3
                    (
                        Random.Range(0, max.x),
                        Random.Range(0, max.y),
                        Random.Range(0, max.z)
                    );
                child.localScale = Vector3.one * Random.Range(obstacleSettings.minScale, obstacleSettings.maxScale);
            }
            Physics.SyncTransforms();
        }

        public void Clear()
        {
            if (!obstacles) return;
            foreach (Transform child in obstacles)
            {
                while (obstacles.childCount > 0)
                {
                    DestroyImmediate(obstacles.GetChild(0).gameObject);
                }
            }
        }

    }
}
