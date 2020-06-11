using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity;
using MessagePack.Unity.Extension;
using Pathfinding.Serialization;
using SimplexNoise;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pathfinding
{
    /// <summary>
    /// Responsible for generating chunks / grid / navmesh and finding paths through them
    /// </summary>
    public class NodesGenerator : MonoBehaviour
    {
        public static int NAVMESH_LAYER;

        public static int OBSTACLE_LAYER;

        public GridGenerationSettings gridSettings;

        public PathfindingSettings pathfindingSettings;

        public PathfindingType pathfindingType;

        public Transform start;

        public Transform goal;

        public Vector3Int chunkCount;

        [SerializeField]
        private Chunk chunkPrefab;

        public FlattenedChunk3DArray chunks;

        [SerializeField]
        public bool visualizePathfinding;

        private SortedSet<Node> openNodes;
        private HashSet<Node> closedNodes;

        //used for profiling
        private void Start()
        {
            AssignNeighbours();
            FindGridPath(start.position, goal.position, pathfindingSettings);
            FindGraphPath(start.position, goal.position, pathfindingSettings);
        }

        public async void GenerateNodes()
        {
            NAVMESH_LAYER = LayerMask.GetMask("NavMesh");
            OBSTACLE_LAYER = LayerMask.GetMask("NavMeshObstacle");

            await GenerateGrid(0);
            switch (pathfindingType)
            {
                case PathfindingType.gridOnly:
                    break;
                case PathfindingType.navmeshOnly:
                    await MarchCubes(10);
                    GenerateGraph();
                    //ClearGrid();
                    break;
                case PathfindingType.both:
                    await MarchCubes(10);
                    GenerateGraph();
                    break;
            }
            print("Finished generating");
        }

        public async Task ClearGrid(int delay = 0)
        {
            foreach (var chunk in chunks.items)
            {
                chunk.grid = null;
                await Task.Delay(delay);
            }
        }

        /// <summary>
        /// Generates new chunks or stores existing ones again, checks if more are present than should be generated
        /// </summary>
        public void GenerateChunks()
        {
            if (chunks == null || chunks.Length != chunkCount.x * chunkCount.y * chunkCount.z)
            {
                chunks = new FlattenedChunk3DArray(FlattenedArrayUtils.New<Chunk>(chunkCount.x, chunkCount.y, chunkCount.z));
            }
            var outdatedChunks = new List<Chunk>();
            foreach (var chunk in GetComponentsInChildren<Chunk>())
            {
                bool outOfXRange = chunk.x >= chunkCount.x;
                bool outOfYRange = chunk.y >= chunkCount.y;
                bool outOfZRange = chunk.z >= chunkCount.z;

                if (outOfXRange || outOfYRange || outOfZRange)
                {
                    outdatedChunks.Add(chunk);
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
            while (outdatedChunks.Count > 0)
            {
                DestroyImmediate(outdatedChunks[0]);
                outdatedChunks.RemoveAt(0);
            }
        }

        /// <summary>
        /// Clears chunks that are no longer needed (when chunk count has been turned down)
        /// </summary>
        public void ClearOutdatedChunks()
        {
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
        public async Task GenerateGrid(int delay = 0)
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
                await Task.Delay(delay);
            }
            foreach (var chunk in chunks.items)
            {
                chunk.grid.FindCrossChunkNeighbours();
                await Task.Delay(delay);
            }
        }

        /// <summary>
        /// Lets all chunks generate their graph, then saves neighbours from adjacent graphs, only call this after navmesh has been built
        /// </summary>
        public async Task GenerateGraph(int delay = 0)
        {
            if (chunks == null) return;
            foreach (var chunk in chunks.items)
            {
                chunk.GenerateGraph();
                await Task.Delay(delay);
            }
            foreach (var chunk in chunks.items)
            {
                chunk.graph.FindCrossChunkNeighbours();
                await Task.Delay(delay);
            }
        }

        /// <summary>
        /// Destroys all chunks / grids / graphs
        /// </summary>
        public void Clear()
        {
            if (chunks == null) return;
            StopAllCoroutines();
            GetComponent<LineRenderer>().positionCount = 0;
            foreach (var chunk in chunks.items)
            {
                chunk.Clear();
            }
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
            while (Physics.Raycast(pos, goal - pos, out var hit, Vector3.Distance(pos, goal), NAVMESH_LAYER))
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
        public Stack<Vector3> FindGridPath(Vector3 start, Vector3 goal, PathfindingSettings settings)
        {
            var pathPoints = new Stack<Vector3>();
            if (chunks == null) return pathPoints;

            var lr = GetComponent<LineRenderer>();

            var tempNodes = new TempNodeDictionary();

            var gridStart = GetClosestGridNode(start);
            var gridGoal = GetClosestGridNode(goal);

            tempNodes.AddNode(gridStart, start);
            tempNodes.AddNode(gridGoal, goal);

            pathPoints = settings.RunAlgorithm(tempNodes[gridStart], tempNodes[gridGoal], gridSettings.isoLevel, out openNodes, out closedNodes);

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
            NAVMESH_LAYER = LayerMask.GetMask("NavMesh");
            OBSTACLE_LAYER = LayerMask.GetMask("NavMeshObstacle");
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
        public Stack<Vector3> FindGraphPath(Vector3 start, Vector3 goal, PathfindingSettings settings)
        {
            var pathPoints = new Stack<Vector3>();
            if (chunks == null) return pathPoints;

            var lr = GetComponent<LineRenderer>();



            openNodes = new SortedSet<Node>();
            closedNodes = new HashSet<Node>();


            var nodesClosestToHit = new List<Node>();

            var hits = GetNavMeshIntersections(start, goal);
            if (hits.Count > 1)
            {
                var tempNodes = new TempNodeDictionary();
                for (int i = 0; i < hits.Count - 1; i += 2)
                {
                    //add temp nodes at navmesh hits
                    nodesClosestToHit.Add(GetClosestGraphNode(hits[i].point));
                    nodesClosestToHit.Add(GetClosestGraphNode(hits[i + 1].point));
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

                }
                tempNodes.AddNode(nodesClosestToHit[0], start);
                tempNodes.AddNode(nodesClosestToHit[hits.Count - 1], goal);
                pathPoints = settings.RunAlgorithm(tempNodes[nodesClosestToHit[0]], tempNodes[nodesClosestToHit[hits.Count - 1]], -1, out openNodes, out closedNodes);
                tempNodes.Cleanup((n) => { openNodes.Remove(n); closedNodes.Remove(n); });
            }
            else
            {
                pathPoints.Push(goal);
                pathPoints.Push(start);
            }

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
            else if (closedNodes?.Count == 0) return;
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
        /// Lets all chunks march the cubes of their grids
        /// </summary>
        public async Task MarchCubes(int delay = 0)
        {
            if (chunks == null) return;
            foreach (var chunk in chunks.items)
            {
                chunk.MarchCubes();
                await Task.Delay(delay);
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

        public async Task Serialize()
        {
            var data = new GeneratorData(this);
            Stream stream = null;
            try
            {
                stream = new FileStream(Application.streamingAssetsPath + "/" + SceneManager.GetActiveScene().name + "_" + gameObject.name + ".nodes", FileMode.OpenOrCreate);

                await Task.Run(() => MessagePackSerializer.SerializeAsync(stream, data));
            }
            catch (IOException e)
            {
                print(e);
            }
            finally
            {
                stream?.Close();
            }
        }

        public async Task Deserialize()
        {
            Stream stream = null;
            GeneratorData data = null;
            try
            {
                stream = new FileStream(Application.streamingAssetsPath + "/" + SceneManager.GetActiveScene().name + "_" + gameObject.name + ".nodes", FileMode.OpenOrCreate);
                data = await MessagePackSerializer.DeserializeAsync<GeneratorData>(stream);
                await Task.Run(() => MessagePackSerializer.SerializeAsync(stream, data));
            }
            catch (IOException e)
            {
                print(e);
            }
            finally
            {
                stream?.Close();
                if (data != null)
                {
                    for (int i = 0; i < data.chunkData.Length; i++)
                    {
                        chunks[i].Deserialize(data.chunkData[i]);
                    }
                    MarchCubes(5);
                }
            }
        }
    }

    public enum PathfindingType
    {
        gridOnly,
        navmeshOnly,
        both
    }
}
