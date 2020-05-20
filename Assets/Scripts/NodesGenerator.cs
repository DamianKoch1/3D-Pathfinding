using SimplexNoise;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NodesGenerator : MonoBehaviour
{
    [SerializeField]
    private GridGenerationSettings gridSettings;

    [SerializeField]
    private ObstacleGenerationSettings obstacleSettings;

    [SerializeField]
    private PathfindingSettings pathfindingSettings;

    private Grid grid;

    [SerializeField]
    private Transform start;

    [SerializeField]
    private Transform end;

    [SerializeField]
    private Transform obstacles;



    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        System.Func<Vector3, float> GetIsoValue = null;
        var collider = GetComponent<MeshCollider>();
        collider.enabled = false;
        switch (gridSettings.mode)
        {
            case Mode.Overlap:
                GetIsoValue = (Vector3 pos) => Physics.OverlapSphere(pos, gridSettings.navMeshOffset).Length == 0 ? 1 : 0;
                break;
            case Mode.Noise:
                if (gridSettings.useRandomSeed)
                {
                    gridSettings.seed = Random.Range(0, 10000);
                    Noise.Seed = gridSettings.seed;
                }
                GetIsoValue = (Vector3 pos) => Noise.CalcPixel3D(pos.x, pos.y, pos.z, gridSettings.scale) / 255f;
                break;
        }

        grid = new Grid(transform.position, gridSettings, GetIsoValue);
        collider.enabled = true;
    }

    public void Clear()
    {
        grid = null;
        GetComponent<MeshFilter>().sharedMesh = null;
        GetComponent<MeshCollider>().sharedMesh = null;
    }


    private void OnValidate()
    {
        if (gridSettings.onValidate != OnValidate)
        {
            gridSettings.onValidate = OnValidate;
        }
        GenerateGrid();
        MarchCubes();
    }

    public void FindPath(float drawDuration = 0)
    {
        if (grid == null)
        {
            GenerateGrid();
        }
        var color = Random.ColorHSV();
        var path = grid.FindPath(start.position, end.position, pathfindingSettings, gridSettings.isoLevel);
        var pathPos = path.Pop();
        Debug.DrawLine(start.position, pathPos, color, drawDuration);
        while (path.Count > 0)
        {
            Debug.DrawLine(pathPos, path.Peek(), color, drawDuration);
            pathPos = path.Pop();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, gridSettings.size);
        if (grid != null)
        {
            if (gridSettings.drawNodes)
            {
                grid.DrawGizmos(gridSettings.color, gridSettings.isoLevel);
            }
        }
    }


    [ContextMenu("RandomizeObstacles")]
    public void RandomizeObstacles()
    {
        if (!obstacleSettings.generate)
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
                    Random.Range(-gridSettings.size.x / 2, gridSettings.size.x / 2),
                    Random.Range(-gridSettings.size.y / 2, gridSettings.size.y / 2),
                    Random.Range(-gridSettings.size.z / 2, gridSettings.size.z / 2)
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
        var combine = new List<CombineInstance>();
        var filter = GetComponent<MeshFilter>();
        var collider = GetComponent<MeshCollider>();
        collider.enabled = false;
        foreach (var obj in Physics.OverlapBox(transform.position, gridSettings.size / 2))
        {
            var mesh = obj.GetComponent<MeshFilter>()?.sharedMesh;
            if (!mesh) continue;
            var scale = obj.transform.lossyScale;
            var newMesh = new Mesh();
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] += new Vector3
                    (
                        Mathf.Sign(verts[i].x) / scale.x,
                        Mathf.Sign(verts[i].y) / scale.y,
                        Mathf.Sign(verts[i].z) / scale.z
                    ) * gridSettings.navMeshOffset;
                verts[i] = obj.transform.localToWorldMatrix.MultiplyPoint3x4(verts[i]);
                verts[i] -= transform.position;
            }
            newMesh.vertices = verts;
            newMesh.triangles = mesh.triangles;
            combine.Add(new CombineInstance() { mesh = newMesh });
        }
        var combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine.ToArray(), true, false);
        combinedMesh.RecalculateNormals();
        combinedMesh.Optimize();
        filter.mesh = combinedMesh;
        collider.enabled = true;
        collider.sharedMesh = combinedMesh;
    }

    public void MarchCubes()
    {
        var filter = GetComponent<MeshFilter>();
        var collider = GetComponent<MeshCollider>();
        filter.sharedMesh = null;
        collider.sharedMesh = null;
        if (grid == null)
        {
            GenerateGrid();
        }
        var mesh = MarchingCubes.March(grid, gridSettings.isoLevel);
        filter.mesh = mesh;
        collider.sharedMesh = mesh;
    }
}
