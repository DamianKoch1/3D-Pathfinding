using SimplexNoise;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class NodesGenerator : MonoBehaviour
{
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

    public Chunk[,,] chunks;

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


    private void Start()
    {
        GenerateChunks();
        GenerateGrid();
        GenerateGraph();
        MarchCubes();
        FindGridPath();
    }

    private void OnValidate()
    {
        if (!autoGenerate) return;
        gridSettings.onValidate = OnValidate;
        GenerateChunks();
        GenerateGrid();
        GenerateGraph();
        MarchCubes();
    }


    public void GenerateChunks()
    {
        chunks = new Chunk[chunkCount.x, chunkCount.y, chunkCount.z];
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
                    chunks[x, y, z].Initialize(gridSettings, x, y, z);
                }
            }
        }
    }

    public void ClearOutOfRangeChunks()
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

    public void GenerateGrid()
    {
        if (chunks == null) return;
        foreach (var chunk in chunks)
        {
            chunk.GenerateGrid();
        }
        for (int x = 0; x < chunkCount.x; x++)
        {
            for (int y = 0; y < chunkCount.y; y++)
            {
                for (int z = 0; z < chunkCount.z; z++)
                {
                    if (x > 0)
                    {
                        chunks[x - 1, y, z].grid.xNeighbour = chunks[x, y, z].grid;
                    }

                    if (y > 0)
                    {
                        chunks[x, y - 1, z].grid.yNeighbour = chunks[x, y, z].grid;
                    }

                    if (z > 0)
                    {
                        chunks[x, y, z - 1].grid.zNeighbour = chunks[x, y, z].grid;
                    }
                }
            }
        }
        hasGrid = true;
    }

    public void GenerateGraph()
    {
        if (chunks == null) return;
        foreach (var chunk in chunks)
        {
            chunk.GenerateGraph();
        }
        hasGraph = true;
    }

    public void Clear()
    {
        if (chunks == null) return;
        foreach (var chunk in GetComponentsInChildren<Chunk>())
        {
            DestroyImmediate(chunk.gameObject);
        }
        GetComponent<LineRenderer>().positionCount = 0;
        hasGrid = false;
        hasGraph = false;
        chunks = null;
    }

    public Node GetClosestGridNode(Vector3 position)
    {
        Vector3 localPos = position - transform.position;
        int x = (int)(localPos.x / gridSettings.chunkSize.x);
        int y = (int)(localPos.y / gridSettings.chunkSize.y);
        int z = (int)(localPos.z / gridSettings.chunkSize.z);
        return chunks[x, y, z].grid.GetClosestNode(position);
    }

    public Node GetClosestGraphNode(Vector3 position)
    {
        Vector3 localPos = position - transform.position;
        int x = (int)(localPos.x / gridSettings.chunkSize.x);
        int y = (int)(localPos.y / gridSettings.chunkSize.y);
        int z = (int)(localPos.z / gridSettings.chunkSize.z);
        return chunks[x, y, z].graph.GetClosestNode(position);
    }

    public List<NavmeshHit> GetNavMeshIntersections(Vector3 start, Vector3 goal)
    {
        var hits = new List<NavmeshHit>();
        var dir = (goal - start).normalized;
        Physics.queriesHitBackfaces = true;
        var pos = start;

        //prevent infinite loops
        int maxRaycasts = 100;

        //RaycastAll only giving first hit since next rays start inside previous face
        while (Physics.Raycast(pos, goal - pos, out var hit, Vector3.Distance(pos, goal), gridSettings.navmeshLayer))
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
            Debug.DrawLine(hit.point, hit.point + Vector3.up, Color.white, 10);
        }

        return hits;
    }

    public void FindGridPath()
    {
        if (chunks == null) return;
        if (!hasGrid)
        {
            GenerateGrid();
        }

        var lr = GetComponent<LineRenderer>();
        var pathPoints = new List<Vector3>();

        pathPoints.Add(start.position);

        var startNode = GetClosestGridNode(start.position);
        var goalNode = GetClosestGridNode(goal.position);

        pathPoints.AddRange(AStar.FindPath(startNode, goalNode, pathfindingSettings, gridSettings.isoLevel, out openNodes, out closedNodes));

        pathPoints.Add(goal.position);

        lr.positionCount = pathPoints.Count;
        lr.SetPositions(pathPoints.ToArray());
    }

    public void FindGraphPath()
    {
        if (chunks == null) return;
        if (!hasGraph)
        {
            GenerateGraph();
        }

        var lr = GetComponent<LineRenderer>();
        var pathPoints = new List<Vector3>();

        pathPoints.Add(start.position);

        var hits = GetNavMeshIntersections(start.position, goal.position);
        if (hits.Count > 0)
        {
            pathPoints.Add(hits[0].point);
            if (hits.Count > 1)
            {
                for (int i = 0; i < hits.Count - 1; i += 2)
                {
                    var startNode = GetClosestGraphNode(hits[i].point);
                    var goalNode = GetClosestGraphNode(hits[i + 1].point);

                    pathPoints.AddRange(AStar.FindPath(startNode, goalNode, pathfindingSettings, -1, out openNodes, out closedNodes));


                    if (i + 2 < hits.Count)
                    {
                        pathPoints.Add(hits[i + 2].point);
                    }
                }
                pathPoints.Add(hits[hits.Count - 1].point);
            }
        }
        pathPoints.Add(goal.position);

        lr.positionCount = pathPoints.Count;
        lr.SetPositions(pathPoints.ToArray());
    }

    private void VisualizePathfinding()
    {
        if (!visualizePathfinding) return;
        if (openNodes == null) return;
        if (closedNodes == null) return;
        var lowestF = closedNodes.OrderBy(n => n.F).First().F;
        float highestF = 0;
        if (openNodes.Count == 0)
        {
            highestF = closedNodes.OrderBy(n => n.F).Last().F;
        }
        else
        {
            highestF = openNodes.Last().F;
        }
        foreach (var node in closedNodes)
        {
            Gizmos.color = Color.Lerp(Color.green, Color.red, (node.F - lowestF) / (highestF - lowestF));
            Gizmos.DrawCube(node.pos, Vector3.one * 2);
        }
        foreach (var node in openNodes)
        {
            Gizmos.color = Color.Lerp(Color.green, Color.red, (node.F - lowestF) / (highestF - lowestF));
            Gizmos.DrawCube(node.pos, Vector3.one);
        }
    }

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
                    Random.Range(-gridSettings.chunkSize.x / 2, gridSettings.chunkSize.x / 2),
                    Random.Range(-gridSettings.chunkSize.y / 2, gridSettings.chunkSize.y / 2),
                    Random.Range(-gridSettings.chunkSize.z / 2, gridSettings.chunkSize.z / 2)
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

    public void ExpandMeshes()
    {
        if (chunks == null) return;
        foreach (var chunk in chunks)
        {
            chunk.ExpandMeshes();
        }
    }

    public void MarchCubes()
    {
        if (chunks == null) return;
        foreach (var chunk in chunks)
        {
            chunk.MarchCubes();
        }
    }

    private void OnDrawGizmos()
    {
        VisualizePathfinding();
    }
}
