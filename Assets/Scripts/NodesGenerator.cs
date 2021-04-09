using MessagePack;
using MessagePack.Resolvers;
using Pathfinding.Containers;
using Pathfinding.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
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

        public static float CostHeuristicBalance;

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

        private BucketList<Node> openNodes;
        private HashSet<Node> closedNodes;

        private int gridNodeCount => ((int)(gridSettings.chunkSize.x / gridSettings.step.x) + (int)(gridSettings.chunkSize.y / gridSettings.step.y) + (int)(gridSettings.chunkSize.z / gridSettings.step.z)) * chunkCount.x * chunkCount.y * chunkCount.z;

        private void Start()
        {
            Deserialize();
        }


        [HideInInspector]
        public bool generating;

        /// <summary>
        /// Builds grid / navmesh / graph depending on pathfinding type, also saves neighbours
        /// </summary>
        /// <returns></returns>
        public async Task GenerateNodes()
        {
            NAVMESH_LAYER = LayerMask.GetMask("NavMesh");
            OBSTACLE_LAYER = LayerMask.GetMask("NavMeshObstacle");
            generating = true;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            switch (pathfindingType)
            {
                case PathfindingType.gridOnly:
                    await GenerateGrid();
                    break;
                case PathfindingType.navmeshOnly:
                    await GenerateGrid(true);
                    await MarchCubes();
                    await GenerateGraph();
                    ClearGrid();
                    break;
                case PathfindingType.both:
                    await GenerateGrid();
                    await MarchCubes();
                    await GenerateGraph();
                    break;
            }
            generating = false;
            AssignNeighbours();
            print("Finished generating, " + sw.Elapsed.TotalSeconds + "s");
            sw.Stop();
        }

        /// <summary>
        /// Resets grid of all chunks
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        private async Task ClearGrid(int delay = 0)
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
        private void ClearOutdatedChunks()
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
        private async Task GenerateGrid(bool blockedOnly = false, int delay = 0)
        {
            if (chunks == null) return;

            foreach (var chunk in chunks.items)
            {
                chunk.GenerateGrid(blockedOnly);
            }
            if (blockedOnly) return;
            foreach (var chunk in chunks.items)
            {
                chunk.grid.FindCrossChunkNeighbours();
                await Task.Delay(delay);
            }
        }

        /// <summary>
        /// Lets all chunks generate their graph, then saves neighbours from adjacent graphs, only call this after navmesh has been built
        /// </summary>
        private async Task GenerateGraph(int delay = 0)
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
            generating = false;
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
        private Chunk GetChunk(Vector3 position)
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
            return GetChunk(position).grid?.GetClosestNode(position);
        }

        /// <summary>
        /// Returns the closest graph node to position of the chunk position is in
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Node GetClosestGraphNode(Vector3 position)
        {
            return GetChunk(position).graph?.GetClosestNode(position);
        }

        /// <summary>
        /// Calculates all intersections with NavMesh from start to goal (including backfaces) to plan paths between
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        public List<Vector3> GetNavMeshIntersections(Vector3 start, Vector3 goal)
        {
            var hits = new List<Vector3>();
            var dir = (goal - start).normalized;
            Physics.queriesHitBackfaces = true;
            var pos = start;

            //prevent infinite loops
            int maxRaycasts = 100;

            //RaycastAll only giving first hit since next rays start inside previous face
            while (Physics.Raycast(pos, goal - pos, out var hit, Vector3.Distance(pos, goal), NAVMESH_LAYER))
            {
                maxRaycasts--;
                hits.Add(hit.point);
                //adding small offset from last point to prevent raycasting on the same face
                pos = hit.point + dir * 0.0001f;
                if (Vector3.Distance(pos, start) < 0.5f) break;
                if (maxRaycasts <= 0) break;
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
            if (pathfindingType == PathfindingType.navmeshOnly) return pathPoints;
            if (chunks == null) return pathPoints;


            var tempNodes = new TempNodeDictionary();

            var gridStart = GetClosestGridNode(start);
            var gridGoal = GetClosestGridNode(goal);

            tempNodes.AddNode(gridStart, start);
            tempNodes.AddNode(gridGoal, goal);


            pathPoints = settings.RunAlgorithm(tempNodes[gridStart], tempNodes[gridGoal], gridSettings.isoLevel, out openNodes, out closedNodes, 50000, gridNodeCount);

            tempNodes.Cleanup();

            if (settings.benchmark)
            {
                var lr = GetComponent<LineRenderer>();
                lr.positionCount = pathPoints.Count;
                lr.SetPositions(pathPoints.ToArray());
            }
            return pathPoints;
        }

        /// <summary>
        /// Finds the actual nodes of each nodes neighbour references and stores them
        /// </summary>
        private void AssignNeighbours()
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
            if (pathfindingType == PathfindingType.gridOnly) return pathPoints;
            if (chunks == null) return pathPoints;


            var nodesClosestToHit = new List<Node>();

            var hits = GetNavMeshIntersections(start, goal);
            if (hits.Count > 1)
            {
                var tempNodes = new TempNodeDictionary();
                for (int i = 0; i < hits.Count - 1; i += 2)
                {
                    //add temp nodes at navmesh hits
                    nodesClosestToHit.Add(GetClosestGraphNode(hits[i]));
                    nodesClosestToHit.Add(GetClosestGraphNode(hits[i + 1]));
                    if (i > 0)
                    {
                        tempNodes.AddNode(nodesClosestToHit[i], hits[i]);

                        //link navmesh exiting hits with entering hits
                        if (i % 2 == 0)
                        {
                            tempNodes[nodesClosestToHit[i]].neighbours.Add(tempNodes[nodesClosestToHit[i - 1]]);
                            tempNodes[nodesClosestToHit[i - 1]].neighbours.Add(tempNodes[nodesClosestToHit[i]]);
                        }
                    }
                    if (i < hits.Count - 2)
                    {
                        tempNodes.AddNode(nodesClosestToHit[i + 1], hits[i + 1]);
                    }

                }
                tempNodes.AddNode(nodesClosestToHit[0], start);
                tempNodes.AddNode(nodesClosestToHit[hits.Count - 1], goal);

                pathPoints = settings.RunAlgorithm(tempNodes[nodesClosestToHit[0]], tempNodes[nodesClosestToHit[hits.Count - 1]], -1, out openNodes, out closedNodes, 50000, gridNodeCount / 100);
                tempNodes.Cleanup();
            }
            else
            {
                pathPoints.Push(goal);
                pathPoints.Push(start);
            }

            if (settings.benchmark)
            {
                var lr = GetComponent<LineRenderer>();
                lr.positionCount = pathPoints.Count;
                lr.SetPositions(pathPoints.ToArray());
            }
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
        private async Task MarchCubes(int delay = 0)
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

        [HideInInspector]
        public bool serializing;

        public async Task Serialize()
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            var data = new GeneratorData(this);
            FileStream fs = null;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            serializing = true;
            try
            {
                fs = new FileStream(Application.streamingAssetsPath + "/" + SceneManager.GetActiveScene().name + "_" + gameObject.name + ".nodes", FileMode.Create, FileAccess.Write, FileShare.None, 128000);
                await Task.Run(() => MessagePackSerializer.SerializeAsync(fs, data, options));
                //MessagePackSerializer.Serialize(fs, data, options);
            }
            catch (System.Exception e)
            {
                print(e);
            }
            finally
            {
                fs?.Close();
                serializing = false;
                print("Saved, " + sw.Elapsed.TotalSeconds + "s");
                sw.Stop();
            }
        }

        public MessagePackCompression compression;
        public MessagePackSerializerOptions options => MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(StandardResolver.Instance, GeneratedResolver.Instance)).WithCompression(compression);

        [HideInInspector]
        public bool deserializing;

        public System.Action OnInitialize;

        public async Task Deserialize()
        {
            FileStream fs = null;
            GeneratorData data = null;
            deserializing = true;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    UnityWebRequest www = new UnityWebRequest(Application.streamingAssetsPath + "/" + SceneManager.GetActiveScene().name + "_" + gameObject.name + ".nodes")
                    {
                        downloadHandler = new DownloadHandlerBuffer()
                    };
                    www.SendWebRequest();
                    while (!www.isDone)
                    {
                        await Task.Delay(10);
                    }
                    data = MessagePackSerializer.Deserialize<GeneratorData>(www.downloadHandler.data, options);
                }
                else
                {
                    fs = new FileStream(Application.streamingAssetsPath + "/" + SceneManager.GetActiveScene().name + "_" + gameObject.name + ".nodes", FileMode.Open, FileAccess.Read, FileShare.None, 128000);
                    data = await Task.Run(() => MessagePackSerializer.DeserializeAsync<GeneratorData>(fs, options)).Result;
                    //data = MessagePackSerializer.Deserialize<GeneratorData>(fs, options);
                }
            }
            catch
            {
                //print(e);
            }
            finally
            {
                fs?.Close();
                deserializing = false;
                if (data != null)
                {
                    print("Loaded, " + sw.Elapsed.TotalSeconds + "s");
                    sw.Restart();
                    for (int i = 0; i < data.chunkData.Length; i++)
                    {
                        chunks[i].Deserialize(data.chunkData[i]);
                    }
                    if (pathfindingType != PathfindingType.navmeshOnly)
                    {
                        MarchCubes();
                    }
                    AssignNeighbours();
                    print("Init, " + sw.Elapsed.TotalSeconds);
                    sw.Stop();
                    OnInitialize?.Invoke();
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
