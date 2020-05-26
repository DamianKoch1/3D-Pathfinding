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

    public Transform end;

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

    public void Clear()
    {
        if (chunks == null) return;
        foreach (var chunk in GetComponentsInChildren<Chunk>())
        {
            DestroyImmediate(chunk.gameObject);
        }
        GetComponent<LineRenderer>().positionCount = 0;
        hasGrid = false;
        chunks = null;
    }


    private void OnValidate()
    {
        if (!autoGenerate) return;
        gridSettings.onValidate = OnValidate;
        GenerateChunks();
        GenerateGrid();
        MarchCubes();
    }

    public void FindPath()
    {
        var grid = chunks[0, 0, 0].grid;
        if (grid == null)
        {
            GenerateGrid();
        }

        var lr = GetComponent<LineRenderer>();
        var pathPoints = new List<Vector3>();
        var color = Random.ColorHSV();

        pathPoints.Add(start.position);

        pathPoints.AddRange(grid.FindPath(start.position, end.position, pathfindingSettings, gridSettings.isoLevel));

        lr.positionCount = pathPoints.Count;
        lr.SetPositions(pathPoints.ToArray());
    }


    private void OnDrawGizmos()
    {
        VisualizePathfinding();
    }

    private void VisualizePathfinding()
    {
        if (!visualizePathfinding) return;
        if (chunks == null) return;
        var grid = chunks[0, 0, 0].grid;
        if (grid == null) return;
        if (grid.openNodes == null) return;
        if (grid.closedNodes == null) return;
        var lowestF = grid.closedNodes.OrderBy(n => n.F).First().F;
        float highestF = 0;
        if (grid.openNodes.Count == 0)
        {
            highestF = grid.closedNodes.OrderBy(n => n.F).Last().F;
        }
        else
        {
            highestF = grid.openNodes.Last().F;
        }
        foreach (var node in grid.closedNodes)
        {
            Gizmos.color = Color.Lerp(pathfindingSettings.lowF, pathfindingSettings.highF, (node.F - lowestF) / (highestF - lowestF));
            Gizmos.DrawCube(node.pos, Vector3.one * 2);
        }
        foreach (var node in grid.openNodes)
        {
            Gizmos.color = Color.Lerp(pathfindingSettings.lowF, pathfindingSettings.highF, (node.F - lowestF) / (highestF - lowestF));
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
}
