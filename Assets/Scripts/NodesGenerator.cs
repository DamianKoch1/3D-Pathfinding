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

    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        System.Func<Vector3, bool> walkableCheck = null;

        switch (gridSettings.mode)
        {
            case Mode.Physics:
                walkableCheck = (Vector3 pos) => Physics.OverlapSphere(pos, gridSettings.navMeshOffset).Length == 0;
                break;
            case Mode.Noise:
                if (gridSettings.useRandomSeed)
                {
                    gridSettings.seed = Random.Range(0, 10000);
                    Noise.Seed = gridSettings.seed;
                }
                walkableCheck = (Vector3 pos) => Noise.CalcPixel3D(pos.x, pos.y, pos.z, gridSettings.scale) >= gridSettings.threshold;
                break;
        }

        grid = new Grid(transform.position, gridSettings, walkableCheck);

    }

    private void Update()
    {
        FindPath();
    }

    private void OnValidate()
    {
        gridSettings.onValidate = OnValidate;
        GenerateGrid();
        MarchCubes();
    }

    public void FindPath(float drawDuration = 0)
    {
        if (grid == null)
        {
            GenerateGrid();
        }
        var path = grid.FindPath(start.position, end.position, pathfindingSettings);
        var pathPos = path.Pop();
        Debug.DrawLine(start.position, pathPos, Color.blue, drawDuration);
        while (path.Count > 0)
        {
            Debug.DrawLine(pathPos, path.Peek(), Color.blue, drawDuration);
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
                grid.DrawGizmos();
            }
        }
    }


    [ContextMenu("RandomizeObstacles")]
    public void RandomizeObstacles()
    {
        if (!obstacleSettings.generate)
        {
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
            return;
        }

        if (!obstacleSettings.prefab) return;

        var childCount = transform.childCount;
        if (childCount < obstacleSettings.count)
        {
            for (int i = childCount; i < obstacleSettings.count; i++)
            {
                Instantiate(obstacleSettings.prefab, transform);
            }
        }
        else
        {
            for (int i = 0; i < childCount - obstacleSettings.count; i++)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }

        foreach (Transform child in transform)
        {
            child.position = transform.position + new Vector3
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
        foreach (var obj in Physics.OverlapBox(transform.position, gridSettings.size / 2))
        {
            var mesh = obj.GetComponent<MeshFilter>()?.sharedMesh;
            if (!mesh) continue;
            var scale = obj.transform.lossyScale;
            var newMesh = new Mesh();
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] += new Vector3(Mathf.Sign(verts[i].x) / scale.x, Mathf.Sign(verts[i].y) / scale.y, Mathf.Sign(verts[i].z) / scale.z) * gridSettings.navMeshOffset;
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
    }

    public void MarchCubes()
    {
        var filter = GetComponent<MeshFilter>();
        if (grid == null)
        {
            GenerateGrid();
        }
        var mesh = MarchingCubes.March(Node.ToBoolArray(grid.nodes), gridSettings.size, gridSettings.step);
        mesh.RecalculateNormals();
        mesh.Optimize();
        filter.mesh = mesh;
    }
}
