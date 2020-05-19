using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugRaycaster : MonoBehaviour
{
    [SerializeField]
    private Transform target;

    public List<NavmeshHit> hits = new List<NavmeshHit>();

    public LayerMask mask;

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
        Update();
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.color = Color.red;
        foreach (var hit in hits)
        {
            Gizmos.DrawSphere(hit.point, 0.5f);
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