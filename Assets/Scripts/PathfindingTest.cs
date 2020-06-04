using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

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


    [SerializeField, Range(0.1f, 5)]
    private float minRecalculateMovement = 1;

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

