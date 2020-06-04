using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Prototype 3D NavMesh Agent
    /// </summary>
    public class PathFollower : MonoBehaviour
    {
        [SerializeField]
        private NodesGenerator generator;

        [SerializeField]
        private Transform target;

        private List<Vector3> path;

        [SerializeField]
        private PathfindingMode mode;

        [SerializeField]
        private PathfindingSettings settings;

        private int idx;

        [SerializeField]
        private float speed;

        [SerializeField]
        private float stoppingDistance;

        private Vector3 dir;


        void Start()
        {
            FindPath();
        }

        private void FindPath()
        {
            idx = 1;
            switch (mode)
            {
                case PathfindingMode.grid:
                    path = generator.FindGridPath(transform.position, target.position, settings);
                    break;
                case PathfindingMode.navmesh:
                    path = generator.FindGraphPath(transform.position, target.position, settings);
                    break;
            }
        }

        void Update()
        {
            if (path == null) return;
            if (idx >= path.Count) return;
            var newPos = Vector3.MoveTowards(transform.position, path[idx], speed * Time.deltaTime);
            if (Vector3.Distance(newPos, path[idx]) <= stoppingDistance)
            {
                idx++;
            }
            dir = newPos - transform.position;
            transform.position = newPos;
            transform.forward = Vector3.Lerp(transform.forward, dir, 0.3f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawLine(transform.position, target.position);
        }
    }

    public enum PathfindingMode
    {
        grid,
        navmesh
    }
}