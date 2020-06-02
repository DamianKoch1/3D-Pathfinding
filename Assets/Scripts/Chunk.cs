using SimplexNoise;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public Grid grid;

    public MeshVertexGraph graph;



    public int x;

    public int y;

    public int z;


    [SerializeField]
    private bool drawNodes;

    [SerializeField]
    private bool drawNeighbours;

    private bool visualizePathfinding;

    private GridGenerationSettings gridSettings;

    private Bounds bounds;

    public void Initialize(GridGenerationSettings settings, int _x, int _y, int _z)
    {
        gridSettings = settings;
        x = _x;
        y = _y;
        z = _z;
        var maxX = (int)(gridSettings.chunkSize.x / gridSettings.step.x);
        var maxY = (int)(gridSettings.chunkSize.y / gridSettings.step.y);
        var maxZ = (int)(gridSettings.chunkSize.z / gridSettings.step.z);
        transform.localPosition = new Vector3(x * maxX * gridSettings.step.x, y * maxY * gridSettings.step.y, z * maxZ * gridSettings.step.z) + gridSettings.chunkSize / 2;
        bounds = new Bounds(transform.position, gridSettings.chunkSize);
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

        if (grid == null)
        {
            grid = new Grid(transform.position, gridSettings, GetIsoValue);
        }
        else
        {
            grid.Update(gridSettings, GetIsoValue);
        }
    }

    public void GenerateGraph()
    {
        graph = new MeshVertexGraph(GetComponent<MeshFilter>(), bounds);
    }


    public void Clear()
    {
        grid = null;
        graph = null;
        GetComponent<MeshFilter>().sharedMesh = null;
        GetComponent<MeshCollider>().sharedMesh = null;
    }


    private void OnDrawGizmos()
    {
        if (!gridSettings) return;
        if (grid != null)
        {
            if (gridSettings.drawExtents)
            {
                Gizmos.DrawWireCube(transform.position, gridSettings.chunkSize);
            }
            if (gridSettings.drawNodes)
            {
                grid.DrawGizmos(gridSettings.nodeColor, gridSettings.isoLevel);
            }
        }

        if (graph != null)
        {
            if (drawNodes || drawNeighbours)
            {
                foreach (var node in graph.nodes.Values)
                {
                    if (drawNodes)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(node.pos, Vector3.one * 0.1f);
                    }
                    if (drawNeighbours)
                    {
                        Gizmos.color = Color.magenta;
                        foreach (var neighbour in node.neighbours)
                        {
                            Gizmos.DrawLine(node.pos, neighbour.pos);
                        }
                    }
                }
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
        filter.sharedMesh = mesh;
        collider.sharedMesh = mesh;
    }
}
