using Pathfinding.Serialization;
using SimplexNoise;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Responsible for generating chunks / grid / navmesh and finding paths through them
    /// </summary>
    public class NodesGenerator : MonoBehaviour
    {
        public const string NAVMESH_LAYER = "NavMesh";

        public const string OBSTACLE_LAYER = "NavMeshObstacle";


        public GridGenerationSettings gridSettings;

        public ObstacleGenerationSettings obstacleSettings;

        public PathfindingSettings pathfindingSettings;

        public Transform start;

        public Transform goal;

        [SerializeField]
        private Transform obstacles;

        [SerializeField]
        private Vector3Int chunkCount;

        [SerializeField]
        private Chunk chunkPrefab;

        public FlattenedChunk3DArray chunks;

        [SerializeField]
        private bool autoGenerate;

        [SerializeField]
        public bool visualizePathfinding;

        //Can't destroy chunks from OnValidate in edit mode
        [HideInInspector]
        public bool hasOutOfRangeChunks;


        [HideInInspector]
        public bool hasGrid;

        [HideInInspector]
        public bool hasGraph;

        private SortedSet<Node> openNodes;
        private HashSet<Node> closedNodes;

        //used for profiling
        private void Start()
        {
            //GenerateChunks();
            //GenerateGrid();
            //MarchCubes();
            //GenerateGraph();
            AssignNeighbours();
            FindGridPath(start.position, goal.position, pathfindingSettings);
            FindGraphPath(start.position, goal.position, pathfindingSettings);
        }

        //better turn this off with too much chunks / low grid step
        private void OnValidate()
        {
            if (!autoGenerate) return;
            gridSettings.onValidate = OnValidate;
            GenerateChunks();
            GenerateGrid();
            GenerateGraph();
            MarchCubes();
        }

        /// <summary>
        /// Generates new chunks or stores existing ones again, checks if more are present than should be generated
        /// </summary>
        public void GenerateChunks()
        {
            chunks = new FlattenedChunk3DArray(FlattenedArrayUtils.New<Chunk>(chunkCount.x, chunkCount.y, chunkCount.z));
            foreach (var chunk in GetComponentsInChildren<Chunk>())
            {
                bool outOfXRange = chunk.x >= chunkCount.x;
                bool outOfYRange = chunk.y >= chunkCount.y;
                bool outOfZRange = chunk.z >= chunkCount.z;

                if (outOfXRange || outOfYRange || outOfZRange)
                {
                    hasOutOfRangeChunks = true;
                    continue;
                }
                chunks[chunk.x, chunk.y, chunk.z] = chunk;
            }

            for (int x = 0; x < chunkCount.x; x++)
            {
                for (int y = 0; y < chunkCount.y; y++)
                {
                    for (int z = 0; z < chunkCount.z; z++)
                    {
                        if (chunks[x, y, z] == null)
                        {
                            var chunk = Instantiate(chunkPrefab.gameObject, transform).GetComponent<Chunk>();
                            chunks[x, y, z] = chunk;
                        }
                        if (x > 0)
                        {
                            chunks[x - 1, y, z].xNeighbour = chunks[x, y, z];
                        }

                        if (y > 0)
                        {
                            chunks[x, y - 1, z].yNeighbour = chunks[x, y, z];
                        }

                        if (z > 0)
                        {
                            chunks[x, y, z - 1].zNeighbour = chunks[x, y, z];
                        }
                        chunks[x, y, z].Initialize(gridSettings, x, y, z);
                    }
                }
            }
        }

        /// <summary>
        /// Clears chunks that are no longer needed (when chunk count has been turned down)
        /// </summary>
        public void ClearOutdatedChunks()
        {
            hasOutOfRangeChunks = false;
            foreach (var chunk in GetComponentsInChildren<Chunk>())
            {
                bool outOfXRange = chunk.x >= chunkCount.x;
                bool outOfYRange = chunk.y >= chunkCount.y;
                bool outOfZRange = chunk.z >= chunkCount.z;

                if (outOfXRange || outOfYRange || outOfZRange)
                {
                    DestroyImmediate(chunk.gameObject);
                }
            }
        }

        /// <summary>
        /// Lets all chunk generate their grid, then saves neighbours from adjacent chunks
        /// </summary>
        public void GenerateGrid()
        {
            if (chunks == null) return;
            if (gridSettings.mode == GenerationMode.Noise)
            {
                if (gridSettings.useRandomSeed)
                {
                    gridSettings.seed = Random.Range(0, 10000);
                    Noise.Seed = gridSettings.seed;
                }
            }
            foreach (var chunk in chunks.items)
            {
                chunk.GenerateGrid();
            }
            foreach (var chunk in chunks.items)
            {
                chunk.grid.FindCrossChunkNeighbours();
            }
            hasGrid = true;
        }

        /// <summary>
        /// Lets all chunks generate their graph, then saves neighbours from adjacent graphs, only call this after navmesh has been built
        /// </summary>
        public void GenerateGraph()
        {
            if (chunks == null) return;
            foreach (var chunk in chunks.items)
            {
                chunk.GenerateGraph();
            }
            foreach (var chunk in chunks.items)
            {
                chunk.graph.FindCrossChunkNeighbours();
            }
            hasGraph = true;
        }

        /// <summary>
        /// Destroys all chunks / grids / graphs
        /// </summary>
        public void Clear()
        {
            if (chunks == null) return;
            GetComponent<LineRenderer>().positionCount = 0;
            foreach (var chunk in chunks.items)
            {
                chunk.Clear();
            }
            hasGrid = false;
            hasGraph = false;
            openNodes = null;
            closedNodes = null;
        }

        /// <summary>
        /// Returns the chunk position is in, make sure position is within a chunk
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Chunk GetChunk(Vector3 position)
        {
            Vector3 localPos = position - transform.position;
            int x = (int)(localPos.x / gridSettings.chunkSize.x);
            int y = (int)(localPos.y / gridSettings.chunkSize.y);
            int z = (int)(localPos.z / gridSettings.chunkSize.z);
            return chunks[x, y, z];
        }

        /// <summary>
        /// Returns the closest grid node to position of the chunk position is in
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Node GetClosestGridNode(Vector3 position)
        {
            return GetChunk(position).grid.GetClosestNode(position);
        }

        /// <summary>
        /// Returns the closest graph node to position of the chunk position is in
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Node GetClosestGraphNode(Vector3 position)
        {
            return GetChunk(position).graph.GetClosestNode(position);
        }

        /// <summary>
        /// Calculates all intersections with NavMesh from start to goal (including backfaces) to plan paths between
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        public List<NavmeshHit> GetNavMeshIntersections(Vector3 start, Vector3 goal)
        {
            var hits = new List<NavmeshHit>();
            var dir = (goal - start).normalized;
            Physics.queriesHitBackfaces = true;
            var pos = start;

            //prevent infinite loops
            int maxRaycasts = 100;

            //RaycastAll only giving first hit since next rays start inside previous face
            while (Physics.Raycast(pos, goal - pos, out var hit, Vector3.Distance(pos, goal), LayerMask.GetMask(NAVMESH_LAYER)))
            {
                maxRaycasts--;
                hits.Add(new NavmeshHit(hit));
                //adding small offset from last point to prevent raycasting on the same face
                pos = hit.point + dir * 0.0001f;
                if (Vector3.Distance(pos, start) < 0.5f) break;
                if (maxRaycasts <= 0) break;
            }

            foreach (var hit in hits)
            {
                Debug.DrawLine(hit.point, hit.point + hit.normal, Color.white, 10);
            }

            return hits;
        }

        /// <summary>
        /// Finds path from start to goal using grid
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public List<Vector3> FindGridPath(Vector3 start, Vector3 goal, PathfindingSettings settings)
        {
            var pathPoints = new List<Vector3>();
            if (chunks == null) return pathPoints;
            if (!hasGrid)
            {
                GenerateGrid();
            }

            var lr = GetComponent<LineRenderer>();

            var tempNodes = new TempNodeDictionary();

            var gridStart = GetClosestGridNode(start);
            var gridGoal = GetClosestGridNode(goal);

            tempNodes.AddNode(gridStart, start);
            tempNodes.AddNode(gridGoal, goal);

            pathPoints.AddRange(settings.RunAlgorithm(tempNodes[gridStart], tempNodes[gridGoal], gridSettings.isoLevel, out openNodes, out closedNodes));

            tempNodes.Cleanup((n) => { openNodes.Remove(n); closedNodes.Remove(n); });

            lr.positionCount = pathPoints.Count;
            lr.SetPositions(pathPoints.ToArray());
            return pathPoints;
        }

        /// <summary>
        /// The only thing you need to call after hot reloading, finds the actual nodes of each nodes neighbour references and stores them
        /// </summary>
        public void AssignNeighbours()
        {
            foreach (var chunk in chunks.items)
            {
                chunk.AssignNeighbours();
            }
        }

        /// <summary>
        /// Finds path from start to goal using NavMesh
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public List<Vector3> FindGraphPath(Vector3 start, Vector3 goal, PathfindingSettings settings)
        {
            var pathPoints = new List<Vector3>();
            if (chunks == null) return pathPoints;
            if (!hasGraph)
            {
                GenerateGraph();
            }

            var lr = GetComponent<LineRenderer>();

            var tempNodes = new TempNodeDictionary();


            openNodes = new SortedSet<Node>();
            closedNodes = new HashSet<Node>();


            var nodesClosestToHit = new Dictionary<int, Node>();

            var hits = GetNavMeshIntersections(start, goal);
            if (hits.Count > 1)
            {
                for (int i = 0; i < hits.Count - 1; i += 2)
                {
                    nodesClosestToHit.Add(i, GetClosestGraphNode(hits[i].point));
                    nodesClosestToHit.Add(i + 1, GetClosestGraphNode(hits[i + 1].point));
                    if (i > 0)
                    {
                        tempNodes.AddNode(nodesClosestToHit[i], hits[i].point);
                        //link navmesh exiting hits with entering hits
                        if (i % 2 == 0)
                        {
                            tempNodes[nodesClosestToHit[i]].neighbours.Add(tempNodes[nodesClosestToHit[i - 1]]);
                            tempNodes[nodesClosestToHit[i - 1]].neighbours.Add(tempNodes[nodesClosestToHit[i]]);
                        }
                    }
                    if (i < hits.Count - 2)
                    {
                        tempNodes.AddNode(nodesClosestToHit[i + 1], hits[i + 1].point);
                    }


                    if (i + 2 < hits.Count)
                    {
                        pathPoints.Add(hits[i + 2].point);
                    }
                }
                tempNodes.AddNode(nodesClosestToHit[0], start);
                tempNodes.AddNode(nodesClosestToHit[hits.Count - 1], goal);
            }
            else return pathPoints;
            pathPoints.AddRange(settings.RunAlgorithm(tempNodes[nodesClosestToHit[0]], tempNodes[nodesClosestToHit[hits.Count - 1]], -1, out openNodes, out closedNodes));

            //??
            pathPoints.RemoveAt(0);

            tempNodes.Cleanup((n) => { openNodes.Remove(n); closedNodes.Remove(n); });
            lr.positionCount = pathPoints.Count;
            lr.SetPositions(pathPoints.ToArray());
            return pathPoints;
        }

        /// <summary>
        /// Draws gizmos for open / closed nodes colored by their F values, be careful with less greedy algorithms, can cause heavy lag
        /// </summary>
        private void VisualizePathfinding()
        {
            if (!visualizePathfinding) return;
            if (openNodes == null && closedNodes == null) return;
            float lowestF = 0;
            float highestF = 0;
            if (openNodes?.Count > 0)
            {
                highestF = openNodes.Last().F;
            }
            else
            {
                highestF = closedNodes.OrderBy(n => n.F).Last().F;
            }
            if (closedNodes?.Count > 0)
            {
                lowestF = closedNodes.OrderBy(n => n.F).First().F;
            }
            if (openNodes?.Count > 0)
            {
                foreach (var node in openNodes)
                {
                    Gizmos.color = Color.Lerp(Color.green, Color.red, (node.F - lowestF) / (highestF - lowestF));
                    Gizmos.DrawCube(node.pos, Vector3.one);
                }
            }
            if (closedNodes?.Count > 0)
            {
                foreach (var node in closedNodes)
                {
                    Gizmos.color = Color.Lerp(Color.green, Color.red, (node.F - lowestF) / (highestF - lowestF));
                    Gizmos.DrawCube(node.pos, Vector3.one * 2);
                }
            }
        }

        /// <summary>
        /// Randomizes obstacle scale / positions within chunk boundaries, instantiates or removes obstacles if necessary
        /// </summary>
        public void RandomizeObstacles()
        {
            if (!obstacles) return;
            if (!obstacleSettings || !obstacleSettings.generate)
            {
                while (obstacles.childCount > 0)
                {
                    DestroyImmediate(obstacles.GetChild(0).gameObject);
                }
                return;
            }

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

            foreach (Transform child in obstacles)
            {
                child.position = obstacles.position + new Vector3
                    (
                        Random.Range(0, gridSettings.chunkSize.x * chunkCount.x),
                        Random.Range(0, gridSettings.chunkSize.y * chunkCount.y),
                        Random.Range(0, gridSettings.chunkSize.z * chunkCount.z)
                    );
                child.rotation = Random.rotation;
                child.localScale = new Vector3
                (
                    Random.Range(obstacleSettings.minScale, obstacleSettings.maxScale),
                    Random.Range(obstacleSettings.minScale, obstacleSettings.maxScale),
                    Random.Range(obstacleSettings.minScale, obstacleSettings.maxScale)
                );
            }
            Physics.SyncTransforms();
        }

        /// <summary>
        /// Lets all chunks march the cubes of their grids
        /// </summary>
        public void MarchCubes()
        {
            if (chunks == null) return;
            foreach (var chunk in chunks.items)
            {
                chunk.MarchCubes();
            }
        }

        public void ToggleNavMesh()
        {
            var material = GetComponent<MeshRenderer>().sharedMaterial;
            var cutOff = material.GetFloat("_Cutoff");
            material.SetFloat("_Cutoff", cutOff == 0 ? 1 : 0);
        }

        private void OnDrawGizmos()
        {
            VisualizePathfinding();
        }
    }
}
