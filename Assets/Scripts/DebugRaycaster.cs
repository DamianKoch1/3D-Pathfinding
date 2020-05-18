using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugRaycaster : MonoBehaviour
{
    [SerializeField]
    private Transform target;

    public List<Vector3> hits = new List<Vector3>();

    public LayerMask mask;

    void Update()
    {
        hits = new List<Vector3>();
        var dir = (target.position - transform.position).normalized;
        Physics.queriesHitBackfaces = true;
        var pos = transform.position;

        //prevent infinite loops
        int maxRaycasts = 100;

        //RaycastAll only giving first hit since next rays start inside previous face
        while (Physics.Raycast(pos, target.position - pos, out var hit, Vector3.Distance(pos, target.position), mask))
        {
            maxRaycasts--;
            if (Vector3.Distance(pos, target.position) < 0.5f) break;
            pos = hit.point;
            hits.Add(pos);
            //adding small offset from last point to prevent raycasting on the same face
            pos += dir * 0.0001f;
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
            Gizmos.DrawSphere(hit, 0.5f);
        }
    }
}
