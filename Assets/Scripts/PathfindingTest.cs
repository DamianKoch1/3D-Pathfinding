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


    [SerializeField, Range(0.1f, 5)]
    private float minRecalculateMovement = 1;

    [SerializeField]
    private PathfindingSettings settings;

    private Vector3 prevPosition;


    public List<NavmeshHit> GetNavMeshIntersections(Vector3 start, Vector3 goal)
    {
        var hits = new List<NavmeshHit>();
        var dir = (goal - start).normalized;
        Physics.queriesHitBackfaces = true;
        var pos = start;

        //prevent infinite loops
        int maxRaycasts = 100;

        //RaycastAll only giving first hit since next rays start inside previous face
        while (Physics.Raycast(pos, goal - pos, out var hit, Vector3.Distance(pos, goal), mask))
        {
            maxRaycasts--;
            hits.Add(new NavmeshHit(hit));
            //adding small offset from last point to prevent raycasting on the same face
            pos = hit.point + dir * 0.0001f;
            if (Vector3.Distance(pos, start) < 0.5f) break;
            if (maxRaycasts <= 0) break;
        }

        return hits;
    }

    //TODO fix path walking on mesh between wrong hits if start is inside mesh
    [ContextMenu("Find path")]
    public void FindPath()
    {
        //if (graph == null) return;
        //var lr = GetComponent<LineRenderer>();
        //var pathPoints = new List<Vector3>();
        //pathPoints.Add(start.position);
        //if (hits.Count > 0)
        //{
        //    pathPoints.Add(hits[0].point);
        //    if (hits.Count > 1)
        //    {
        //        for (int i = 0; i < hits.Count - 1; i += 2)
        //        {
        //            pathPoints.AddRange(graph.FindPath(hits[i].point, hits[i + 1].point, settings));
        //            if (i + 2 < hits.Count)
        //            {
        //                pathPoints.Add(hits[i + 2].point);
        //            }
        //        }
        //        pathPoints.Add(hits[hits.Count - 1].point);
        //    }
        //}
        //pathPoints.Add(target.position);
        //lr.positionCount = pathPoints.Count;
        //lr.SetPositions(pathPoints.ToArray());
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        GetComponent<LineRenderer>().positionCount = 0;
    }


    private void OnDrawGizmos()
    {
        if (Vector3.Distance(start.position, prevPosition) >= minRecalculateMovement)
        {
            hits = GetNavMeshIntersections(start.position, target.position);
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
    }

}

