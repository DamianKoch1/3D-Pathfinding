using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingTest : MonoBehaviour
{
    [SerializeField]
    private Transform target;

    [SerializeField]
    private List<NavmeshHit> hits = new List<NavmeshHit>();

    [SerializeField]
    private LayerMask mask;

    [SerializeField]
    private MeshFilter targetMesh;

    private MeshVertexGraph graph;

    [SerializeField]
    private PathfindingSettings settings;

    private Vector3 prevPosition;

    [ContextMenu("Build Graph")]
    private void BuildGraph()
    {
        graph = new MeshVertexGraph(targetMesh.sharedMesh, targetMesh.transform);
    }

    [ContextMenu("Clear")]
    private void Clear()
    {
        graph = null;
        GetComponent<LineRenderer>().positionCount = 0;
    }


    [ContextMenu("Find path")]
    public void FindPath()
    {
        if (graph == null) return;
        if (hits.Count <= 1) return;
        var pathPoints = new List<Vector3>();
        pathPoints.Add(transform.position);
        pathPoints.Add(hits[0].point);
        for (int i = 0; i < hits.Count; i += 2)
        {
            pathPoints.AddRange(graph.FindPath(hits[i].point, hits[i + 1].point, settings));
            if (i + 2 < hits.Count)
            {
                pathPoints.Add(hits[i + 2].point);
            }
        }
        pathPoints.Add(hits[hits.Count - 1].point);
        pathPoints.Add(target.position);
        var lr = GetComponent<LineRenderer>();
        lr.positionCount = pathPoints.Count;
        GetComponent<LineRenderer>().SetPositions(pathPoints.ToArray());
    }


    private void OnDrawGizmosSelected()
    {
        Update();
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.color = Color.red;
        foreach (var hit in hits)
        {
            Gizmos.DrawSphere(hit.point, 0.5f);
        }

        if (graph != null)
        {
            
            Gizmos.color = Color.green;
            foreach (var node in graph.nodes.Values)
            {
                Gizmos.DrawWireCube(node.pos, Vector3.one * 0.1f);
            }
        }
    }


    void Update()
    {
        hits = new List<NavmeshHit>();
        var dir = (target.position - transform.position).normalized;
        Physics.queriesHitBackfaces = true;
        var pos = transform.position;

        //prevent infinite loops
        int maxRaycasts = 100;

        //RaycastAll only giving first hit since next rays start inside previous face
        while (Physics.Raycast(pos, target.position - pos, out var hit, Vector3.Distance(pos, target.position), mask))
        {
            maxRaycasts--;
            hits.Add(new NavmeshHit(hit));
            //adding small offset from last point to prevent raycasting on the same face
            pos = hit.point + dir * 0.0001f;
            if (Vector3.Distance(pos, target.position) < 0.5f) break;
            if (maxRaycasts <= 0) break;
        }
    }

    private void OnDrawGizmos()
    {
        
    }
}

[System.Serializable]
public struct NavmeshHit
{
    public Vector3 point;
    public int triangleIndex;

    [Tooltip("1,0,0 = 1st triangle corner, 0,1,0 2nd, 0,0,1 3rd")]
    public Vector3 barycentric;

    public NavmeshHit(RaycastHit hit)
    {
        point = hit.point;
        triangleIndex = hit.triangleIndex;
        barycentric = hit.barycentricCoordinate;
    }
}