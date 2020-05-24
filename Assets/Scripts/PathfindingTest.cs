using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathfindingTest : MonoBehaviour
{
    [SerializeField]
    private Transform start;

    [SerializeField]
    private Transform target;

    [SerializeField]
    private List<NavmeshHit> hits = new List<NavmeshHit>();

    [SerializeField]
    private LayerMask mask;

    [SerializeField]
    private MeshFilter targetMesh;

    private MeshVertexGraph graph;

    [SerializeField, Range(0.1f, 5)]
    private float minRecalculateMovement = 1;

    [SerializeField]
    private PathfindingSettings settings;

    private Vector3 prevPosition;

    [SerializeField]
    private bool drawNodes;

    [SerializeField]
    private bool drawNeighbours;


    public void BuildGraph()
    {
        graph = new MeshVertexGraph(targetMesh);
    }

    //TODO fix path walking on mesh between wrong hits if start is inside mesh
    [ContextMenu("Find path")]
    public void FindPath()
    {
        if (graph == null) return;
        var lr = GetComponent<LineRenderer>();
        var pathPoints = new List<Vector3>();
        pathPoints.Add(start.position);
        if (hits.Count > 0)
        {
            pathPoints.Add(hits[0].point);
            if (hits.Count > 1)
            {
                for (int i = 0; i < hits.Count - 1; i += 2)
                {
                    pathPoints.AddRange(graph.FindPath(hits[i].point, hits[i + 1].point, settings));
                    if (i + 2 < hits.Count)
                    {
                        pathPoints.Add(hits[i + 2].point);
                    }
                }
                pathPoints.Add(hits[hits.Count - 1].point);
            }
        }
        pathPoints.Add(target.position);
        lr.positionCount = pathPoints.Count;
        lr.SetPositions(pathPoints.ToArray());
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        graph = null;
        GetComponent<LineRenderer>().positionCount = 0;
    }


    private void OnDrawGizmos()
    {
        Update();

        if (Vector3.Distance(start.position, prevPosition) >= minRecalculateMovement)
        {
            prevPosition = start.position;
            FindPath();
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start.position, target.position);
        Gizmos.color = Color.red;
        foreach (var hit in hits)
        {
            Gizmos.DrawSphere(hit.point, 0.5f);
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
                        Gizmos.color = Color.gray;
                        foreach (var neighbour in node.neighbours)
                        {
                            Gizmos.DrawLine(node.pos, neighbour.pos);
                        }
                    }
                }
            }
        }
    }


    void Update()
    {
        if (!start) return;
        if (!target) return;
        hits = new List<NavmeshHit>();
        var dir = (target.position - start.position).normalized;
        Physics.queriesHitBackfaces = true;
        var pos = start.position;

        //prevent infinite loops
        int maxRaycasts = 100;

        //RaycastAll only giving first hit since next rays start inside previous face
        while (Physics.Raycast(pos, target.position - pos, out var hit, Vector3.Distance(pos, target.position), mask))
        {
            maxRaycasts--;
            hits.Add(new NavmeshHit(hit));
            //adding small offset from last point to prevent raycasting on the same face
            pos = hit.point + dir * 0.0001f;
            if (Vector3.Distance(pos, start.position) < 0.5f) break;
            if (maxRaycasts <= 0) break;
        }
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