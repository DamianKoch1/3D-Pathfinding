using SimplexNoise;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public Grid grid;

    public int x;

    public int y;

    public int z;


    private GridGenerationSettings gridSettings;

    public void Initialize(GridGenerationSettings settings, int _x, int _y, int _z)
    {
        gridSettings = settings;
        x = _x;
        y = _y;
        z = _z;
        var maxX = (int)(gridSettings.chunkSize.x / gridSettings.step.x);
        var maxY = (int)(gridSettings.chunkSize.y / gridSettings.step.y);
        var maxZ = (int)(gridSettings.chunkSize.z / gridSettings.step.z);
        transform.localPosition = new Vector3(x * maxX * gridSettings.step.x, y * maxY * gridSettings.step.y, z * maxZ * gridSettings.step.z);
        gameObject.name = "Chunk " + new Vector3Int(x, y, z);
    }

    public void GenerateGrid()
    {
        System.Func<Vector3, float> GetIsoValue = null;
        switch (gridSettings.mode)
        {
            case Mode.Overlap:
                GetIsoValue = (Vector3 pos) =>
                {
                    var overlaps = Physics.OverlapSphere(pos, gridSettings.navMeshOffset, ~gridSettings.navmeshLayer.value);
                    if (overlaps.Length == 0) return 1;
                    var nearest = overlaps.OrderBy(o => Vector3.Distance(pos, o.ClosestPoint(pos))).First();
                    return Vector3.Distance(pos, nearest.ClosestPoint(pos)) / gridSettings.navMeshOffset;
                };
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
    }

    public void Clear()
    {
        grid = null;
        GetComponent<MeshFilter>().sharedMesh = null;
        GetComponent<MeshCollider>().sharedMesh = null;
    }

    public Stack<Vector3> FindPath(Vector3 start, Vector3 end, PathfindingSettings pathfindingSettings)
    {
        if (!gridSettings) return new Stack<Vector3>();
        return grid.FindPath(start, end, pathfindingSettings, gridSettings.isoLevel);
    }

    private void OnDrawGizmos()
    {
        if (!gridSettings) return;
        if (!gridSettings.drawExtents) return;
        Gizmos.DrawWireCube(transform.position, gridSettings.chunkSize);
        if (grid != null)
        {
            if (gridSettings.drawNodes)
            {
                grid.DrawGizmos(gridSettings.nodeColor, gridSettings.isoLevel);
            }
        }
    }

    public void ExpandMeshes()
    {
        if (!gridSettings) return;
        var combine = new List<CombineInstance>();
        var filter = GetComponent<MeshFilter>();
        var collider = GetComponent<MeshCollider>();
        foreach (var obj in Physics.OverlapBox(transform.position, gridSettings.chunkSize / 2, Quaternion.identity, ~gridSettings.navmeshLayer.value))
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
        collider.sharedMesh = combinedMesh;
    }

    public void MarchCubes()
    {
        if (!gridSettings) return;
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
