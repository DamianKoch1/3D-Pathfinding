using System;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Basic 3D NavMesh Agent
    /// </summary>
    public class PathFollower : MonoBehaviour
    {
        [SerializeField]
        private NodesGenerator generator;

        [SerializeField]
        private Transform target;

        private Vector3[] path;

        [SerializeField]
        private PathfindingMode mode;

        [SerializeField]
        private PathfindingSettings settings;

        private int idx;

        [SerializeField]
        private float speed;

        [SerializeField, Range(0, 1)]
        private float turnRate = 0.3f;

        [SerializeField]
        private float stoppingDistance;

        private Vector3 dir;

        public Action OnReachedTarget;


        void Awake()
        {
            generator.OnInitialize += FindPath;
        }

        private void FindPath()
        {
            idx = 1;
            switch (mode)
            {
                case PathfindingMode.grid:
                    path = generator.FindGridPath(transform.position, target.position, settings).ToArray();
                    break;
                case PathfindingMode.navmesh:
                    path = generator.FindGraphPath(transform.position, target.position, settings).ToArray();
                    break;
            }
        }

        void Update()
        {
            if (!(path?.Length > 0)) return;
            if (idx >= path.Length)
            {
                path = null;
                OnReachedTarget?.Invoke();
                return;
            }
            var newPos = Vector3.MoveTowards(transform.position, path[idx], speed * Time.deltaTime);
            if (Vector3.Distance(newPos, path[idx]) <= stoppingDistance)
            {
                idx++;
            }
            dir = newPos - transform.position;
            transform.position = newPos;
            transform.forward = Vector3.Lerp(transform.forward, dir, turnRate);
        }

        private void OnDrawGizmos()
        {
            if (path == null) return;
            for (int i = 0; i < path.Length - 2; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }
    }

    public enum PathfindingMode
    {
        grid,
        navmesh
    }
}